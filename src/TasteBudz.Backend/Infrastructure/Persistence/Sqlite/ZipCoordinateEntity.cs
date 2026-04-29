namespace TasteBudz.Backend.Infrastructure.Persistence.Sqlite;


internal sealed class ZipCoordinateEntity
{
    public string ZipCode { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}
