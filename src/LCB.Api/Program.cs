using LCB.Api.DependencyInjection;
using LCB.Api.Endpoints;
using LCB.Api.Extensions;
using LCB.Application.DependencyInjection;

namespace LCB.Api;

public class Program
{
    private static void Main(string[] args)
    {
var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddLogging(builder.Configuration);

        builder.Services.ConfigureLogging();
        builder.Services.AddJwtAuthentication(builder.Configuration);
        builder.Services.AddHandlers();
        builder.Services.AddRepositories();
        builder.Services.AddServices();
        builder.Services.AddSwagger();

builder.Services.AddAuthorization();
builder.Services.AddControllers();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.UseDevelopSwagger();

app.MapAuthEndpoints();

app.Run();
    }
}