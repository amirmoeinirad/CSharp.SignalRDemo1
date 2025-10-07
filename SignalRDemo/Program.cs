
using System.Threading.RateLimiting;

namespace SignalRDemo
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // The WebApplication class used to configure the HTTP pipelines and routes.            
            var builder = WebApplication.CreateBuilder(args);


            // ----------------------------
            // SERVICES
            // ----------------------------


            // (1)
            // Add/Register SignalR service to the ASP.NET Dependency Injection (DI) container.
            // This makes SignalR services available for use in the application.
            builder.Services.AddSignalR();

            // (2)
            // Enable Cross-Origin Resource Sharing (CORS) with a specific allowed origin.
            // This is because in the dev environment, front and back use different addresses and ports.           
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowSpecificOrigin", builder =>
                {
                    builder.WithOrigins("https://localhost:7000") // The frontend address (Backend accepts requests only from this address!)
                           .AllowAnyHeader()
                           .AllowAnyMethod()
                           .AllowCredentials(); // Required for authentication
                });
            });

            // (3)
            // Adding/Registering rate limiting service in the application's dependency injection container.
            // This prevents (D)DoS attacks.
            // In general, rate limiting is a technique used to control the rate of incoming requests to a server or API.
            builder.Services.AddRateLimiter(options =>
            {
                // Defines a global rate limiter that applies to all requests.
                // Uses Partitioned Rate Limiting, meaning that requests are grouped based on a partition key or
                // an identifier (client's IP address),                
                // The first generic argument <HttpContext> means rate limiting is applied at the HTTP request level.
                // The second generic argument <string> means the partition key (IP address) is a string.
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        // Each unique IP address is treated as a separate "bucket" (partition).
                        partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        // factory function
                        // Defines the rate-limiting behavior.
                        // The underscore (_) means we don’t need the input parameter.
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            // Max 10 connections per minute
                            PermitLimit = 10,
                            // Fixed window size of 1 minute. After 1 minute, the counter resets.
                            Window = TimeSpan.FromMinutes(1) 
                        }));
            });


            // Build the WebApplication instance.
            var app = builder.Build();


            // ----------------------------
            // MIDDLEWARES
            // ----------------------------


            // (1)
            // Allows serving index.html in the 'wwwroot' folder of the project.            
            app.UseDefaultFiles();

            // (2)
            // Enables serving static files (like JS, HTML, CSS) on the current path which defaults to the 'wwwroot' subfolder.
            app.UseStaticFiles();

            // (3)
            // Enable CORS before mapping hubs
            app.UseCors("AllowSpecificOrigin");

            // (4)
            // Enable Rate Limiter
            app.UseRateLimiter();

            // (5)
            // Enable routing
            app.UseRouting();

            // (6)
            // Enable SignalR
            // This maps /ChatHub (ChatHub class) as the WebSocket endpoint.
            // This maps incoming requests with the specified path/URL pattern to the ChatHub class.
            // "/ChatHub" means the clients will connect to the SignalR hub using this URL, e.g. ws://localhost:portNumber/ChatHub
            app.MapHub<ChatHub>("/ChatHub");

            // (7)
            // Default Route
            // This is a simple route that responds with "Hello World from the backend!" when accessing the root URL ("/").
            // Since index.html is located in the wwwroot folder, it is automatically loaded in the browser by ASP.NET 
            // when the application is started.
            // This way, the frontend and backend are loaded simultaneously.
            app.MapGet("/", () => "Hello World from the backend!");

            // Run the application and start listening for incoming HTTP requests.
            app.Run();
        }
    }
}

/*

How It All Works Together:

1) User opens index.html → JavaScript starts a connection to SignalR Hub on the server.
2) User clicks the button → sendMessage() is called on the client.
3) JavaScript sends a message to C# (SendMessage function on the server).
4) C# receives it and broadcasts it to all clients.
5) JavaScript listens (connection.on("ReceiveMessage")) and displays the message on the client.

----------------------------------------------------------------------

1) The index.html file must be located on the server (such as localhost at the wwwroot folder) 
in order to use the WebSocket.
In this case, the frontend is served by IIS server and backend by Kestrel server (ASP.NET Core).
In such a scenario, CORS policy must be set in order for the web application to work (front & back use different port numbers).

2) Another way (and a simpler one) is to serve the index.html file in the wwwroot folder of the ASP.NET Core project.
In this case, both frontend and backend will be served by ASP.NET Core and therefore, no need for CORS policy to be enabled.
However, the app.UseDefaultFiles() middleware line must be added to Program.cs to enable index.html serving. 
 
*/
