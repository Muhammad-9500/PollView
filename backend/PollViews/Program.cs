using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using PollViews.Data;
using PollViews.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is missing.");
builder.Services.AddDbContext<PollViewsDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddHttpClient();
builder.Services.AddSingleton<AdminPasswordService>();
builder.Services.AddSingleton<SentimentAnalysisService>();
builder.Services.AddScoped<AdminAccountSeeder>();
builder.Services.AddScoped<DemoResponseSeeder>();
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
    policy.WithOrigins("http://localhost:5500", "http://127.0.0.1:5500", "https://localhost:7033", "http://localhost:5211")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()));
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "PollViews.Admin";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.LoginPath = "/admin/login";
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddScoped<InecSeedService>();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// The plain frontend is served locally over HTTP during development. Keep the
// HTTP API available there; production deployments still redirect to HTTPS.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("Frontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var database = scope.ServiceProvider.GetRequiredService<PollViewsDbContext>();
    await database.Database.EnsureCreatedAsync();
    await AdminDatabaseInitializer.EnsureTablesAsync(database);
    await scope.ServiceProvider.GetRequiredService<AdminAccountSeeder>().SeedAsync();
    await scope.ServiceProvider.GetRequiredService<InecSeedService>().SeedAsync();
    if (builder.Configuration.GetValue<bool>("Admin:SeedDemoResponses"))
        await scope.ServiceProvider.GetRequiredService<DemoResponseSeeder>().SeedAsync();
}

app.Run();
