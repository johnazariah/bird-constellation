var builder = DistributedApplication.CreateBuilder(args);

// Add Owlet Service with health checks
var owletService = builder.AddProject("owlet-service", @"..\Owlet.Service\Owlet.Service.csproj");

builder.Build().Run();
