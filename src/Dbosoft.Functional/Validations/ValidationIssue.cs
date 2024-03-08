using LanguageExt.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dbosoft.Functional.Validations;

/// <summary>
/// Represents an issue which was detected during a validation.
/// </summary>
public readonly struct ValidationIssue
{
    private readonly string _member;
    private readonly string _message;

    /// <summary>
    /// Creates a new validation issue.
    /// </summary>
    public ValidationIssue(string member, string message)
    {
        _member = member;
        _message = message;
    }

    /// <summary>
    /// The path to the member in the object tree which caused the issue,
    /// e.g. <c>Participants[2].Name</c>.
    /// </summary>
    public string Member => _member;

    /// <summary>
    /// The description of the issue.
    /// </summary>
    public string Message => _message;

    /// <summary>
    /// Converts the issue to an <see cref="Error"/>.
    /// </summary>
    public Error ToError() => Error.New(ToString());

    /// <summary>
    /// Returns a string representation of the issue.
    /// </summary>
    public override string ToString() =>
        string.IsNullOrWhiteSpace(_member) ? _message : $"{_member}: {_message}";
}
