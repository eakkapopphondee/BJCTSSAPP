using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using thaibev.Data;

namespace thaibev.Controllers;

public sealed class ApiLogReportController(ISqlDataAccess dataAccess, SqlObjectNames sqlObjects) : Controller
{
    [HttpGet]
    public IActionResult Index() => View();

    [HttpGet]
    public async Task<IActionResult> Search(DateTime? dateFrom, DateTime? dateTo, bool advanced = false,
        string? apiEndPoint = null, string? requestRefKey = null, string? responseRefKey = null,
        string? requestBody = null, string? responseStatus = null, int page = 1, int perPage = 20,
        CancellationToken cancellationToken = default)
    {
        if (dateFrom?.Date > dateTo?.Date) return BadRequest(new { Message = "วันที่เริ่มต้นต้องไม่เกินวันที่สิ้นสุด" });
        if (dateFrom?.Date < new DateTime(1753, 1, 1) || dateTo?.Date < new DateTime(1753, 1, 1)
            || dateTo?.Date >= DateTime.MaxValue.Date)
            return BadRequest(new { Message = "Invalid date range." });
        if (page < 1 || perPage is < 1 or > 100) return BadRequest(new { Message = "Invalid pagination." });
        var filters = new List<string>();
        var parameters = new List<SqlParameter>();
        if (dateFrom.HasValue)
        {
            filters.Add("[RequestDate] >= @DateFrom");
            parameters.Add(new("@DateFrom", SqlDbType.DateTime) { Value = dateFrom.Value.Date });
        }
        if (dateTo.HasValue)
        {
            filters.Add("[RequestDate] < @DateToExclusive");
            parameters.Add(new("@DateToExclusive", SqlDbType.DateTime) { Value = dateTo.Value.Date.AddDays(1) });
        }
        if (advanced)
        {
            AddContains("APIEndPoint", apiEndPoint);
            AddContains("RequestRefKey", requestRefKey);
            AddContains("ResponseRefKey", responseRefKey);
            AddContains("RequestBody", requestBody);
            AddContains("ResponseStatus", responseStatus);
        }
        void AddContains(string column, string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            var parameter = "@" + column;
            filters.Add($"[{column}] LIKE {parameter} ESCAPE '\\'");
            var escaped = value.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_").Replace("[", "\\[");
            parameters.Add(new(parameter, SqlDbType.NVarChar) { Value = $"%{escaped}%" });
        }
        var table = $"{sqlObjects.Prefix}.[tsssAPILog]";
        var where = filters.Count == 0 ? "" : "WHERE " + string.Join(" AND ", filters);
        var countParameters = parameters.Select(parameter => (SqlParameter)((ICloneable)parameter).Clone()).ToArray();
        var countTable = await dataAccess.SelectAsync($"SELECT COUNT_BIG(*) AS [Total] FROM {table} {where};",
            countParameters, cancellationToken);
        var total = Convert.ToInt64(countTable.Rows[0]["Total"]);
        parameters.Add(new("@Offset", SqlDbType.Int) { Value = (page - 1) * perPage });
        parameters.Add(new("@PerPage", SqlDbType.Int) { Value = perPage });
        var rows = await dataAccess.SelectAsync($"""
            SELECT [RequestDate], [APIEndPoint], [ResponseStatus], [ResponseStatusCode] AS [ResponseCode],
                   [ResponseMessage], [ResponseRefKey], [RequestRefKey], [RequestBody], [ResponseData]
            FROM {table} {where}
            ORDER BY [RequestDate] DESC, [ID] DESC
            OFFSET @Offset ROWS FETCH NEXT @PerPage ROWS ONLY;
            """, parameters.ToArray(), cancellationToken);
        return Json(new
        {
            Rows = rows.Rows.Cast<DataRow>().Select(row => new
            {
                RequestDate = row.Field<DateTime?>("RequestDate"),
                APIEndPoint = row.Field<string>("APIEndPoint"),
                ResponseStatus = row.Field<string>("ResponseStatus"),
                ResponseCode = row.Field<string>("ResponseCode"),
                ResponseMessage = row.Field<string>("ResponseMessage"),
                ResponseRefKey = row.Field<string>("ResponseRefKey"),
                RequestRefKey = row.Field<string>("RequestRefKey"),
                RequestBody = row.Field<string>("RequestBody"),
                ResponseData = row.Field<string>("ResponseData")
            }),
            Pagination = new { Page = page, PerPage = perPage, TotalCount = total,
                TotalPages = Math.Max(1, (long)Math.Ceiling((double)total / perPage)) }
        });
    }
}
