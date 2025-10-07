
using Microsoft.AspNetCore.SignalR;
using System.Net;

namespace SignalRDemo
{
    // 'Microsoft.AspNetCore.SignalR.Hub' is a base class for a SignalR Hub.
    // This class handles client-server Websocket communication.
    public class ChatHub : Hub
    {
        // Send/Broadcast a message to all clients                
        // This method is called by the client with a message
        // Then, the message is sent from the server (SignalR) to all the connected clients.
        public async Task SendMessage(string text, string user, string message, string dateTime)
        {
            text = "Message from the .NET SignalR backend server: ";
            
            // The following line converts a string to an HTML-encoded string.
            // It encodes special HTML characters into their corresponding HTML entities to prevent issues like HTML injection and
            // Cross-Site Scripting (XSS) attacks.
            string safeMessage = WebUtility.HtmlEncode(message); // Prevents XSS (Injection) Attacks

            // 'IClientProxy.SendAsync()' invokes a method on all the connected clients.
            // In other words, it broadcasts messages to all connected clients.                        
            // "ReceiveMessage" is the name of the event on the client side to be invoked and to receive the server's message.
            await Clients.All.SendAsync("ReceiveMessage", text, user, safeMessage, dateTime);
        }
    }
}
