using AppSugerenciasUI;
using AppSugerenciasUI.Api;
using Scalar.AspNetCore;
using Microsoft.AspNetCore.Rewrite;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureServices();

var app = builder.Build();

// La página de login de Azure AD B2C carga la plantilla de /b2c desde su propio dominio.
// Va primero para que el header también salga en redirecciones y páginas de error.
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/b2c"))
    {
        string b2c = app.Configuration["AzureAdB2C:Instance"]?.TrimEnd('/');
        if (string.IsNullOrEmpty(b2c) == false)
        {
            context.Response.Headers.AccessControlAllowOrigin = b2c;
            if (HttpMethods.IsOptions(context.Request.Method))
            {
                context.Response.Headers.AccessControlAllowMethods = "GET";
                context.Response.Headers.AccessControlAllowHeaders = context.Request.Headers.AccessControlRequestHeaders;
                context.Response.StatusCode = StatusCodes.Status204NoContent;
                return;
            }
        }
    }

    await next();
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/no-encontrado", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseRewriter(
    new RewriteOptions().Add(
    context =>
    {
        if (context.HttpContext.Request.Path == "/MicrosoftIdentity/Account/SignedOut")
        {
            context.HttpContext.Response.Redirect("/");
        }
    }
    ));

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapControllers();
app.MapRazorPages();
app.MapSugerenciasApi();
app.MapOpenApi();
app.MapScalarApiReference("/api/docs", options => options.WithTitle("API de Sugerencias"));
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

public partial class Program;
