using thaibev.Infrastructure.Api;
using thaibev.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);
var cookieExpirationMinutes = builder.Configuration.GetValue<int>("Authentication:CookieExpirationMinutes");
if (cookieExpirationMinutes <= 0)
{
    throw new InvalidOperationException("Authentication:CookieExpirationMinutes must be greater than zero.");
}

// Add services to the container.
var pagination = builder.Configuration.GetSection("Pagination").Get<thaibev.Models.PaginationOptions>()
    ?? throw new InvalidOperationException("Pagination configuration is required.");
pagination.Validate();
builder.Services.AddSingleton(pagination);
builder.Services.AddSingleton(new SqlObjectNames(builder.Configuration));
builder.Services.AddControllersWithViews();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.Cookie.Name = "TBR.Auth";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(cookieExpirationMinutes);
        options.SlidingExpiration = false;
        options.Events.OnRedirectToLogin = context =>
        {
            if (ApiErrors.IsDataRequest(context.Request)) context.Response.StatusCode = 401;
            else context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            if (ApiErrors.IsDataRequest(context.Request)) context.Response.StatusCode = 403;
            else context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
        options.Events.OnValidatePrincipal = async context =>
        {
            if (!int.TryParse(context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                    out var userId) || userId <= 0 ||
                !context.Principal!.HasClaim(claim => claim.Type == UserAuthentication.RoleIdClaim))
            {
                context.RejectPrincipal();
                await Microsoft.AspNetCore.Authentication.AuthenticationHttpContextExtensions.SignOutAsync(
                    context.HttpContext, CookieAuthenticationDefaults.AuthenticationScheme);
            }
        };
    });
builder.Services.AddAuthorization(options =>
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
builder.Services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
builder.Services.AddScoped<ISqlDataAccess, SqlDataAccess>();
builder.Services.AddScoped<UserAuthentication>();
builder.Services.AddScoped<PasswordHash>();
builder.Services.AddScoped<AccessTokenService>();
builder.Services.AddScoped<OauthTokenkeyService>();
builder.Services.Configure<OauthTokenkeyOptions>(builder.Configuration.GetSection("Oauth:Tokenkey"));
builder.Services.AddScoped<FactoryLogoStorage>();
builder.Services.Configure<ApiOptions>(builder.Configuration.GetSection("Api"));
builder.Services.AddScoped<ExternalApiSecurity>();
builder.Services.AddScoped<ExternalApiAudit>();
builder.Services.AddScoped<ApiAccessFilter>();
builder.Services.AddScoped<FactoryService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<RoleService>();
builder.Services.AddScoped<PermissionService>();
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<TransportCompanyService>();
builder.Services.AddScoped<TruckTypeService>();
builder.Services.AddScoped<ProvinceService>();
builder.Services.AddScoped<LicensePlateService>();
builder.Services.AddScoped<LicensePlateProductService>();
builder.Services.AddScoped<ProductTypeService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<ConstantService>();
builder.Services.AddScoped<URLSettingService>();
builder.Services.AddScoped<WeightTypeService>();
builder.Services.AddScoped<ProgramTypeService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
// Uploaded logos are created at runtime, outside the static asset manifest.
app.UseStaticFiles();
app.UseRouting();
app.UseMiddleware<ApiErrors>();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets().AllowAnonymous();
app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
