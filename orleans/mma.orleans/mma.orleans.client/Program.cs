using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using mma.orleans.client;
using Orleans;
using Orleans.Hosting;
using Orleans.Runtime;
using System.Reflection;
using mma.orleans;

public class Program
{
    public async static Task Main(string[] args)
    {

        Console.WriteLine("\r\n _______ _______ _______      ______ __           __        _______              \r\n|   |   |   |   |   _   |    |      |  |--.---.-.|  |_     |   _   |.-----.-----.\r\n|       |       |       |    |   ---|     |  _  ||   _|    |       ||  _  |  _  |\r\n|__|_|__|__|_|__|___|___|    |______|__|__|___._||____|    |___|___||   __|   __|\r\n                                                                    |__|  |__|   \r\n");


        Console.WriteLine("----------------------------------");
        Console.WriteLine("Welcome to the MAA Orleans Client!");
        Console.WriteLine("----------------------------------");



        Console.WriteLine("Please Enter you Username:");
        using var host = new HostBuilder()
        .UseOrleansClient(clientBuilder =>
        {
            clientBuilder.UseLocalhostClustering().AddMemoryStreams("MMAChat");
        })
        .Build();

        var client = host.Services.GetRequiredService<IClusterClient>();
        //var manager = host.Services.GetRequiredService<IManagementGrain>();
        ClientContext context = new(client);
        await host.StartAsync();
        context = context with
        {
            UserName = Console.ReadLine()
        };

        await ChatClient.ProcessLoopAsync(context);
        await StopAsync(host);
    }
    static Task StopAsync(IHost host) => host.StopAsync();

}