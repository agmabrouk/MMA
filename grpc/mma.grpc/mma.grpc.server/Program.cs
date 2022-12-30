using mma.grpc.server.helper;
using mma.grpc.server.Services;

var builder = WebApplication.CreateBuilder(args);

// Additional configuration is required to successfully run gRPC on macOS.
// For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682

// Add services to the container.
builder.Services.AddSingleton<IChatResources, ChatResources>();
builder.Services.AddGrpc(cfg => cfg.EnableDetailedErrors = true);

var app = builder.Build();

app.Urls.Add("http://localhost:6570");
app.Urls.Add("https://localhost:6571");
// Configure the HTTP request pipeline.
app.MapGrpcService<GreeterService>();
app.MapGrpcService<ChatService>();
app.MapGet("/", () => "Weclome To MMA (Minimal Messageing APP) Server!");
//app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
app.Run();


