using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace TERMS_MOBILE_WEB_API
{
    public class Program
    {
        public static void Main(string[] args)
        {

            //CreateHostBuilder(args).Build().Run();
            var host = CreateHostBuilder(args).Build();

            // Ensure wwwroot path is set if it's missing
            var env = (IWebHostEnvironment)host.Services.GetService(typeof(IWebHostEnvironment));
            if (string.IsNullOrEmpty(env.WebRootPath))
            {
                env.WebRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            }

            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.AddDebug();
                    logging.AddEventSourceLogger();
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    //webBuilder.UseUrls("http://localhost:44321");
                });
    }
}
