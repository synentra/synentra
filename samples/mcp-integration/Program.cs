using ModelContextProtocol.Server;
using System.ComponentModel;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddMcpServer()
    .WithHttpTransport(options => options.Stateless = true)
    .WithToolsFromAssembly();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
app.MapMcp("/mcp");

app.Run("http://0.0.0.0:3001");

[McpServerToolType]
public static class DemoTools
{
    [McpServerTool, Description("Returns the supplied message unchanged.")]
    public static string Echo(
        [Description("Message to return")] string message)
        => message;
}