# mcp-server-win32-registry
Locally Hosted MCP Server For the Windows Registry

## Using 
https://github.com/modelcontextprotocol

## Notes
You do not need to have the server running manually. The command specified in your .mcp.json (in this case, the path to your mcp-server-win32-registry-server.exe) will
be used by Visual Studio to automatically start the server process when needed.  The MCP server will be running on your local machine,
accessing your local Windows Registry.

Visual Studio launchs the mcp-server-win32-registry-server using the command and manage its lifecycle as required.
