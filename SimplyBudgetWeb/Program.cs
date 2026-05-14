using SimplyBudgetWeb.Core;
using SimplyBudgetWeb.Middleware;

using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults()
    .AddDatabase();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

var app = builder.Build();

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

app.MapControllers();

if (!app.Environment.IsDevelopment())
{
    app.MapFallbackToFile("index.html");
}

app.Run();
