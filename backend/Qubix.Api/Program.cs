using Microsoft.AspNetCore.Identity;
using Qubix.Api.ErrorHandling;
using Qubix.Api.RealTime;
using Qubix.Infrastructure;
using Qubix.Infrastructure.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff zzz ";
});

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddApiErrorHandling();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "qubix.auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Configuration.GetValue("Identity:SeedRolesOnStartup", true))
{
    await app.Services.SeedIdentityRolesAsync();
}

// Configure the HTTP request pipeline.
app.UseExceptionHandler();
app.UseApiStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.DocumentTitle = "Qubix API";
        options.SwaggerEndpoint("/openapi/v1.json", "Qubix API v1");
    });
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<QuizHub>("/hubs/quiz");

app.Run();

public partial class Program
{
}
