using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using mma.rest.server2;

namespace ConsoleSignalRServer
{
    class Program
    {
        static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
         Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>().UseUrls("http://localhost:6868;https://localhost:6869");
                });
    }
}
