using CollabEditor.API.HealthChecks;
using CollabEditor.API.WebSockets;
using CollabEditor.Application;
using CollabEditor.Infrastructure;
using CollabEditor.Messaging;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add CORS for frontend
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:3000" };

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Add Application and Infrastructure layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddMessaging(builder.Configuration);
builder.Services.AddAppHealthChecks(builder.Configuration); // Health checks must come after messaging

var app = builder.Build();

// Enable WebSockets
app.UseWebSockets();

// Add WebSocket middleware
app.UseMiddleware<WebSocketMiddleware>();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthorization();
app.MapControllers();
app.MapAppHealthChecks();

// Health check endpoints


app.Run();