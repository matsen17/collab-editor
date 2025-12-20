namespace CollabEditor.Messaging.Configuration;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMQ";

    public string HostName { get; set; } = "localhost";
    
    public int Port { get; set; } = 5672;
    
    public string UserName { get; set; } = "guest";
    
    public string Password { get; set; } = "guest";
    
    public string VirtualHost { get; set; } = "/";
    
    public string ExchangeName { get; set; } = "collab-editor-exchange";
    
    public string ExchangeType { get; set; } = "topic";
}