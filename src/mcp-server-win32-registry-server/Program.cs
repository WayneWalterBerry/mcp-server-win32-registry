// <copyright file="RegistryTool.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace mcp_server_win32_registry_server
{
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Serilog;
    using System;
    using System.IO;

    internal class Program
    {
        static void Main(string[] args)
        {
            // Debugging aid: Uncomment to launch debugger when this method is called
            System.Diagnostics.Debugger.Launch();

            Log.Logger = new LoggerConfiguration()
                       .MinimumLevel.Verbose() // Capture all log levels
                       .WriteTo.File(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "TestServer_.log"),
                           rollingInterval: RollingInterval.Day,
                           outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                       .WriteTo.Debug()
                       .WriteTo.Console(standardErrorFromLevel: Serilog.Events.LogEventLevel.Verbose)
                       .CreateLogger();

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
