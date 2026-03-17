using LCB.Api.DependencyInjection;
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
app.MapControllers();

app.Run();