using System.Data;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using thaibev.Models;

namespace thaibev.Data;

public sealed partial class ProgramTypeService
{
    private string TableName => $"{sqlObjects.Prefix}.[tsssProgramType]";
    private const string SelectColumns = """
        [ID], [ProgramTypeCode], [ProgramTypeName],   
          [DataStatus], [CreateUserID], [CreateUserDate],
        [UpdateUserID], [UpdateUserDate]
        """;


    public Task<ProgramTypeViewModel> CreateModelAsync(CancellationToken cancellationToken) => Task.FromResult(new ProgramTypeViewModel());

    public async Task<ProgramTypeViewModel?> FindAsync(int id, CancellationToken cancellationToken)
    {
        var table = await dataAccess.SelectAsync(
            $"SELECT {SelectColumns} FROM {TableName} WHERE [ID] = @Id;",
            [new SqlParameter("@Id", SqlDbType.Int) { Value = id }], cancellationToken);
        return table.Rows.Count == 0 ? null : ToModel(table.Rows[0]);
    }



    private static ProgramTypeViewModel ToModel(DataRow row) => new()
    {
        Id = row.Field<int>("ID"),
        ProgramTypeCode = row.Field<string>("ProgramTypeCode") ?? string.Empty,
        ProgramTypeName = row.Field<string>("ProgramTypeName") ?? string.Empty,
        DataStatus = row.Field<string>("DataStatus") ?? string.Empty,
        CreateUserID = row.Field<int>("CreateUserID"),
        CreateUserDate = row.Field<DateTime>("CreateUserDate"),
        UpdateUserID = row.Field<int?>("UpdateUserID"),
        UpdateUserDate = row.Field<DateTime?>("UpdateUserDate")
    };

}
