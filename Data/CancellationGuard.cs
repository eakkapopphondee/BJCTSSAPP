using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace thaibev.Data;

public static class CancellationGuard
{
    public const int Blocked = -2;

    public static BadRequestObjectResult ValidationError(string entity) => new(new
    {
        errors = new Dictionary<string, string[]>
        {
            ["DataStatus"] = ["ไม่สามารถปรับเป็นสถานะยกเลิกได้เนื่องจากข้อมูลใช้งานอยู่กับ " + (entity switch
            {
                "ProgramType" => "โรงงาน/บริษัท",
                "ProductType" => "ทะเบียนสินค้า",
                "Product" => "ทะเบียนรถ รายการสินค้า",
                "Factory" => "ตั่งค่า IP หรือ ตั่งค่า URL",
                _ => "ทะเบียนรถ"
            })]
        }
    });

    public static async Task<int> UpdateWithCancellationGuardAsync(this ISqlDataAccess dataAccess, SqlObjectNames sqlObjects,
        string entity, string sql, SqlParameter[] parameters, CancellationToken cancellationToken)
    {
        var status = parameters.FirstOrDefault(parameter => parameter.ParameterName == "@DataStatus");
        // Delete actions always cancel; edits only need the guard when saving status C.
        if (status is not null && !string.Equals(Convert.ToString(status.Value)?.Trim(), "C", StringComparison.OrdinalIgnoreCase))
            return await dataAccess.UpdateAsync(sql, parameters, cancellationToken);

        var dependency = entity switch
        {
            "ProgramType" => $"EXISTS (SELECT 1 FROM {sqlObjects.Prefix}.[tsssFactory] WITH (UPDLOCK, HOLDLOCK) WHERE [ProgramTypeID] = @Id AND [DataStatus] = 'A')",
            "TruckType" => LicensePlate(sqlObjects, "[TruckTypeID] = @Id"),
            "Province" => LicensePlate(sqlObjects, "([PlateProvinceID] = @Id OR [TrailerProviceID] = @Id)"),
            "TransportCompany" => LicensePlate(sqlObjects, "[TransportCompanyID] = @Id"),
            "ProductType" => $"EXISTS (SELECT 1 FROM {sqlObjects.Prefix}.[tsssProduct] WITH (UPDLOCK, HOLDLOCK) WHERE [ProductTypeID] = @Id AND [DataStatus] = 'A')",
            "Product" => $"EXISTS (SELECT 1 FROM {sqlObjects.Prefix}.[tsssLicensePlateProduct] WITH (UPDLOCK, HOLDLOCK) WHERE [ProductID] = @Id AND [DataStatus] = 'A')",
            "Factory" => FactoryDependency(sqlObjects, status is not null),
            _ => throw new ArgumentOutOfRangeException(nameof(entity))
        };
        var guardedSql = $"""
            SET XACT_ABORT ON;
            BEGIN TRY
                BEGIN TRANSACTION;
                IF {dependency}
                    THROW 50003, 'Active dependent records prevent cancellation', 1;
                {sql}
                COMMIT TRANSACTION;
            END TRY
            BEGIN CATCH
                IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
                THROW;
            END CATCH;
            """;
        try
        {
            return await dataAccess.UpdateAsync(guardedSql, parameters, cancellationToken);
        }
        catch (SqlException exception) when (exception.Number == 50003)
        {
            return Blocked;
        }
    }

    private static string LicensePlate(SqlObjectNames sqlObjects, string predicate) =>
        $"EXISTS (SELECT 1 FROM {sqlObjects.Prefix}.[tsssLicensePlate] WITH (UPDLOCK, HOLDLOCK) WHERE {predicate} AND [DataStatus] = 'A')";

    private static string FactoryDependency(SqlObjectNames sqlObjects, bool edit)
    {
        // Read the persisted code by ID so changing the code in the same edit cannot bypass validation.
        var code = $"d.[FactoryCode] = (SELECT [FactoryCode] FROM {sqlObjects.Prefix}.[tsssFactory] WITH (UPDLOCK, HOLDLOCK) WHERE [ID] = @Id)";
        if (edit) code = $"({code} OR d.[FactoryCode] = @FactoryCode)";
        return $"""
            EXISTS (SELECT 1 FROM {sqlObjects.Prefix}.[tsssConstant] d WITH (UPDLOCK, HOLDLOCK)
                WHERE {code} AND d.[DataStatus] = 'A')
            OR EXISTS (SELECT 1 FROM {sqlObjects.Prefix}.[tsssURLSetting] d WITH (UPDLOCK, HOLDLOCK)
                WHERE {code} AND d.[DataStatus] = 'A')
            """;
    }
}
