using System.Data;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using thaibev.Models;

namespace thaibev.Data;

public sealed partial class TransportCompanyService
{
    private string TableName => $"{sqlObjects.Prefix}.[tsssTransportCompany]";
    private const string SelectColumns = """
        [ID], [TransportCompanyCode], [TransportCompanyName], [Address1], [PhoneNumber], [Remark],
          [DataStatus], [CreateUserID], [CreateUserDate],
        [UpdateUserID], [UpdateUserDate]
        """;


    public Task<TransportCompanyViewModel> CreateModelAsync(CancellationToken cancellationToken) => Task.FromResult(new TransportCompanyViewModel());

    public async Task<TransportCompanyViewModel?> FindAsync(int id, CancellationToken cancellationToken)
    {
        var table = await dataAccess.SelectAsync(
            $"SELECT {SelectColumns} FROM {TableName} WHERE [ID] = @Id;",
            [new SqlParameter("@Id", SqlDbType.Int) { Value = id }], cancellationToken);
        return table.Rows.Count == 0 ? null : ToModel(table.Rows[0]);
    }



    private static TransportCompanyViewModel ToModel(DataRow row) => new()
    {
        Id = row.Field<int>("ID"),
        TransportCompanyCode = row.Field<string>("TransportCompanyCode") ?? string.Empty,
        TransportCompanyName = row.Field<string>("TransportCompanyName") ?? string.Empty,
        Address1 = row.Field<string>("Address1") ?? string.Empty,
        PhoneNumber = row.Field<string>("PhoneNumber") ?? string.Empty,
        Remark = row.Field<string>("Remark"),
        DataStatus = row.Field<string>("DataStatus") ?? string.Empty,
        CreateUserID = row.Field<int>("CreateUserID"),
        CreateUserDate = row.Field<DateTime>("CreateUserDate"),
        UpdateUserID = row.Field<int?>("UpdateUserID"),
        UpdateUserDate = row.Field<DateTime?>("UpdateUserDate")
    };

}
