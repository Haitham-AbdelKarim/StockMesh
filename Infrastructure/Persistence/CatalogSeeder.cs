using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Persistence;

public static class CatalogSeeder
{
    public static async Task EnsureCatalogAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await SeedProductsAsync(dbContext);
        await SeedStoresAsync(dbContext);

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedProductsAsync(AppDbContext dbContext)
    {
        var products = new[]
        {
            new Product("PlayStation 5 Console", VerticalCategory.Gaming, "Sony"),
            new Product("Xbox Series X Console", VerticalCategory.Gaming, "Microsoft"),
            new Product("Nintendo Switch Console", VerticalCategory.Gaming, "Nintendo"),
            new Product("Wireless Controller", VerticalCategory.Gaming, "Sony"),
            new Product("Gaming Headset", VerticalCategory.Gaming, "Razer"),
            new Product("Mechanical Keyboard", VerticalCategory.Gaming, "SteelSeries"),
            new Product("Gaming Mouse", VerticalCategory.Gaming, "Logitech"),
            new Product("27-Inch Gaming Monitor", VerticalCategory.Gaming, "ASUS"),
            new Product("Game - FIFA 26", VerticalCategory.Gaming, "EA Sports"),
            new Product("Game - Call of Duty", VerticalCategory.Gaming, "Activision"),
            new Product("Game - Zelda: Tears of the Kingdom", VerticalCategory.Gaming, "Nintendo"),
            new Product("Gaming Chair", VerticalCategory.Gaming, "Secretlab"),
            new Product("PlayStation Plus Gift Card", VerticalCategory.Gaming, "Sony"),
            new Product("Xbox Game Pass Gift Card", VerticalCategory.Gaming, "Microsoft"),
            new Product("Steam Wallet Card", VerticalCategory.Gaming, "Valve"),
            new Product("VR Headset", VerticalCategory.Gaming, "Meta"),
            new Product("Capture Card", VerticalCategory.Gaming, "Elgato"),
            new Product("Cooling Fan Kit", VerticalCategory.Gaming, "Corsair"),

            new Product("Paracetamol 500mg - 20 Tabs", VerticalCategory.Pharmacy, "Panadol"),
            new Product("Ibuprofen 200mg - 24 Tabs", VerticalCategory.Pharmacy, "Brufen"),
            new Product("Amoxicillin 500mg - 20 Caps", VerticalCategory.Pharmacy, "Pharco"),
            new Product("Cough Syrup 150ml", VerticalCategory.Pharmacy, "Ricodex"),
            new Product("Antihistamine 10mg - 10 Tabs", VerticalCategory.Pharmacy, "Claritin"),
            new Product("Vitamin C 1000mg", VerticalCategory.Pharmacy, "Centrum"),
            new Product("Multivitamin - 60 Tabs", VerticalCategory.Pharmacy, "Supradyn"),
            new Product("Digital Thermometer", VerticalCategory.Pharmacy, "Omron"),
            new Product("Blood Pressure Monitor", VerticalCategory.Pharmacy, "Omron"),
            new Product("Insulin Syringes - 100 Pcs", VerticalCategory.Pharmacy, "BD"),
            new Product("Antiseptic Solution 250ml", VerticalCategory.Pharmacy, "Betadine"),
            new Product("Adhesive Bandages - 50 Pcs", VerticalCategory.Pharmacy, "Band-Aid"),
            new Product("Medical Gauze 10cm", VerticalCategory.Pharmacy, "Cura"),
            new Product("Antacid - 20 Tabs", VerticalCategory.Pharmacy, "Maalox"),
            new Product("Oral Rehydration Salts", VerticalCategory.Pharmacy, "Hydrite"),
            new Product("Vitamin D3 5000 IU", VerticalCategory.Pharmacy, "Nature's Bounty"),
            new Product("Eye Drops 10ml", VerticalCategory.Pharmacy, "Optrex"),
            new Product("Face Masks - 50 Pcs", VerticalCategory.Pharmacy, "Medi")
        };

        var existingNames = await dbContext.Products
            .Select(p => p.Name)
            .ToHashSetAsync();

        foreach (var product in products)
        {
            if (!existingNames.Contains(product.Name))
            {
                dbContext.Products.Add(product);
            }
        }
    }

    private static async Task SeedStoresAsync(AppDbContext dbContext)
    {
        var stores = new[]
        {
            new Store("Nile Gaming Hub", VerticalCategory.Gaming, 30.048, 31.235, 40, true),
            new Store("Game Zone Downtown", VerticalCategory.Gaming, 30.044, 31.235, 35, true),
            new Store("Cairo PlayHouse", VerticalCategory.Gaming, 30.051, 31.248, 30, true),
            new Store("Maadi Care Pharmacy", VerticalCategory.Pharmacy, 30.052, 31.224, 20, true),
            new Store("Heliopolis Pharmacy", VerticalCategory.Pharmacy, 30.096, 31.337, 25, true),
            new Store("Downtown Chemists", VerticalCategory.Pharmacy, 30.047, 31.236, 15, true)
        };

        var existingNames = await dbContext.Stores
            .Select(s => s.Name)
            .ToHashSetAsync();

        foreach (var store in stores)
        {
            if (!existingNames.Contains(store.Name))
            {
                dbContext.Stores.Add(store);
            }
        }
    }
}