// <copyright file="RegistryTool.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace mcp_server_win32_registry_server
{
    using Microsoft.Win32;
    using ModelContextProtocol.Server;
    using System.ComponentModel;

    [McpServerToolType]
    public sealed class RegistryTool
    {
        [McpServerTool, Description("Read a registry value")]
        public string ReadRegistry(string keyPath, string valueName)
        {
            return Registry.GetValue(keyPath, valueName, "Not found")?.ToString() ?? "Not found";
        }
    }
}