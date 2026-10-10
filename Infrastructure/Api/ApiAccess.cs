using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace thaibev.Infrastructure.Api;

public sealed class ApiOptions
{
    public Dictionary<string, ApiRouteOptions> Routes { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public bool AllowsExternal(string? route) => route is not null
        && Routes.TryGetValue("/" + route.Trim('/'), out var settings) && settings.ExternalEnabled;
}

public sealed class ApiRouteOptions
{
    public bool ExternalEnabled { get; set; }
    public bool ValidateRequestMetadata { get; set; } = true;
}

// The filter handles authentication itself, instead of the application's cookie-only fallback policy.
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true)]
public sealed class ApiAccessAttribute : Attribute, IFilterFactory, IAllowAnonymous
{
    public bool AllowUnauthenticatedExternal { get; set; }
    public bool IsReusable => false;
    public IFilterMetadata CreateInstance(IServiceProvider serviceProvider) => serviceProvider.GetRequiredService<ApiAccessFilter>();
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true)]
public sealed class ExternalOnlyAttribute : Attribute { }

public sealed class ApiAccessFilter(IOptionsMonitor<ApiOptions> options,
    ExternalApiSecurity externalSecurity, IAntiforgery antiforgery) : IAsyncAuthorizationFilter
{
    public const string ExternalKey = "Api.External";

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var http = context.HttpContext;
        var syncDate = DateTime.Now;
        var metadata = context.ActionDescriptor.EndpointMetadata;
        if (metadata.OfType<ApiAccessAttribute>().LastOrDefault()?.AllowUnauthenticatedExternal == true)
        {
            if (!options.CurrentValue.AllowsExternal(context.ActionDescriptor.AttributeRouteInfo?.Template))
                context.Result = ApiResponse.Failure("External access is disabled for this routing path.", HttpMethods.IsGet(http.Request.Method)).ToResult(403);
            else http.Items[ExternalKey] = true;
            return;
        }
        var externalOnly = metadata.OfType<ExternalOnlyAttribute>().Any();
        var hasExternalHeaders = http.Request.Headers.ContainsKey("AccessToken") || http.Request.Headers.ContainsKey("Token");
        if (!externalOnly && !hasExternalHeaders)
        {
            var cookie = await http.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (cookie.Succeeded && cookie.Principal is not null)
            {
                http.User = cookie.Principal;
                http.Items[ExternalKey] = false;
                if (!HttpMethods.IsGet(http.Request.Method) && !HttpMethods.IsHead(http.Request.Method) && !HttpMethods.IsOptions(http.Request.Method))
                {
                    try { await antiforgery.ValidateRequestAsync(http); }
                    catch (AntiforgeryValidationException)
                    {
                        context.Result = ApiResponse.Failure("Invalid or missing antiforgery token.", HttpMethods.IsGet(http.Request.Method)).ToResult(400);
                    }
                }
                return;
            }
        }
        if (!options.CurrentValue.AllowsExternal(context.ActionDescriptor.AttributeRouteInfo?.Template))
        {
            context.Result = ApiResponse.Failure("External access is disabled for this routing path.", HttpMethods.IsGet(http.Request.Method)).ToResult(403);
            return;
        }
        var route = "/" + context.ActionDescriptor.AttributeRouteInfo!.Template!.Trim('/');
        http.Items[ExternalApiRequest.ContextKey] = await ExternalApiRequest.ReadAsync(http, syncDate,
            options.CurrentValue.Routes[route]);
        await externalSecurity.ValidateTokenAsync(http);
        http.Items[ExternalKey] = true;
    }
}
