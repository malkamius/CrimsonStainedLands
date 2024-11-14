using System.Text;
using System.Text.Json;
using Discord;
using Discord.WebSocket;

namespace CrimsonStainedLands
{ 

    internal class Discord
    {
        private static Discord _instance;
        private static readonly object _lock = new();
        private readonly HttpClient _client;
        private readonly DiscordSocketClient _discordClient;

        public delegate void MessageReceivedHandler(string username, string channel, string content);
        public event MessageReceivedHandler OnMessageReceived;

        // Public accessor for the singleton instance
        public static Discord Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new Discord();
                    }
                }
                return _instance;
            }
        }

        // Private constructor to prevent direct instantiation
        private Discord()
        {
            _client = new HttpClient();

            var config = new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.MessageContent |
                         GatewayIntents.Guilds |
                         GatewayIntents.GuildMessages,
                LogLevel = LogSeverity.Info
            };

            _discordClient = new DiscordSocketClient(config);
            _discordClient.MessageReceived += HandleMessageAsync;
            _discordClient.Ready += ReadyAsync;
            _discordClient.Log += LogAsync;
        }

        public async Task StartAsync(string botToken)
        {
            if (!string.IsNullOrEmpty(botToken))
            {
                await _discordClient.LoginAsync(TokenType.Bot, botToken);
                await _discordClient.StartAsync();
            }
        }

        public async Task StopAsync()
        {
            if (_discordClient != null)
            {
                await _discordClient.StopAsync();
                await _discordClient.DisposeAsync();
            }
        }

        private Task ReadyAsync()
        {
            Game.log($"Bot connected as {_discordClient.CurrentUser}");
            return Task.CompletedTask;
        }

        private Task LogAsync(LogMessage log)
        {
            Game.log(log.ToString());
            return Task.CompletedTask;
        }

        private Task HandleMessageAsync(SocketMessage message)
        {
            // Ignore system messages
            if (message is not SocketUserMessage userMessage)
                return Task.CompletedTask;

            // Ignore bots and webhooks
            if (userMessage.Author.IsBot || userMessage.Author.IsWebhook)
                return Task.CompletedTask;

            // Trigger the event with message details
            OnMessageReceived?.Invoke(
                userMessage.Author.Username,
                userMessage.Channel.Name,
                userMessage.Content
            );

            return Task.CompletedTask;
        }

        public async Task SendMessage(string webhookUrl, string username, string content)
        {
            if (string.IsNullOrEmpty(webhookUrl))
            {
                return;
            }
            var payload = new
            {
                username = username,
                name = username,
                content = content
            };
            var jsonPayload = JsonSerializer.Serialize(payload);
            var httpContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            try
            {
                var response = await _client.PostAsync(webhookUrl, httpContent);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                Game.log($"Error sending Discord webhook: {ex.Message}");
            }
        }
    }
} // end namespace