using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class Store : BaseEntity
{
    public string Name { get; private set; }

    public VerticalCategory VerticalCategory { get; private set; }

    public double Latitude { get; private set; }

    public double Longitude { get; private set; }

    public bool IsVerified { get; private set; }

    public double MaxSearchRadiusKm { get; private set; }

    private Store()
    {
        Name = null!;
    }

    public Store(
        string name,
        VerticalCategory verticalCategory,
        double latitude,
        double longitude,
        double maxSearchRadiusKm = 30,
        bool isVerified = false)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Store name is required.", nameof(name));
        }

        if (latitude is < -90 or > 90)
        {
            throw new ArgumentOutOfRangeException(nameof(latitude), "Latitude must be between -90 and 90.");
        }

        if (longitude is < -180 or > 180)
        {
            throw new ArgumentOutOfRangeException(nameof(longitude), "Longitude must be between -180 and 180.");
        }

        if (maxSearchRadiusKm <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxSearchRadiusKm), "Search radius must be positive.");
        }

        Name = name;
        VerticalCategory = verticalCategory;
        Latitude = latitude;
        Longitude = longitude;
        IsVerified = isVerified;
        MaxSearchRadiusKm = maxSearchRadiusKm;
    }

    public void Verify()
    {
        IsVerified = true;
    }
}