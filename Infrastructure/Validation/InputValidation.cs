using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json;

namespace thaibev.Infrastructure.Validation;

// Stateless validation utilities; table-specific requirements remain in each model/service.
public static class InputValidation
{
    public static void ValidateModel(object value)
    {
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(value, new ValidationContext(value), errors, true))
            throw new ValidationException(string.Join(" ", errors.Select(error => error.ErrorMessage)));
    }

    public static Dictionary<string, JsonElement> ReadObject(JsonElement body)
    {
        if (body.ValueKind != JsonValueKind.Object) throw new ValidationException("A JSON object is required.");
        var input = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in body.EnumerateObject())
            if (!input.TryAdd(property.Name, property.Value)) throw new ValidationException($"Duplicate field: {property.Name}.");
        return input;
    }

    public static void ValidateSqlDateTime(DateTime date, string field)
    {
        if (date < new DateTime(1753, 1, 1) || date > new DateTime(9999, 12, 31, 23, 59, 59, 997))
            throw new ValidationException($"{field} must be a valid SQL datetime.");
    }

    public static int? OptionalInt(object? value, string field) => string.IsNullOrWhiteSpace(value?.ToString()) ? null
        : int.TryParse(value.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) ? number
        : throw new ValidationException($"{field} must be an integer or null.");

    // Parsing and SQL range validation are separate so this parser is useful for other date types too.
    public static DateTime? OptionalDate(object? value, string field) => string.IsNullOrWhiteSpace(value?.ToString()) ? null
        : DateTime.TryParse(value.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var date) ? date
        : throw new ValidationException($"{field} must be a datetime or null.");
}
