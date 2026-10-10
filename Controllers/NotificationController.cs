using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using thaibev.Data;
using thaibev.Infrastructure.Api;
using thaibev.Models;

namespace thaibev.Controllers;

public sealed class NotificationController(ISqlDataAccess dataAccess, SqlObjectNames sqlObjects, ISqlConnectionFactory connectionFactory, PaginationOptions pagination) : Controller
{
    private string TableName => $"{sqlObjects.Prefix}.[tsssNotificationH]";
    private const string SelectColumns = """
        [ID], [NotificationNo], [NotificationDate], [NotificationName], [NotificationSubj],
        [NotificationMsg], [TriggerType], [StartDate], [NotificationTime], [DataStatus],
        [CreateUserID], [CreateUserDate], [UpdateUserID], [UpdateUserDate]
        """;

    [HttpGet]
    public IActionResult Index() => View();
    [HttpGet]
    public IActionResult Create() => View(new NotificationViewModel());

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var table = await dataAccess.SelectAsync($"SELECT {SelectColumns} FROM {TableName} WHERE [ID] = @Id;",
            [new("@Id", SqlDbType.Int) { Value = id }], cancellationToken);
        if (table.Rows.Count == 0) return NotFound();
        var model = ToModel(table.Rows[0]);
        model.ToRecipients = await LoadRecipients(id, false, cancellationToken);
        model.CcRecipients = await LoadRecipients(id, true, cancellationToken);
        return View(model);
    }

    private async Task<List<NotificationRecipientViewModel>> LoadRecipients(int id, bool cc, CancellationToken cancellationToken)
    {
        var (table, email, owner) = DetailNames(cc);
        var rows = await dataAccess.SelectAsync(
            $"SELECT [ID], [{email}], [{owner}] FROM {table} WHERE [NotificationHID] = @Id ORDER BY [ID];",
            [new("@Id", SqlDbType.Int) { Value = id }], cancellationToken);
        return rows.Rows.Cast<DataRow>().Select(row => new NotificationRecipientViewModel
        {
            Id = row.Field<int>("ID"), Email = row.Field<string>(email) ?? "", Owner = row.Field<string>(owner) ?? ""
        }).ToList();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Create([FromBody] NotificationViewModel model, CancellationToken cancellationToken)
        => Save(model, false, cancellationToken);

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Edit([FromBody] NotificationViewModel model, CancellationToken cancellationToken)
        => Save(model, true, cancellationToken);

    private async Task<IActionResult> Save(NotificationViewModel model, bool edit, CancellationToken cancellationToken)
    {
        if (!edit) model.DataStatus = "A";
        AuditStamp.Apply(User, model, edit);
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        if (edit && model.Id <= 0) return BadRequest();
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);
        if (edit)
        {
            using var exists = new SqlCommand($"SELECT [ID] FROM {TableName} WITH (UPDLOCK, HOLDLOCK) WHERE [ID] = @Id;", connection, transaction);
            exists.Parameters.Add("@Id", SqlDbType.Int).Value = model.Id;
            if (await exists.ExecuteScalarAsync(cancellationToken) is null) return NotFound();
        }
        using (var duplicates = new SqlCommand($"""
            SELECT
                CASE WHEN EXISTS (SELECT 1 FROM {TableName} WITH (UPDLOCK, HOLDLOCK)
                    WHERE [NotificationNo] = @No AND [DataStatus] = 'A' AND (@Edit = 0 OR [ID] <> @Id)) THEN 1 ELSE 0 END AS NoExists,
                CASE WHEN EXISTS (SELECT 1 FROM {TableName} WITH (UPDLOCK, HOLDLOCK)
                    WHERE [NotificationDate] = @Date AND [DataStatus] = 'A' AND (@Edit = 0 OR [ID] <> @Id)) THEN 1 ELSE 0 END AS DateExists;
            """, connection, transaction))
        {
            duplicates.Parameters.Add("@No", SqlDbType.VarChar, 20).Value = model.NotificationNo;
            duplicates.Parameters.Add("@Date", SqlDbType.DateTime).Value = model.NotificationDate!.Value;
            duplicates.Parameters.Add("@Edit", SqlDbType.Bit).Value = edit;
            duplicates.Parameters.Add("@Id", SqlDbType.Int).Value = model.Id;
            await using var reader = await duplicates.ExecuteReaderAsync(cancellationToken);
            await reader.ReadAsync(cancellationToken);
            if (reader.GetInt32(0) == 1) ModelState.AddModelError(nameof(model.NotificationNo), "เลขที่การแจ้งเตือนซ้ำ");
            if (reader.GetInt32(1) == 1) ModelState.AddModelError(nameof(model.NotificationDate), "วันที่และเวลาการแจ้งเตือนซ้ำ");
        }
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        // Reject stale/foreign detail IDs before modifying any data.
        if (!await ValidateRecipientIds(model.ToRecipients, model.Id, edit, false, connection, transaction, cancellationToken)
            || !await ValidateRecipientIds(model.CcRecipients, model.Id, edit, true, connection, transaction, cancellationToken))
        {
            ModelState.AddModelError("Recipients", "ข้อมูลผู้รับมีการเปลี่ยนแปลงหรือไม่อยู่ในรายการนี้ กรุณาโหลดหน้าใหม่");
            return ValidationProblem(ModelState);
        }
        var sql = edit ? $"""
            UPDATE {TableName} SET [NotificationNo]=@NotificationNo, [NotificationDate]=@NotificationDate,
                [NotificationName]=@NotificationName, [NotificationSubj]=@NotificationSubj,
                [NotificationMsg]=@NotificationMsg, [TriggerType]=@TriggerType, [StartDate]=@StartDate,
                [NotificationTime]=@NotificationTime, [DataStatus]=@DataStatus,

                [UpdateUserID]=@UpdateUserID, [UpdateUserDate]=@UpdateUserDate WHERE [ID]=@Id;
            SELECT @Id;
            """ : $"""
            INSERT INTO {TableName} ([NotificationNo], [NotificationDate], [NotificationName], [NotificationSubj],
                [NotificationMsg], [TriggerType], [StartDate], [NotificationTime], [DataStatus],
                [CreateUserID], [CreateUserDate], [UpdateUserID], [UpdateUserDate])
            VALUES (@NotificationNo, @NotificationDate, @NotificationName, @NotificationSubj,
                @NotificationMsg, @TriggerType, @StartDate, @NotificationTime, @DataStatus,
                @CreateUserID, @CreateUserDate, @UpdateUserID, @UpdateUserDate);
            SELECT CAST(SCOPE_IDENTITY() AS int);
            """;
        using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.AddRange(HeaderParameters(model));
        var id = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
        var now = DateTime.Now;
        await SaveRecipients(model.ToRecipients, id, edit, false, now, connection, transaction, cancellationToken);
        await SaveRecipients(model.CcRecipients, id, edit, true, now, connection, transaction, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Ok(new { id });
    }

    private (string Table, string Email, string Owner) DetailNames(bool cc) => cc
        ? ($"{sqlObjects.Prefix}.[tsssNotificationD2]", "emailCC", "emailCCOwner")
        : ($"{sqlObjects.Prefix}.[tsssNotificationD1]", "emailTo", "emailToOwner");

    private async Task<bool> ValidateRecipientIds(List<NotificationRecipientViewModel> recipients,
        int id, bool edit, bool cc, SqlConnection connection, SqlTransaction transaction, CancellationToken cancellationToken)
    {
        var ids = recipients.Where(row => row.Id != 0).Select(row => row.Id).ToList();
        if (ids.Count != ids.Distinct().Count() || (!edit && ids.Count != 0)) return false;
        var (table, _, _) = DetailNames(cc);
        using var command = new SqlCommand($"SELECT [ID] FROM {table} WITH (UPDLOCK, HOLDLOCK) WHERE [NotificationHID]=@Id;", connection, transaction);
        command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
        var existing = new HashSet<int>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) existing.Add(reader.GetInt32(0));
        return ids.All(existing.Contains);
    }

    private async Task SaveRecipients(List<NotificationRecipientViewModel> recipients, int id, bool edit,
        bool cc, DateTime now, SqlConnection connection, SqlTransaction transaction, CancellationToken cancellationToken)
    {
        var (table, email, owner) = DetailNames(cc);
        var retained = recipients.Where(row => row.Id > 0).Select(row => row.Id).ToHashSet();
        var existing = new List<int>();
        using (var read = new SqlCommand($"SELECT [ID] FROM {table} WHERE [NotificationHID]=@Id;", connection, transaction))
        {
            read.Parameters.Add("@Id", SqlDbType.Int).Value = id;
            await using var reader = await read.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken)) existing.Add(reader.GetInt32(0));
        }
        foreach (var removed in existing.Where(value => !retained.Contains(value)))
        {
            using var delete = new SqlCommand($"DELETE FROM {table} WHERE [ID]=@Id AND [NotificationHID]=@Header;", connection, transaction);
            delete.Parameters.Add("@Id", SqlDbType.Int).Value = removed;
            delete.Parameters.Add("@Header", SqlDbType.Int).Value = id;
            await delete.ExecuteNonQueryAsync(cancellationToken);
        }
        foreach (var recipient in recipients)
        {
            var sql = recipient.Id > 0
                ? $"UPDATE {table} SET [{email}]=@Email, [{owner}]=@Owner, [DataStatus]='A', [UpdateUserID]=@UserID, [UpdateUserDate]=@Now WHERE [ID]=@Id AND [NotificationHID]=@Header;"
                : $"""
                    INSERT INTO {table} ([NotificationHID], [{email}], [{owner}], [DataStatus],
                        [CreateUserID], [CreateUserDate], [UpdateUserID], [UpdateUserDate])
                    VALUES (@Header, @Email, @Owner, 'A', @UserID, @Now, NULL, NULL);
                    """;
            using var command = new SqlCommand(sql, connection, transaction);
            command.Parameters.Add("@Id", SqlDbType.Int).Value = recipient.Id;
            command.Parameters.Add("@Header", SqlDbType.Int).Value = id;
            command.Parameters.Add("@Email", SqlDbType.VarChar, 200).Value = recipient.Email;
            command.Parameters.Add("@Owner", SqlDbType.NVarChar, 200).Value = recipient.Owner;
            command.Parameters.Add("@Now", SqlDbType.DateTime).Value = now;
            command.Parameters.Add("@UserID", SqlDbType.Int).Value = AuditStamp.UserId(User);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    [HttpDelete, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);
        using var command = new SqlCommand($"""
            SELECT [ID] FROM {TableName} WITH (UPDLOCK, HOLDLOCK) WHERE [ID]=@Id;
            """, connection, transaction);
        command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
        if (await command.ExecuteScalarAsync(cancellationToken) is null) return NotFound();
        command.CommandText = $"""
            UPDATE {sqlObjects.Prefix}.[tsssNotificationD1]
            SET [DataStatus]='C', [UpdateUserID]=@UpdateUserID, [UpdateUserDate]=@UpdateUserDate
            WHERE [NotificationHID]=@Id;
            UPDATE {sqlObjects.Prefix}.[tsssNotificationD2]
            SET [DataStatus]='C', [UpdateUserID]=@UpdateUserID, [UpdateUserDate]=@UpdateUserDate
            WHERE [NotificationHID]=@Id;
            UPDATE {TableName}
            SET [DataStatus]='C', [UpdateUserID]=@UpdateUserID, [UpdateUserDate]=@UpdateUserDate
            WHERE [ID]=@Id;
            """;
        command.Parameters.Add("@UpdateUserID", SqlDbType.Int).Value = AuditStamp.UserId(User);
        command.Parameters.Add("@UpdateUserDate", SqlDbType.DateTime).Value = DateTime.Now;
        await command.ExecuteNonQueryAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Ok(new { id });
    }

    private static SqlParameter[] HeaderParameters(NotificationViewModel model) =>
    [
        new("@Id", SqlDbType.Int) { Value = model.Id },
        new("@NotificationNo", SqlDbType.VarChar, 20) { Value = model.NotificationNo },
        new("@NotificationDate", SqlDbType.DateTime) { Value = model.NotificationDate },
        new("@NotificationName", SqlDbType.NVarChar, 150) { Value = model.NotificationName },
        new("@NotificationSubj", SqlDbType.NVarChar, 200) { Value = model.NotificationSubj },
        new("@NotificationMsg", SqlDbType.NVarChar, -1) { Value = (object?)model.NotificationMsg ?? DBNull.Value },
        new("@TriggerType", SqlDbType.Char, 1) { Value = model.TriggerType },
        new("@StartDate", SqlDbType.DateTime) { Value = model.StartDate },
        new("@NotificationTime", SqlDbType.Time) { Scale = 7, Value = model.NotificationTime },
        new("@DataStatus", SqlDbType.Char, 1) { Value = model.DataStatus },
        new("@CreateUserID", SqlDbType.Int) { Value = model.CreateUserID },
        new("@CreateUserDate", SqlDbType.DateTime) { Value = model.CreateUserDate },
        new("@UpdateUserID", SqlDbType.Int) { Value = (object?)model.UpdateUserID ?? DBNull.Value },
        new("@UpdateUserDate", SqlDbType.DateTime) { Value = (object?)model.UpdateUserDate ?? DBNull.Value }
    ];

    private static NotificationViewModel ToModel(DataRow row) => new()
    {
        Id = row.Field<int>("ID"),
        NotificationNo = row.Field<string>("NotificationNo") ?? "",
        NotificationDate = row.Field<DateTime>("NotificationDate"),
        NotificationName = row.Field<string>("NotificationName") ?? "",
        NotificationSubj = row.Field<string>("NotificationSubj") ?? "",
        NotificationMsg = row.Field<string>("NotificationMsg"),
        TriggerType = row.Field<string>("TriggerType") ?? "",
        StartDate = row.Field<DateTime>("StartDate"),
        NotificationTime = row.Field<TimeSpan>("NotificationTime"),
        DataStatus = row.Field<string>("DataStatus") ?? "",
        CreateUserID = row.Field<int>("CreateUserID"),
        CreateUserDate = row.Field<DateTime>("CreateUserDate"),
        UpdateUserID = row.Field<int?>("UpdateUserID"),
        UpdateUserDate = row.Field<DateTime?>("UpdateUserDate")
    };
    [HttpGet]
    public async Task<IActionResult> Rows(string? TextSearch, string? dataStatus, int page = 1,
        int pageSize = 0, CancellationToken cancellationToken = default)
    {
        var result = await LoadRowsAsync(TextSearch, dataStatus, page, pageSize, cancellationToken);
        return Json(new { rows = result.Rows, page = result.Page, pageSize = result.PageSize,
            totalCount = result.TotalCount, totalPages = result.TotalPages });
    }

    [HttpGet("/v1/Setup/Notification/Search"), ApiAccess]
    public async Task<IActionResult> Search(string? TextSearch, string? DataStatus, int Page = 1,
        int PerPage = 10, CancellationToken cancellationToken = default)
    {
        if (Page < 1 || PerPage is < 1 or > 1000 || DataStatus is not (null or "A" or "C" or "L"))
            return ApiResponse.Failure("Invalid search parameters.", true).ToResult(400);
        var result = await LoadRowsAsync(TextSearch, DataStatus, Page, PerPage, cancellationToken);
        return ApiResponse.Success(result.Rows, new Dictionary<string, object?>
        {
            ["Pagination"] = new { result.Page, PerPage = result.PageSize, result.TotalCount, result.TotalPages }
        }).ToResult();
    }

    private async Task<(List<NotificationViewModel> Rows, int Page, int PageSize, int TotalCount, int TotalPages)>
        LoadRowsAsync(string? search, string? dataStatus, int page, int pageSize, CancellationToken cancellationToken)
    {
        const string whereClause = """
            WHERE (@Search = N'' OR [NotificationNo] LIKE @LikeSearch OR [NotificationName] LIKE @LikeSearch)
              AND (@DataStatus = N'L' OR [DataStatus] = @DataStatus)
            """;
        var value = search?.Trim() ?? string.Empty;
        var status = dataStatus?.Trim().ToUpperInvariant();
        status = status is "A" or "C" ? status : "L";
        pageSize = pagination.Normalize(pageSize);
        SqlParameter[] FilterParameters() =>
        [
            new SqlParameter("@Search", SqlDbType.NVarChar, -1) { Value = value },
            new SqlParameter("@LikeSearch", SqlDbType.NVarChar, -1) { Value = $"%{value}%" },
            new SqlParameter("@DataStatus", SqlDbType.Char, 1) { Value = status }
        ];
        var countTable = await dataAccess.SelectAsync(
            $"SELECT COUNT(*) FROM {TableName} {whereClause};", FilterParameters(), cancellationToken);
        var totalCount = Convert.ToInt32(countTable.Rows[0][0]);
        var totalPages = Math.Max((int)Math.Ceiling(totalCount / (double)pageSize), 1);
        page = Math.Clamp(page, 1, totalPages);
        var parameters = FilterParameters().Concat(new[]
        {
            new SqlParameter("@Offset", SqlDbType.Int) { Value = (page - 1) * pageSize },
            new SqlParameter("@PageSize", SqlDbType.Int) { Value = pageSize }
        }).ToArray();
        var table = await dataAccess.SelectAsync($"""
            SELECT {SelectColumns} FROM {TableName} {whereClause}
            ORDER BY [ID] DESC OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """, parameters, cancellationToken);
        return (table.Rows.Cast<DataRow>().Select(ToModel).ToList(), page, pageSize, totalCount, totalPages);
    }

}
