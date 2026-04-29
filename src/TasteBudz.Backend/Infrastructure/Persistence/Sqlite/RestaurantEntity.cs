namespace TasteBudz.Backend.Infrastructure.Persistence.Sqlite;


internal sealed class RestaurantEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? StreetAddress { get; set; }
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public Domain.PriceTier PriceTier { get; set; }
    public string? ExternalPlaceId { get; set; }
    public bool IsArchived { get; set; }
}
