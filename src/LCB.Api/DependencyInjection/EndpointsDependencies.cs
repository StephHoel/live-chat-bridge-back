using LCB.Api.Endpoints;

namespace LCB.Api.DependencyInjection;

public static class EndpointsDependencies
{
    public static WebApplication AddEndpoints(this WebApplication app)
    {
        app.MapAuthEndpoints();
        app.MapConfigEndpoints();
        app.MapMessageEndpoints();
        app.MapQueueEndpoints();
        app.MapWorkerEndpoints();

        return app;
    }
}