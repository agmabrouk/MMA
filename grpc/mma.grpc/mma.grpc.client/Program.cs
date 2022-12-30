// See https://aka.ms/new-console-template for more information
using mma.grpc.client;


//// The port number must match the port of the gRPC server.
//using var channel = GrpcChannel.ForAddress("https://localhost:49153");
//var client = new Greeter.GreeterClient(channel);
//var reply = await client.SayHelloAsync(new HelloRequest { Name = "GreeterClient" });
//Console.WriteLine("Greeting: " + reply.Message);
//Console.WriteLine("Press any key to exit...");
//Console.ReadKey();


//using var channel = GrpcChannel.ForAddress("https://localhost:49153");
//var chatClient = new mmaChatService.mmaChatServiceClient(channel);


Console.WriteLine("-------------------------------");
Console.WriteLine("Welcome to the MAA GRPC Client!");
Console.WriteLine("-------------------------------");
new mmaGrpcClient();
Console.ReadLine();
