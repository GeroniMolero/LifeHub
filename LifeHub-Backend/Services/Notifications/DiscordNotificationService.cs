namespace LifeHub.Services.Notifications
{
    public class DiscordNotificationService : INotificationService
    {
        private readonly HttpClient _httpClient;
        private readonly string? _webhookUrl;
        private readonly ILogger<DiscordNotificationService> _logger;

        public DiscordNotificationService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<DiscordNotificationService> logger)
        {
            _httpClient = httpClientFactory.CreateClient();
            _webhookUrl = configuration["Discord:WebhookUrl"];
            _logger = logger;
        }

        public async Task NotifyNewUserAsync()
        {
            if (string.IsNullOrEmpty(_webhookUrl)) return;
            try
            {
                await _httpClient.PostAsJsonAsync(_webhookUrl, new
                {
                    content = "Nuevo usuario registrado en LifeHub."
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo enviar la notificación a Discord.");
            }
        }
    }
}
