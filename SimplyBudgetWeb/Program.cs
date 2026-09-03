using SimplyBudgetWeb.Core;
using SimplyBudgetWeb.Hubs;
using SimplyBudgetWeb.Middleware;
using SimplyBudgetWeb.Services;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults()
    .AddDatabase()
    .AddStartupWarmup();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddMemoryCache();

// Swagger is only mapped in Development, so avoid paying to build the generator and its
// document/schema services on a production cold start.
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
}

// Add CORS for frontend in development
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.SetIsOriginAllowed(origin => new Uri(origin).Host == "localhost")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
        else
        {
            var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
                ?? ["https://yourdomain.com"];
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
    });
});

// Entra ID authentication via Microsoft.Identity.Web
builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration, "AzureAd");
builder.Services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.Events ??= new JwtBearerEvents();
    var existingOnMessageReceived = options.Events.OnMessageReceived;

    options.Events.OnMessageReceived = async context =>
    {
        if (existingOnMessageReceived is not null)
        {
            await existingOnMessageReceived(context);
        }

        if (!string.IsNullOrEmpty(context.Token))
        {
            return;
        }

        var accessToken = context.Request.Query["access_token"];
        if (string.IsNullOrEmpty(accessToken))
        {
            return;
        }

        if (context.HttpContext.Request.Path.StartsWithSegments(BudgetMonthHub.HubPath))
        {
            context.Token = accessToken;
        }
    };
});

// Authorization: restrict to the SimplyBudgetUsers Entra security group
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SimplyBudgetUsers", policy =>
    {
        // Require membership in the SimplyBudgetUsers Entra group.
        // The group Object ID is read from configuration so it can be set per environment.
        var groupId = builder.Configuration["Authorization:SimplyBudgetUsersGroupId"]
            ?? throw new InvalidOperationException("Authorization:SimplyBudgetUsersGroupId is not configured.");
        policy.RequireClaim("groups", groupId);
    });

    // Make SimplyBudgetUsers the default policy so all [Authorize] controllers are covered.
    options.DefaultPolicy = options.GetPolicy("SimplyBudgetUsers")!;
    options.FallbackPolicy = options.GetPolicy("SimplyBudgetUsers")!;
});

builder.Services.AddScoped<CurrentUserSyncService>();
builder.Services.AddScoped<IBudgetMonthUpdateNotifier, BudgetMonthUpdateNotifier>();
builder.Services.AddSingleton<IBudgetMonthDataCache, BudgetMonthDataCache>();

var app = builder.Build();

app.LogStartupTimings();

app.MapDefaultEndpoints();

// Enable CORS
app.UseCors("AllowFrontend");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseMigrationsEndPoint();
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseDefaultFiles();
    app.UseStaticFiles();
}

app.UseAuthentication();
app.UseAuthorization();

// Record/refresh the signed-in user as a pending-expense assignee so the "assign to" list on
// the Pending Expenses page is always the set of people who have logged in.
app.UseMiddleware<CurrentUserSyncMiddleware>();

app.MapControllers();
app.MapHub<BudgetMonthHub>(BudgetMonthHub.HubPath);

if (!app.Environment.IsDevelopment())
{
    app.MapFallbackToFile("index.html");
}

app.Run();
