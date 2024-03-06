using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Dbosoft.Functional.Validations;
using LanguageExt;
using LanguageExt.Common;
using Moq;
using static Dbosoft.Functional.Validations.ComplexValidations;
using static LanguageExt.Prelude;

namespace Dbosoft.Functional.Tests.Validations;

public class ComplexValidationsTests
{
    [Fact]
    public void ValidateProperty_NotRequiredAndNullableIntIsNull_ReturnsSuccess()
    {
        var validateMock = new Mock<Func<int, Validation<Error, int>>>();
        var result = ValidateProperty(
            new TestType() { NullableIntValue = null },
            t => t.NullableIntValue,
            validateMock.Object,
            "Some.Path",
            required: false);

        result.Should().BeSuccess();
        validateMock.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void ValidateProperty_NotRequiredAndStringIsEmpty_ReturnsSuccess(string? value)
    {
        var validateMock = new Mock<Func<string, Validation<Error, string>>>();
        var result = ValidateProperty(
            new TestType() { StringValue = value },
            t => t.StringValue,
            validateMock.Object,
            "Some.Path",
            required: false);

        result.Should().BeSuccess();
        validateMock.VerifyNoOtherCalls();
    }

    [Fact]
    public void ValidateProperty_RequiredAndNullableIntIsNull_ReturnsFail()
    {
        var result = ValidateProperty(
            new TestType() { NullableIntValue = null },
            t => t.NullableIntValue,
            (int _) => Success<Error, int>(1),
            "Some.Path",
            required: true);

        result.Should().BeFail().Which.Should().SatisfyRespectively(
            issue =>
            {
                issue.Member.Should().Be("Some.Path.NullableIntValue");
                issue.Message.Should().Be("The NullableIntValue is required.");
            });
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void ValidateProperty_RequiredAndStringIsEmpty_ReturnsFail(string? value)
    {
        var result = ValidateProperty(
            new TestType() { StringValue = value },
            t => t.StringValue,
            _ => Success<Error, string>("test"),
            "Some.Path",
            required: true);

        result.Should().BeFail().Which.Should().SatisfyRespectively(
            issue =>
            {
                issue.Member.Should().Be("Some.Path.StringValue");
                issue.Message.Should().Be("The StringValue is required.");
            });
    }

    public class TravelGroupExample
    {
        [Fact]
        public void ValidateTravelGroup_ValidTravelGroupWithOptionalData_ReturnsSuccess()
        {
            var travelGroup = new TravelGroup
            {
                Name = "My Travel Group",
                Description = "The most important\ntravel group",
                Contact = new Contact
                {
                    Name = "John Doe",
                    Phone = "+1234567890"
                },
                Participants = new[]
                {
                    new Participant { Name = "Alice", Age = 25 },
                    new Participant { Name = "Bob", Age = 30 },
                }
            };

            var result = TravelGroupValidations.ValidateTravelGroup(travelGroup);

            result.Should().BeSuccess();
        }

        [Fact]
        public void ValidateTravelGroup_ValidTravelGroupWithoutOptionalData_ReturnsSuccess()
        {
            var travelGroup = new TravelGroup
            {
                Name = "My Travel Group",
                Contact = new Contact
                {
                    Name = "John Doe",
                },
                Participants = new[]
                {
                    new Participant { Name = "Alice", Age = 25 },
                    new Participant { Name = "Bob", Age = 30 },
                }
            };

            var result = TravelGroupValidations.ValidateTravelGroup(travelGroup);

            result.Should().BeSuccess();
        }

        [Fact]
        public void ValidateTravelGroup_InvalidTravelGroup_ReturnsFail()
        {
            var travelGroup = new TravelGroup
            {
                Name = "My\nTravel\nGroup",
                Description = "The most important | travel group",
                Contact = new Contact
                {
                    Name = "John\nDoe",
                    Phone = "abc"
                },
                Participants = new[]
                {
                    new Participant { Name = "Alice\nAdams", Age = -1 },
                }
            };

            var result = TravelGroupValidations.ValidateTravelGroup(travelGroup);

            result.Should().BeFail().Which.Should().SatisfyRespectively(
                issue =>
                {
                    issue.Member.Should().Be("Name");
                    issue.Message.Should().Be("The name can only contain letters, digits and spaces.");
                },
                issue =>
                {
                    issue.Member.Should().Be("Description");
                    issue.Message.Should().Be("The description can only contain letters, digits and white space.");
                },
                issue =>
                {
                    issue.Member.Should().Be("Contact.Name");
                    issue.Message.Should().Be("The name can only contain letters, digits and spaces.");
                },
                issue =>
                {
                    issue.Member.Should().Be("Contact.Phone");
                    issue.Message.Should().Be("The phone number can only contain digits or +.");
                },
                issue =>
                {
                    issue.Member.Should().Be("Participants");
                    issue.Message.Should().Be("The list must have 2 or more entries.");
                },
                issue =>
                {
                    issue.Member.Should().Be("Participants[0].Name");
                    issue.Message.Should().Be("The name can only contain letters, digits and spaces.");
                },
                issue =>
                {
                    issue.Member.Should().Be("Participants[0].Age");
                    issue.Message.Should().Be("The age must be between 0 and 150.");
                });
        }
    }
}

public static class TravelGroupValidations
{
    public static Validation<ValidationIssue, Unit> ValidateTravelGroup(
        TravelGroup travelGroup, string path = "") =>
        ValidateProperty(travelGroup, x => x.Name, ValidateName, path, required: true)
        | ValidateProperty(travelGroup, x => x.Description, ValidateDescription, path, required: false)
        | ValidateProperty(travelGroup, x => x.Contact, ValidateContact, path, required: true)
        | ValidateList(travelGroup, x => x.Participants, ValidateParticipant, path, minCount: 2, maxCount: 20);

    public static Validation<ValidationIssue, Unit> ValidateContact(
        Contact contact, string path = "") =>
        ValidateProperty(contact, x => x.Name, ValidateName, path, required: true)
        | ValidateProperty(contact, x => x.Phone, ValidatePhone, path, required: false);

    public static Validation<ValidationIssue, Unit> ValidateParticipant(
        Participant participant, string path = "") =>
        ValidateProperty(participant, x => x.Name, ValidateName, path, required: true)
        | ValidateProperty(participant, x => x.Age, ValidateAge, path, required: true);

    public static Validation<Error, string> ValidateName(string name) =>
        from _ in guard(name.ToSeq().All(c => char.IsLetterOrDigit(c) || c == ' '),
                Error.New("The name can only contain letters, digits and spaces."))
            .ToValidation()
        select name;

    public static Validation<Error, string> ValidateDescription(string description) =>
        from _ in guard(description.ToSeq().All(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c)),
                Error.New("The description can only contain letters, digits and white space."))
            .ToValidation()
        select description;

    public static Validation<Error, int> ValidateAge(int age) =>
        from _ in guard(age is >= 0 and <= 150,
                Error.New("The age must be between 0 and 150."))
            .ToValidation()
        select age;

    public static Validation<Error, string> ValidatePhone(string phone) =>
        from _ in guard(phone.ToSeq().All(c => char.IsDigit(c) || c == '+'),
                Error.New("The phone number can only contain digits or +."))
            .ToValidation()
        select phone;
}

public class TestType
{
    public string? StringValue { get; set; }

    public int? NullableIntValue { get; set; }
}

public class TravelGroup
{
    public string? Name { get; set; }

    public string? Description { get; set; }

    public Contact? Contact { get; set; }

    public Participant[]? Participants { get; set; }
}

public class Participant
{
    public string? Name { get; set; }

    public int Age { get; set; }
}

public class Contact
{
    public string? Name { get; set; }

    public string? Phone { get; set; }
}
