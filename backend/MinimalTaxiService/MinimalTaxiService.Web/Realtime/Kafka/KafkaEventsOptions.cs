namespace MinimalTaxiService.Web.Realtime.Kafka;

public sealed class KafkaEventsOptions
{
    public const string SectionName = "Kafka";

    // Включает Kafka transport для trip events.
    public bool Enabled { get; set; }

    // Адрес(а) брокера Kafka, например: localhost:9092
    public string BootstrapServers { get; set; } = "localhost:9092";

    // Топик, в который пишем и из которого читаем события изменений поездок.
    public string Topic { get; set; } = "minimal-taxi.trip-events";

    // Consumer group для backend-инстансов сервиса.
    public string GroupId { get; set; } = "minimal-taxi-service";
}
