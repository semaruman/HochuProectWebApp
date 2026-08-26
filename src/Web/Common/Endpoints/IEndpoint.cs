namespace Web.Common.Endpoints;

public interface IEndpoint
{
    void Map(IEndpointRouteBuilder app);
}

public static class EndpointRegistration
{
    public static void MapFeatureEndpoints(this IEndpointRouteBuilder app)
    {
        var endpoints = typeof(IEndpoint).Assembly
            .GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false } && typeof(IEndpoint).IsAssignableFrom(t));

        foreach (var type in endpoints)
        {
            var endpoint = (IEndpoint)Activator.CreateInstance(type)!;
            endpoint.Map(app);
        }
    }
}
