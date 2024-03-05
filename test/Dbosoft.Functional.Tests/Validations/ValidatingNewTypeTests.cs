using Dbosoft.Functional.Validations;
using LanguageExt.ClassInstances;
using LanguageExt.Common;
using LanguageExt;
using static FluentAssertions.FluentActions;
using static LanguageExt.Prelude;

public class ValidatingNewTypeTests
{
    public class TestTypeWithArgumentException
        : ValidatingNewType<TestTypeWithArgumentException, string, OrdStringOrdinalIgnoreCase>
    {
        public TestTypeWithArgumentException(string value) : base(value)
        {
            throw new ArgumentException("The value is invalid", nameof(value));
        }
    }

    public class TestTypeWithValidationException
        : ValidatingNewType<TestTypeWithValidationException, string, OrdStringOrdinalIgnoreCase>
    {
        public TestTypeWithValidationException(string value) : base(value)
        {
            ValidOrThrow(Fail<Error, string>(Error.New("First validation failed"))
                         | Fail<Error, string>(Error.New("Second validation failed")));
        }
    }

    [Fact]
    public void New_WithArgumentException_ThrowsArgumentException()
    {
        Invoking(() => TestTypeWithArgumentException.New("test"))
            .Should().Throw<ArgumentException>()
                .WithMessage("The value is invalid (Parameter 'value')");
    }

    [Fact]
    public void New_WithValidationErrors_ThrowsValidationException()
    {
        var exception = Invoking(() => TestTypeWithValidationException.New("test"))
            .Should().Throw<TestTypeWithValidationException.ValidationException<TestTypeWithValidationException>>()
            .WithMessage("The value is not a valid TestTypeWithValidationException: "
                + "[First validation failed, Second validation failed]")
            .Subject.Should().ContainSingle().Subject;

        ShouldContainValidationErrors(exception.Errors);
    }

    [Fact]
    public void NewEither_WithArgumentException_ReturnsErrorWithInnerError()
    {
        var result = TestTypeWithArgumentException.NewEither("test");

        var error = result.Should().BeLeft().Subject;
        var innerError = error.Inner.Should().BeSome()
            .Which.Should().BeOfType<Exceptional>().Subject;
        innerError.Message.Should().Be("The value is invalid (Parameter 'value')");
        innerError.IsExceptional.Should().BeTrue();
        innerError.Exception.Should().BeSome().Which
            .Should().BeOfType<ArgumentException>();
    }

    [Fact]
    public void NewEither_WithValidationErrors_ReturnsErrorWithInnerError()
    {
        var result = TestTypeWithValidationException.NewEither("test");

        var error = result.Should().BeLeft().Subject;
        error.Message.Should().Be("The value is not a valid TestTypeWithValidationException.");
        var innerErrors = error.Inner.Should().BeSome().Which
            .Should().BeOfType<ManyErrors>().Subject.Errors;
        ShouldContainValidationErrors(innerErrors);
    }

    [Fact]
    public void NewValidation_ArgumentException_ReturnsErrorWithException()
    {
        var result = TestTypeWithArgumentException.NewValidation("test");

        result.Should().BeFail().Which.Should().SatisfyRespectively(
            error =>
            {
                error.Message.Should().Be("The value is invalid (Parameter 'value')");
                error.IsExceptional.Should().BeTrue();
                error.Exception.Should().BeSome().Which
                    .Should().BeOfType<ArgumentException>();
            });
    }

    [Fact]
    public void NewValidation_WithValidationErrors_ReturnsBothErrors()
    {
        var result = TestTypeWithValidationException.NewValidation("test");

        var errors = result.Should().BeFail().Subject;
        ShouldContainValidationErrors(errors);
    }

    private static void ShouldContainValidationErrors(Seq<Error> errors)
    {
        errors.Should().SatisfyRespectively(
            error =>
            {
                error.Message.Should().Be("First validation failed");
                error.IsExceptional.Should().BeFalse();
                error.Exception.Should().BeNone();
            },
            error =>
            {
                error.Message.Should().Be("Second validation failed");
                error.IsExceptional.Should().BeFalse();
                error.Exception.Should().BeNone();
            });
    }
}
