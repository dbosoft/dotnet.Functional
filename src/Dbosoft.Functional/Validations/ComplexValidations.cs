using LanguageExt.Common;
using LanguageExt;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Text;
using static LanguageExt.Prelude;

namespace Dbosoft.Functional.Validations;

#nullable enable

/// <summary>
/// These functions can be used to validate complex objects. They
/// automate the process of the descending through an object tree
/// and collecting the validation issues.
/// </summary>
public static class ComplexValidations
{
    public static Validation<ValidationIssue, Unit> ValidateProperty<T, TResult>(
        T toValidate,
        Expression<Func<T, string?>> getProperty,
        Func<string, Validation<Error, TResult>> validate,
        string path = "", 
        bool required = false) =>
        Optional(getProperty.Compile().Invoke(toValidate))
            .Filter(notEmpty)
            .Match(
                Some: v => validate(v).Map(_ => unit)
                    .MapFail(e => new ValidationIssue(JoinPath(path, getProperty), e.Message)),
                None: Success<ValidationIssue, Unit>(unit));

    public static Validation<ValidationIssue, Unit> ValidateProperty<T, TProperty, TResult>(
        T toValidate,
        Expression<Func<T, TProperty?>> getProperty,
        Func<TProperty, Validation<Error, TResult>> validate,
        string path = "",
        bool required = false) =>
        Optional(getProperty.Compile().Invoke(toValidate))
            .Match(
                Some: v => validate(v).Map(_ => unit)
                    .MapFail(e => new ValidationIssue(JoinPath(path, getProperty), e.Message)),
                None: Success<ValidationIssue, Unit>(unit));

    public static Validation<ValidationIssue, Unit> ValidateProperty<T, TProperty>(
        T toValidate,
        Expression<Func<T, TProperty?>> getProperty,
        Func<TProperty, string, Validation<ValidationIssue, Unit>> validate,
        string path = "",
        bool required = false) =>
        Optional(getProperty.Compile().Invoke(toValidate))
            .Match(
                Some: v => validate(v, JoinPath(path, getProperty)),
                None: Success<ValidationIssue, Unit>(unit));

    private static Validation<ValidationIssue, Unit> ValidateProperty<T, TProperty>(
        Option<TProperty> value,
        Func<TProperty, Validation<ValidationIssue, Unit>> validate,
        string propertyName,
        string path,
        bool required) =>
        value.Match(
            Some: validate,
            None: () => required
                ? Fail<ValidationIssue, Unit>(new ValidationIssue(path, $"The property {propertyName} is required."))
                : Success<ValidationIssue, Unit>(unit));

    public static Validation<ValidationIssue, Unit> ValidateList<T, TProperty>(
        T toValidate,
        Expression<Func<T, IEnumerable<TProperty?>?>> getList,
        Func<TProperty, string, Validation<ValidationIssue, Unit>> validate,
        string path = "",
        Option<int> minCount = default,
        Option<int> maxCount = default) =>
        getList.Compile().Invoke(toValidate).ToSeq()
            .Map((index, listItem) =>
                from li in Optional(listItem).ToValidation(
                    new ValidationIssue($"{JoinPath(path, getList)}[{index}]", "The entry must not be null."))
                from _ in validate(listItem, $"{JoinPath(path, getList)}[{index}]")
            select unit)
            .Fold(Success<ValidationIssue, Unit>(unit), (acc, listItem) => acc | listItem);

    private static string JoinPath<T, TProperty>(string path, Expression<Func<T, TProperty?>> getProperty)
    {
        if (!(getProperty.Body is MemberExpression memberExpression))
            throw new ArgumentException("The expression must access and return a class member");

        return notEmpty(path) ? $"{path}.{memberExpression.Member.Name}" : memberExpression.Member.Name;
    }
}

#nullable restore
