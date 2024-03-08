using LanguageExt.ClassInstances.Pred;
using LanguageExt.Common;
using LanguageExt.TypeClasses;
using LanguageExt;
using System;
using System.Collections.Generic;
using System.Text;
using static LanguageExt.Prelude;

namespace Dbosoft.Functional.DataTypes;

/// <summary>
/// This class extends the LanguageExt <see cref="NewType{NEWTYPE, A, PRED, ORD}"/>
/// for support for validation using <see cref="Validation{FAIL,SUCCESS}"/>.
/// </summary>
public abstract class ValidatingNewType<NEWTYPE, A, ORD> : NewType<NEWTYPE, A, True<A>, ORD>
    where NEWTYPE : ValidatingNewType<NEWTYPE, A, ORD>
    where ORD : struct, Ord<A>
{
    protected ValidatingNewType(A value) : base(value) { }

    public static Validation<Error, NEWTYPE> NewValidation(A value) =>
        NewTry(value).Match(
            Succ: Success<Error, NEWTYPE>,
            Fail: ex => ex switch
            {
                ValidationException<NEWTYPE> vex => Fail<Error, NEWTYPE>(vex.Errors),
                ArgumentNullException _ => Fail<Error, NEWTYPE>(Error.New("The value cannot be null.")),
                _ => Fail<Error, NEWTYPE>(Error.New(ex))
            });

    public static Either<Error, NEWTYPE> NewEither(A value) =>
        NewValidation(value).ToEither().MapLeft(
            errors => Error.New($"The value is not a valid {typeof(NEWTYPE).Name}.", Error.Many(errors)));

    /// <summary>
    /// Subclasses should use this method in their constructors to validate the value.
    /// </summary>
    /// <exception cref="ValidationException{NEWTYPE}">
    /// Thrown when the validation has failed.
    /// </exception>
    protected static T ValidOrThrow<T>(Validation<Error, T> validation) =>
        validation.Match(
            Succ: identity,
            Fail: errors => throw new ValidationException<NEWTYPE>(errors));

    /// <summary>
    /// This exception is thrown by <see cref="ValidatingNewType{NEWTYPE, A, ORD}"/>
    /// when the validation has failed.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class ValidationException<T> : Exception
    {
        internal ValidationException(Seq<Error> errors)
            : base($"The value is not a valid {typeof(T).Name}: {errors.ToFullArrayString()}")
        {
            Errors = errors;
        }

        /// <summary>
        /// The actual validation errors.
        /// </summary>
        public Seq<Error> Errors { get; }
    }
}
