using ApiService.Handlers;

namespace ApiService;

public static class RouteConfig
{
    public static void MapEndpoints(this WebApplication app)
    {
        MapTestEndpoints(app);
        MapAiEndpoints(app);
    }

    public static void MapTestEndpoints(WebApplication app)
    {
        app.MapGet("/test", async (context) =>
        {
            context.Response.ContentType = "text/plain;charset=utf-8";
            await context.Response.WriteAsync("Hello World!");
        });
    }

    public static void MapAiEndpoints(WebApplication app)
    {
        var group = app.MapGroup("ai");
        group.MapPost("/search", SLMHandler.SearchAsync);
        group.MapPost("/ner", SLMHandler.NerAsync);
        group.MapPost("/relation", SLMHandler.RelationExtractionAsync);
    }

}
