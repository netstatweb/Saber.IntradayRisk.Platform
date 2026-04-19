using Saber.Risk.Api.Hubs;
using Saber.Risk.Api.Services;
using Saber.Risk.Core.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. Kontrolery i Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 2. SignalR - Serce komunikacji Real-Time
builder.Services.AddSignalR();

// 3. CORS - Rozszerzony pod React (localhost:3000) i WPF
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3000") // Adres Twojego Reacta
              .SetIsOriginAllowed(_ => true)       // Obsługa WPF
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// 4. Rejestracja usług biznesowych i Algo
builder.Services.AddScoped<IRiskDataService, SqlRiskDataService>();
builder.Services.AddHostedService<MarketSimulator>();

// DODAJEMY: Silnik Algo (nasz mózg handlujący)
// builder.Services.AddHostedService<AlgoTradingEngine>(); 

// Konfiguracja uwierzytelniania JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };

        // Specjalna konfiguracja dla SignalR (przekazywanie tokena w query string)
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// 5. Middleware Pipeline - KOLEJNOŚĆ JEST KLUCZOWA!
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// 1. CORS zawsze pierwszy
app.UseCors();

// 2. Authentication przed Authorization
app.UseAuthentication();
app.UseAuthorization();

// 6. Endpointy i Huby
app.MapControllers();
app.MapHub<RiskHub>("/hubs/risk");

app.Run();