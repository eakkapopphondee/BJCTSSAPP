using System.Data;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using thaibev.Models;

namespace thaibev.Data;

public sealed partial class CustomerService
{
    private string TableName => $"{sqlObjects.Prefix}.[tsssCustomer]";
    private const string SelectColumns = """
        [ID], [CustomerCode], [CustomerName], [Address1], [PhoneNumber], [SAPCode], [SAPDesc], [Remark],
        [MolasFlag], [TBRFlag], [DefaultFlag], [DataStatus], [CreateUserID], [CreateUserDate],
        [UpdateUserID], [UpdateUserDate]
        """;


    public Task<CustomerViewModel> CreateModelAsync(CancellationToken cancellationToken) => Task.FromResult(new CustomerViewModel());

    public async Task<CustomerViewModel?> FindAsync(int id, CancellationToken cancellationToken)
    {
        var table = await dataAccess.SelectAsync(
            $"SELECT {SelectColumns} FROM {TableName} WHERE [ID] = @Id;",
            [new SqlParameter("@Id", SqlDbType.Int) { Value = id }], cancellationToken);
        return table.Rows.Count == 0 ? null : ToModel(table.Rows[0]);
    }



    private static CustomerViewModel ToModel(DataRow row) => new()
    {
        Id = row.Field<int>("ID"),
        CustomerCode = row.Field<string>("CustomerCode") ?? string.Empty,
        CustomerName = row.Field<string>("CustomerName") ?? string.Empty,
        Address1 = row.Field<string>("Address1") ?? string.Empty,
        PhoneNumber = row.Field<string>("PhoneNumber") ?? string.Empty,
        SAPCode = row.Field<string>("SAPCode"),
        SAPDesc = row.Field<string>("SAPDesc"),
        Remark = row.Field<string>("Remark"),
        MolasFlag = row.Field<string>("MolasFlag") ?? string.Empty,
        TBRFlag = row.Field<string>("TBRFlag") ?? string.Empty,
        DefaultFlag = row.Field<string>("DefaultFlag") ?? "N",
        DataStatus = row.Field<string>("DataStatus") ?? string.Empty,
        CreateUserID = row.Field<int>("CreateUserID"),
        CreateUserDate = row.Field<DateTime>("CreateUserDate"),
        UpdateUserID = row.Field<int?>("UpdateUserID"),
        UpdateUserDate = row.Field<DateTime?>("UpdateUserDate")
    };

}
