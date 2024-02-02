using Chip8EmuServer;
using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

builder.WebHost.UseUrls("http://localhost:8080");
app.UseWebSockets();
app.Map("/ws", async context => 
{
    if(context.WebSockets.IsWebSocketRequest)
    {
        ConnHandler connectionHandler = new ConnHandler(await context.WebSockets.AcceptWebSocketAsync());
        connectionHandler.Listen();
        connectionHandler.Send();
    }
    else
    {
        Console.WriteLine("bad request");
        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
    }
});

await app.RunAsync();
