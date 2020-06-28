﻿using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace app
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args)
                .UseUrls("http://0.0.0.0:8080/")
                .Build()
                .Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();
    }
}
