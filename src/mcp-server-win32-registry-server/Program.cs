// <copyright file="RegistryTool.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace mcp_server_win32_registry_server
{
    using System;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Serilog;

    internal class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Log.Information("Starting server...");

                var builder = Host.CreateApplicationBuilder(args);
                builder.Services.AddSerilog();
                builder.Services.AddMcpServer()
                    .WithStdioServerTransport()
                    .WithTools<RegistryTool>();

                var app = builder.Build();

                app.RunAsync().Wait();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
