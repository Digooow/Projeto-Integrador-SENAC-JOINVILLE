using Cantina.Api;

var app = CantinaApiHost.CreateWebApplication(args);
await CantinaApiHost.InitializeDatabaseAsync(app);
await app.RunAsync();

public partial class Program
{
}
