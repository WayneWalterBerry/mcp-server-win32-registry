## 🎯 Overview

### ✨ What is the mcp-server-win32-registry server?

The mcp-server-win32-registry servier is a local running service that enables MCP hosts like GitHub Copilot and Cursor to search and retrieve registry information from the local machine. By implementing the standardized Model Context Protocol (MCP), this service allows any compatible AI system to read the Window registry

**Example JSON configuration:**
```json
{
    "mcp_server_win32_registry_server": {
      "type": "stdio",
      "command": "D:\\mcp-server-win32-registry\\artifacts\\Debug\\mcp-server-win32-registry-server.exe",
      "args": []
    }
}
```

### ▶️ Getting Started
1. Open GitHub Copilot in VS Code and [switch to Agent mode](https://code.visualstudio.com/docs/copilot/chat/chat-agent-mode)
2. You should see the mcp-server-win32-registry server in the list of tools
3. Try a prompt that tells the agent to use the cp-server-win32-registry server, such as "What is the shell namespace extension installed to handle the .zip file extesnion?"
4. The agent will query the registry via  mcp-server-win32-registry server to answer your question

### Example Prompts
Prompt: What is the shell namespace extension installed to handle the .zip file extesnion?
![Answer To Prompt](docs/images/prompt-zip-shell-extension.png)

## Using 
https://github.com/modelcontextprotocol

## Notes
You do not need to have the server running manually. The command specified in your .mcp.json (in this case, the path to your mcp-server-win32-registry-server.exe) will
be used by Visual Studio to automatically start the server process when needed.  The MCP server will be running on your local machine,
accessing your local Windows Registry.

Visual Studio launchs the mcp-server-win32-registry-server using the command and manage its lifecycle as required.
