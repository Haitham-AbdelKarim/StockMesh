using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class Product : BaseEntity
{
    public string Name { get; private set; }

    public VerticalCategory VerticalCategory { get; private set; }

    public string? Brand { get; private set; }

    public string? Barcode { get; private set; }

    private Product()
    {
        Name = null!;
    }

    public Product(
        string name,
        VerticalCategory verticalCategory,
        string? brand = null,
        string? barcode = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Product name is required.", nameof(name));
        }

        Name = name;
        VerticalCategory = verticalCategory;
        Brand = brand;
        Barcode = barcode;
    }
}