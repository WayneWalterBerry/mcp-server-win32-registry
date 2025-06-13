// <copyright file="RegistryTool.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace mcp_server_win32_registry_server
{
    using Microsoft.Win32;
    using ModelContextProtocol.Server;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Text.Json;

    /// <summary>
    /// Provides registry access operations for the MCP server.
    /// </summary>
    [McpServerToolType]
    public sealed class RegistryTool
    {
        /// <summary>
        /// Gets a registry value and returns it as serialized JSON.
        /// </summary>
        /// <param name="keyPath">The full path to the registry key (e.g. "HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows").</param>
        /// <param name="valueName">The name of the value to read.</param>
        /// <returns>A JSON serialized string representing the registry value, or an error message.</returns>
        [McpServerTool, Description("Get a registry value")]
        public static string GetValue(string keyPath, string valueName)
        {
            Log.Information($"Getting registry value: KeyPath='{keyPath}', ValueName='{valueName}'");

            try
            {
                var value = Registry.GetValue(keyPath, valueName, null);

                if (value == null)
                {
                    return JsonSerializer.Serialize(new { error = "Value not found" });
                }

                // Handle different data types appropriately
                if (value is byte[] byteArray)
                {
                    return JsonSerializer.Serialize(new
                    {
                        type = "binary",
                        value = Convert.ToBase64String(byteArray),
                        displayValue = BitConverter.ToString(byteArray).Replace("-", " ")
                    });
                }
                else if (value is string[] stringArray)
                {
                    return JsonSerializer.Serialize(new { type = "multi-string", value = stringArray });
                }
                else
                {
                    return JsonSerializer.Serialize(new
                    {
                        type = value.GetType().Name,
                        value = value.ToString()
                    });
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting registry value");
                return JsonSerializer.Serialize(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Gets all subkeys under the specified registry key path.
        /// </summary>
        /// <param name="keyPath">The full path to the registry key (e.g. "HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft").</param>
        /// <returns>A JSON string containing the list of subkeys, or an error message.</returns>
        [McpServerTool, Description("Get a list of all subkeys under a registry key")]
        public static string GetSubKeys(string keyPath)
        {
            Log.Information($"Getting subkeys for registry path: '{keyPath}'");

            try
            {
                var rootKey = GetRootKeyFromPath(keyPath, out string subKeyPath);
                if (rootKey == null)
                {
                    return $"Error: Invalid registry key path '{keyPath}'";
                }

                using (var key = rootKey.OpenSubKey(subKeyPath))
                {
                    if (key == null)
                    {
                        return $"Error: Registry key '{keyPath}' not found";
                    }

                    var subKeys = key.GetSubKeyNames();
                    return JsonSerializer.Serialize(subKeys);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting registry subkeys");
                return $"Error: {ex.Message}";
            }
        }

        /// <summary>
        /// Gets all value names and their data under the specified registry key path.
        /// </summary>
        /// <param name="keyPath">The full path to the registry key (e.g. "HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows").</param>
        /// <returns>A JSON string containing the key-value pairs, or an error message.</returns>
        [McpServerTool, Description("Get all values under a registry key")]
        public static string GetValues(string keyPath)
        {
            Log.Information($"Getting values for registry path: '{keyPath}'");

            try
            {
                var rootKey = GetRootKeyFromPath(keyPath, out string subKeyPath);
                if (rootKey == null)
                {
                    return $"Error: Invalid registry key path '{keyPath}'";
                }

                using (var key = rootKey.OpenSubKey(subKeyPath))
                {
                    if (key == null)
                    {
                        return $"Error: Registry key '{keyPath}' not found";
                    }

                    var valueNames = key.GetValueNames();
                    var result = new Dictionary<string, string>();

                    foreach (var valueName in valueNames)
                    {
                        var value = key.GetValue(valueName);
                        result[valueName] = value?.ToString() ?? "(null)";
                    }

                    return JsonSerializer.Serialize(result);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting registry values");
                return $"Error: {ex.Message}";
            }
        }

        /// <summary>
        /// Searches for registry keys that match the given search pattern.
        /// </summary>
        /// <param name="rootKeyPath">The root path to start searching from (e.g. "HKEY_LOCAL_MACHINE\SOFTWARE").</param>
        /// <param name="searchPattern">The pattern to search for in key names.</param>
        /// <param name="maxDepth">Maximum depth to search (1-5), defaults to 2.</param>
        /// <returns>A JSON string containing the matching key paths, or an error message.</returns>
        [McpServerTool, Description("Search for registry keys matching a pattern")]
        public static string FindKeys(string rootKeyPath, string searchPattern, int maxDepth = 2)
        {
            Log.Information($"Searching for registry keys: Root='{rootKeyPath}', Pattern='{searchPattern}', MaxDepth={maxDepth}");

            try
            {
                // Limit search depth for safety
                maxDepth = Math.Min(Math.Max(maxDepth, 1), 5);

                var rootKey = GetRootKeyFromPath(rootKeyPath, out string subKeyPath);
                if (rootKey == null)
                {
                    return $"Error: Invalid registry key path '{rootKeyPath}'";
                }

                var results = new List<string>();
                SearchKeys(rootKey, subKeyPath, searchPattern, results, maxDepth, 0);

                return JsonSerializer.Serialize(results);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error searching registry keys");
                return $"Error: {ex.Message}";
            }
        }

        /// <summary>
        /// Searches for registry values that match the given search pattern.
        /// </summary>
        /// <param name="rootKeyPath">The root path to start searching from (e.g. "HKEY_LOCAL_MACHINE\SOFTWARE").</param>
        /// <param name="searchPattern">The pattern to search for in value names or data.</param>
        /// <param name="maxDepth">Maximum depth to search (1-3), defaults to 2.</param>
        /// <returns>A JSON string containing the matching values with their paths, or an error message.</returns>
        [McpServerTool, Description("Search for registry values matching a pattern")]
        public static string FindValues(string rootKeyPath, string searchPattern, int maxDepth = 2)
        {
            Log.Information($"Searching for registry values: Root='{rootKeyPath}', Pattern='{searchPattern}', MaxDepth={maxDepth}");

            try
            {
                // Limit search depth for safety
                maxDepth = Math.Min(Math.Max(maxDepth, 1), 3);

                var rootKey = GetRootKeyFromPath(rootKeyPath, out string subKeyPath);
                if (rootKey == null)
                {
                    return $"Error: Invalid registry key path '{rootKeyPath}'";
                }

                var results = new Dictionary<string, string>();
                SearchValues(rootKey, subKeyPath, searchPattern, results, maxDepth, 0);

                return JsonSerializer.Serialize(results);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error searching registry values");
                return $"Error: {ex.Message}";
            }
        }

        /// <summary>
        /// Gets the key type for a registry value.
        /// </summary>
        /// <param name="keyPath">The full path to the registry key.</param>
        /// <param name="valueName">The name of the value.</param>
        /// <returns>A string representing the registry value type, or an error message.</returns>
        [McpServerTool, Description("Get the type of a registry value")]
        public static string GetValueType(string keyPath, string valueName)
        {
            Log.Information($"Getting value type: KeyPath='{keyPath}', ValueName='{valueName}'");

            try
            {
                var rootKey = GetRootKeyFromPath(keyPath, out string subKeyPath);
                if (rootKey == null)
                {
                    return $"Error: Invalid registry key path '{keyPath}'";
                }

                using (var key = rootKey.OpenSubKey(subKeyPath))
                {
                    if (key == null)
                    {
                        return $"Error: Registry key '{keyPath}' not found";
                    }

                    var valueKind = key.GetValueKind(valueName);
                    return valueKind.ToString();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting registry value type");
                return $"Error: {ex.Message}";
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// Extracts the root key and subkey path from a full registry path.
        /// </summary>
        /// <param name="fullKeyPath">The full registry key path.</param>
        /// <param name="subKeyPath">Output parameter for the subkey portion of the path.</param>
        /// <returns>The root RegistryKey object, or null if invalid.</returns>
        private static RegistryKey GetRootKeyFromPath(string fullKeyPath, out string subKeyPath)
        {
            subKeyPath = string.Empty;

            if (string.IsNullOrWhiteSpace(fullKeyPath))
            {
                return null;
            }

            string[] parts = fullKeyPath.Split('\\');
            if (parts.Length < 1)
            {
                return null;
            }

            RegistryKey rootKey = null;
            string rootName = parts[0].ToUpperInvariant();

            switch (rootName)
            {
                case "HKEY_CLASSES_ROOT":
                case "HKCR":
                    rootKey = Registry.ClassesRoot;
                    break;
                case "HKEY_CURRENT_USER":
                case "HKCU":
                    rootKey = Registry.CurrentUser;
                    break;
                case "HKEY_LOCAL_MACHINE":
                case "HKLM":
                    rootKey = Registry.LocalMachine;
                    break;
                case "HKEY_USERS":
                case "HKU":
                    rootKey = Registry.Users;
                    break;
                case "HKEY_CURRENT_CONFIG":
                case "HKCC":
                    rootKey = Registry.CurrentConfig;
                    break;
            }

            if (rootKey != null && parts.Length > 1)
            {
                subKeyPath = string.Join("\\", parts.Skip(1));
            }

            return rootKey;
        }

        /// <summary>
        /// Recursively searches registry keys matching the search pattern.
        /// </summary>
        private static void SearchKeys(RegistryKey rootKey, string currentPath, string searchPattern, List<string> results, int maxDepth, int currentDepth)
        {
            if (currentDepth > maxDepth)
            {
                return;
            }

            try
            {
                using (var key = rootKey.OpenSubKey(currentPath))
                {
                    if (key == null)
                    {
                        return;
                    }

                    // Check if current key matches
                    string fullPath = $"{GetRootKeyName(rootKey)}\\{currentPath}";
                    if (fullPath.IndexOf(searchPattern, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        results.Add(fullPath);
                    }

                    // Search subkeys
                    foreach (var subKeyName in key.GetSubKeyNames())
                    {
                        string newPath = string.IsNullOrEmpty(currentPath) ? subKeyName : $"{currentPath}\\{subKeyName}";
                        SearchKeys(rootKey, newPath, searchPattern, results, maxDepth, currentDepth + 1);

                        // Safety limit - don't return too many results
                        if (results.Count > 100)
                        {
                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, $"Error accessing registry path: {currentPath}");
            }
        }

        /// <summary>
        /// Recursively searches registry values matching the search pattern.
        /// </summary>
        private static void SearchValues(RegistryKey rootKey, string currentPath, string searchPattern, Dictionary<string, string> results, int maxDepth, int currentDepth)
        {
            if (currentDepth > maxDepth)
            {
                return;
            }

            try
            {
                using (var key = rootKey.OpenSubKey(currentPath))
                {
                    if (key == null)
                    {
                        return;
                    }

                    string fullPath = $"{GetRootKeyName(rootKey)}\\{currentPath}";

                    // Check if any value matches
                    foreach (var valueName in key.GetValueNames())
                    {
                        if (string.IsNullOrEmpty(valueName) && valueName.IndexOf(searchPattern, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            continue;
                        }

                        var value = key.GetValue(valueName)?.ToString() ?? string.Empty;

                        if (valueName.IndexOf(searchPattern, StringComparison.OrdinalIgnoreCase) >= 0 ||
                            value.IndexOf(searchPattern, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            string fullValuePath = $"{fullPath}\\{valueName}";
                            results[fullValuePath] = value;

                            // Safety limit - don't return too many results
                            if (results.Count > 100)
                            {
                                return;
                            }
                        }
                    }

                    // Search subkeys
                    foreach (var subKeyName in key.GetSubKeyNames())
                    {
                        string newPath = string.IsNullOrEmpty(currentPath) ? subKeyName : $"{currentPath}\\{subKeyName}";
                        SearchValues(rootKey, newPath, searchPattern, results, maxDepth, currentDepth + 1);

                        // Safety limit - don't return too many results
                        if (results.Count > 100)
                        {
                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, $"Error accessing registry path: {currentPath}");
            }
        }

        /// <summary>
        /// Gets the string name of the root registry key.
        /// </summary>
        /// <param name="key">The registry key to get the name for.</param>
        /// <returns>The string name of the root key.</returns>
        private static string GetRootKeyName(RegistryKey key)
        {
            if (key == Registry.ClassesRoot) return "HKEY_CLASSES_ROOT";
            if (key == Registry.CurrentUser) return "HKEY_CURRENT_USER";
            if (key == Registry.LocalMachine) return "HKEY_LOCAL_MACHINE";
            if (key == Registry.Users) return "HKEY_USERS";
            if (key == Registry.CurrentConfig) return "HKEY_CURRENT_CONFIG";
            return "UNKNOWN";
        }

        #endregion
    }
}