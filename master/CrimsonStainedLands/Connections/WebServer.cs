using CrimsonStainedLands.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using System.IO;
using System.Net.Http;
using System.Net.WebSockets;
using System.Security.Cryptography.X509Certificates;

namespace CrimsonStainedLands.Connections
{
    public class WebServer
    {
        private readonly WebApplication app;
        private readonly ConnectionManager manager;
        private readonly CancellationTokenSource cancellationTokenSource;
        public ConcurrentList<WebsocketConnection> connections = new ConcurrentList<WebsocketConnection>();
        private ConnectionManager.ConnectionConnected connectionConnectedCallback;

        public WebServer(ConnectionManager manager, string address, int port, X509Certificate2 certificate, CancellationTokenSource cancellationTokenSource)
        {
            this.manager = manager;
            this.cancellationTokenSource = cancellationTokenSource;

            var builder = WebApplication.CreateBuilder();
            builder.Services.AddCors();
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ListenAnyIP(port, listenOptions =>
                {
                    listenOptions.UseHttps(new HttpsConnectionAdapterOptions
                    {
                        ServerCertificate = certificate,
                        SslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13
                    });
                });
            });

            app = builder.Build();
            ConfigureApp();
        }

        private void ConfigureApp()
        {
            app.UseCors(builder =>
            {
                builder
                    .WithOrigins(
                        "https://crimsonstainedlands.net",
                        "https://www.crimsonstainedlands.net",
                        "https://kbs-cloud.com",
                        "https://games.mywire.org"
                    )
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });
            app.UseWebSockets();
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "web")),
                RequestPath = ""
            });

            app.MapGet("/who", async (HttpContext context) =>
            {
                try
                {
                    var result = new WhoListController().GetContent();
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(result);
                }
                catch
                {
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsync("An error occurred processing the request.");
                }
            });

            app.Map("/", async context =>
            {
                if (context.WebSockets.IsWebSocketRequest)
                {
                    using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                    var connection = new WebsocketConnection(manager, this, webSocket, cancellationTokenSource, context);
                    connections.Add(connection);
                    connectionConnectedCallback(connection, null, null);
                    await connection.HandleWebSocketConnection();
                }
                else
                {
                    string html = await File.ReadAllTextAsync(Path.Combine(Directory.GetCurrentDirectory(), "web", "client.html"));
                    html = html.Replace("new App(4003)", $"new App({Settings.Port + 3})");
                    await context.Response.WriteAsync(html);
                }
            });

        }

        public async Task Start(ConnectionManager.ConnectionConnected connectionConnected)
        {
            connectionConnectedCallback = connectionConnected;
            try
            {
                var host = app.Services.GetService<IHost>();
                await host.StartAsync(cancellationTokenSource.Token);

                // Wait for the token to be cancelled
                var tcs = new TaskCompletionSource<object>();
                cancellationTokenSource.Token.Register(() => tcs.TrySetResult(null));
                await tcs.Task;

                // Graceful shutdown
                await host.StopAsync(TimeSpan.FromSeconds(30));
            }
            catch
            {
                
            }
            
            //await app.RunAsync();
        }
    }
}