using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using thaibev.Infrastructure.Validation;

namespace thaibev.Data;

// Converts values only: no table selection, queries, CRUD or business rules.
public static class SqlValues
{
    public static Dictionary<string, object?> ToRecord(DataRow row) => row.Table.Columns.Cast<DataColumn>()
        .ToDictionary(column => column.ColumnName, column => row.IsNull(column) ? null : row[column]);

    public static object? Read(SqlColumn column, JsonElement value)
    {
        if (value.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            if (!column.Nullable) throw new ValidationException($"{column.Name} is required.");
            return null;
        }
        switch (column.Type)
        {
            case SqlDbType.Int:
                if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var integer)) return integer;
                break;
            case SqlDbType.Decimal:
                if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number))
                {
                    var limit = (decimal)Math.Pow(10, column.Precision - column.Scale);
                    if (number > -limit && number < limit && decimal.Round(number, column.Scale) == number) return number;
                }
                break;
            case SqlDbType.DateTime:
                if (value.ValueKind == JsonValueKind.String && value.TryGetDateTime(out var date))
                {
                    InputValidation.ValidateSqlDateTime(date, column.Name);
                    return date;
                }
                break;
            default:
                if (value.ValueKind == JsonValueKind.String)
                {
                    var text = value.GetString()!;
                    if (text.Length <= column.Length && (column.Nullable || !string.IsNullOrWhiteSpace(text))) return text;
                }
                break;
        }
        throw new ValidationException($"{column.Name} has an invalid type, length, precision or value.");
    }

    public static SqlParameter Parameter(SqlColumn column, object? value)
    {
        var parameter = new SqlParameter("@" + column.Name, column.Type) { Value = value ?? DBNull.Value };
        if (column.Length > 0) parameter.Size = column.Length;
        if (column.Type == SqlDbType.Decimal) { parameter.Precision = column.Precision; parameter.Scale = column.Scale; }
        return parameter;
    }
}
