namespace MinimalTaxiService.Domain.Enums;

public enum TripStatus
{
    Created,            // Только создан
    DriverAccepted,     // Водитель принял
    Completed,          // Поездка завершена
    CancelledByPassenger,
    CancelledByDriver,
    Expired               // Не принял никто за N минут
}