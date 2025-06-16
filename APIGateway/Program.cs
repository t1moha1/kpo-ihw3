using Yarp.ReverseProxy;
using Yarp.ReverseProxy.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromMemory(
        new[]
        {
            new RouteConfig
            {
                RouteId = "orders_route",
                ClusterId = "orders_cluster",
                Match = new RouteMatch { Path = "/orders/{**catchall}" }
            },
            new RouteConfig
            {
                RouteId = "payments_route",
                ClusterId = "payments_cluster",
                Match = new RouteMatch { Path = "/payments/{**catchall}" }
            }
        },
        new[]
        {
            new ClusterConfig
            {
                ClusterId = "orders_cluster",
                Destinations = new Dictionary<string, DestinationConfig>
                {
                    ["dest1"] = new DestinationConfig { Address = "http://orders-service:80/" }
                }
            },
            new ClusterConfig
            {
                ClusterId = "payments_cluster",
                Destinations = new Dictionary<string, DestinationConfig>
                {
                    ["dest1"] = new DestinationConfig { Address = "http://payments-service:80/" }
                }
            }
        }
    );

var app = builder.Build();

app.MapReverseProxy();

app.Run();