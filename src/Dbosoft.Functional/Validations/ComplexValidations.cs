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
        ValidateProperty(
            Optional(getProperty.Compile().Invoke(toValidate)).Filter(notEmpty),
            v => validate(v).Map(_ => unit)
                .MapFail(e => new ValidationIssue(JoinPath(path, getProperty), e.Message)),
            path,
            GetPropertyName(getProperty),
            required);

    public static Validation<ValidationIssue, Unit> ValidateProperty<T, TProperty, TResult>(
        T toValidate,
        Expression<Func<T, TProperty?>> getProperty,
        Func<TProperty, Validation<Error, TResult>> validate,
        string path = "",
        bool required = false) where TProperty :struct
    {
        var value = getProperty.Compile().Invoke(toValidate);
        return ValidateProperty(
            value.HasValue ? Some(value.Value) : None,
            v => validate(v).Map(_ => unit)
                .MapFail(e => new ValidationIssue(JoinPath(path, getProperty), e.Message)),
            path,
            GetPropertyName(getProperty),
            required);
    }

    public static Validation<ValidationIssue, Unit> ValidateProperty<T, TProperty, TResult>(
        T toValidate,
        Expression<Func<T, TProperty?>> getProperty,
        Func<TProperty, Validation<Error, TResult>> validate,
        string path = "",
        bool required = false) =>
        ValidateProperty(
            Optional(getProperty.Compile().Invoke(toValidate)),
            v => validate(v).Map(_ => unit)
                .MapFail(e => new ValidationIssue(JoinPath(path, getProperty), e.Message)),
            path,
            GetPropertyName(getProperty),
            required);

    public static Validation<ValidationIssue, Unit> ValidateProperty<T, TProperty>(
        T toValidate,
        Expression<Func<T, TProperty?>> getProperty,
        Func<TProperty, string, Validation<ValidationIssue, Unit>> validate,
        string path = "",
        bool required = false) =>
        ValidateProperty(
            Optional(getProperty.Compile().Invoke(toValidate)),
            v => validate(v, JoinPath(path, getProperty)),
            path,
            GetPropertyName(getProperty),
            required);

    private static Validation<ValidationIssue, Unit> ValidateProperty<TProperty>(
        Option<TProperty> value,
        Func<TProperty, Validation<ValidationIssue, Unit>> validate,
        string path,
        string propertyName,
        bool required) =>
        value.Match(
            Some: validate,
            None: () => required
                ? Fail<ValidationIssue, Unit>(
                    new ValidationIssue(JoinPath(path, propertyName),
                        $"The {propertyName} is required."))
                : Success<ValidationIssue, Unit>(unit));

    public static Validation<ValidationIssue, Unit> ValidateList<T, TProperty>(
        T toValidate,
        Expression<Func<T, IEnumerable<TProperty?>?>> getList,
        Func<TProperty, string, Validation<ValidationIssue, Unit>> validate,
        string path = "",
        Option<int> minCount = default,
        Option<int> maxCount = default) =>
        ValidateList(
            getList.Compile().Invoke(toValidate).ToSeq(),
            getList,
            validate,
            path,
            minCount,
            maxCount);

    private static Validation<ValidationIssue, Unit> ValidateList<T, TProperty>(
        Seq<TProperty?> list,
        Expression<Func<T, IEnumerable<TProperty?>?>> getList,
        Func<TProperty, string, Validation<ValidationIssue, Unit>> validate,
        string path = "",
        Option<int> minCount = default,
        Option<int> maxCount = default) =>
        match(minCount.Filter(c => c > list.Count),
            Some: c => Fail<ValidationIssue, Unit>(
                new ValidationIssue(JoinPath(path, getList), $"The list must have {c} or more entries.")),
            None: () => Success<ValidationIssue, Unit>(unit))
        | match(maxCount.Filter(c => c < list.Count),
            Some: c => Fail<ValidationIssue, Unit>(
                new ValidationIssue(JoinPath(path, getList), $"The list must have {c} or fewer entries.")),
            None: () => Success<ValidationIssue, Unit>(unit))
        | list.Map((index, listItem) =>
                from li in Optional(listItem).ToValidation(
                    new ValidationIssue($"{JoinPath(path, getList)}[{index}]", "The entry must not be null."))
                from _ in validate(listItem, $"{JoinPath(path, getList)}[{index}]")
                select unit)
            .Fold(Success<ValidationIssue, Unit>(unit), (acc, listItem) => acc | listItem);

    private static string JoinPath<T, TProperty>(string path, Expression<Func<T, TProperty?>> getProperty) =>
        notEmpty(path) ? $"{path}.{GetPropertyName(getProperty)}" : GetPropertyName(getProperty);

    private static string JoinPath(string path, string propertyName) =>
        notEmpty(path) ? $"{path}.{propertyName}" : propertyName;

    private static string GetPropertyName<T, TProperty>(Expression<Func<T, TProperty?>> getProperty) =>
        getProperty.Body switch
        {
            MemberExpression memberExpression => memberExpression.Member.Name,
            _ => throw new ArgumentException("The expression must access and return a class member.",
                nameof(getProperty))
        };
}

#nullable restore
