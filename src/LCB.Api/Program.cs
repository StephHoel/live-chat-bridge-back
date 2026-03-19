using LCB.Api.Endpoints;
using LCB.Api.Extensions;
using LCB.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddJwtAuthentication()
    .AddHandlers()
    .AddInfrastructure()
    .AddSwagger();

builder.Services.AddAuthorization();
builder.Services.AddControllers();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.UseDevelopSwagger();

app.MapAuthEndpoints();

app.Run();