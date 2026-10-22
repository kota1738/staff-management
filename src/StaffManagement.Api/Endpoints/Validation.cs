using System.ComponentModel.DataAnnotations;

namespace StaffManagement.Api.Endpoints;

/// <summary>
/// Data-annotation validation for minimal APIs, shaped like ValidationProblemDetails
/// so the response matches what an MVC controller would return.
/// </summary>
public static class Validation
{
    public static Dictionary<string, string[]> Validate(object model)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(model);

        if (Validator.TryValidateObject(model, context, results, validateAllProperties: true))
        {
            return new Dictionary<string, string[]>();
        }

        return results
            .SelectMany(result => (result.MemberNames.Any() ? result.MemberNames : [string.Empty])
                .Select(member => (member, message: result.ErrorMessage ?? "Invalid value")))
            .GroupBy(pair => ToCamelCase(pair.member))
            .ToDictionary(group => group.Key, group => group.Select(pair => pair.message).ToArray());
    }

    private static string ToCamelCase(string value) =>
        string.IsNullOrEmpty(value) ? value : char.ToLowerInvariant(value[0]) + value[1..];
}
