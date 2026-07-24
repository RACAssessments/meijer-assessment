namespace MeijerProducts.Api.Data;

public static class DbInitializer
{
    // All 30 sample products share this image in docs/product-details.json — used here as
    // the single source of truth for seeding, since that file is the more complete sample
    // (it's the only one with description+price) and its imageUrl is internally consistent,
    // unlike docs/products (1).json which alternates between two placeholder URLs by id parity.
    private const string ImageUrl =
        "https://www.meijer.com/content/dam/meijer/product/0736/54/7567/20/0736547567200_0_A1C1_0600.png";

    private static readonly Product[] SeedProducts =
    [
        new() { Id = 0, Title = "Bananas", Summary = "Fresh bananas, perfect for a healthy snack.", Description = "Fresh bananas, perfect for a healthy snack. Rich in potassium and vitamins.", Price = "$0.59/lb", ImageUrl = ImageUrl },
        new() { Id = 1, Title = "Gala Apples", Summary = "Crisp and sweet Gala apples.", Description = "Crisp and sweet Gala apples, great for snacking and baking.", Price = "$1.29/lb", ImageUrl = ImageUrl },
        new() { Id = 2, Title = "Broccoli Crowns", Summary = "Fresh broccoli crowns, perfect for steaming or roasting.", Description = "Fresh broccoli crowns, perfect for steaming or roasting. Rich in vitamins and minerals.", Price = "$1.99/lb", ImageUrl = ImageUrl },
        new() { Id = 3, Title = "Strawberries", Summary = "Juicy and sweet strawberries.", Description = "Juicy and sweet strawberries, perfect for desserts and snacks.", Price = "$2.99/lb", ImageUrl = ImageUrl },
        new() { Id = 4, Title = "Blueberries", Summary = "Fresh and delicious blueberries.", Description = "Fresh and delicious blueberries, great for snacking and baking.", Price = "$3.49/lb", ImageUrl = ImageUrl },
        new() { Id = 5, Title = "Carrots", Summary = "Crunchy and sweet carrots.", Description = "Crunchy and sweet carrots, perfect for salads and snacks.", Price = "$0.99/lb", ImageUrl = ImageUrl },
        new() { Id = 6, Title = "Spinach", Summary = "Fresh and nutritious spinach.", Description = "Fresh and nutritious spinach, great for salads and cooking.", Price = "$2.49/bag", ImageUrl = ImageUrl },
        new() { Id = 7, Title = "Tomatoes", Summary = "Juicy and ripe tomatoes.", Description = "Juicy and ripe tomatoes, perfect for salads and cooking.", Price = "$1.49/lb", ImageUrl = ImageUrl },
        new() { Id = 8, Title = "Cucumbers", Summary = "Fresh and crisp cucumbers.", Description = "Fresh and crisp cucumbers, great for salads and snacks.", Price = "$0.79/each", ImageUrl = ImageUrl },
        new() { Id = 9, Title = "Bell Peppers", Summary = "Colorful and crunchy bell peppers.", Description = "Colorful and crunchy bell peppers, perfect for salads and cooking.", Price = "$1.29/each", ImageUrl = ImageUrl },
        new() { Id = 10, Title = "Oranges", Summary = "Sweet and juicy oranges.", Description = "Sweet and juicy oranges, perfect for a refreshing snack.", Price = "$1.49/lb", ImageUrl = ImageUrl },
        new() { Id = 11, Title = "Pineapples", Summary = "Fresh and sweet pineapples.", Description = "Fresh and sweet pineapples, perfect for a tropical treat.", Price = "$3.99/each", ImageUrl = ImageUrl },
        new() { Id = 12, Title = "Mangoes", Summary = "Juicy and sweet mangoes.", Description = "Juicy and sweet mangoes, perfect for a tropical snack.", Price = "$1.49/each", ImageUrl = ImageUrl },
        new() { Id = 13, Title = "Peaches", Summary = "Sweet and juicy peaches.", Description = "Sweet and juicy peaches, perfect for a summer treat.", Price = "$2.49/lb", ImageUrl = ImageUrl },
        new() { Id = 14, Title = "Plums", Summary = "Sweet and juicy plums.", Description = "Sweet and juicy plums, perfect for a healthy snack.", Price = "$2.99/lb", ImageUrl = ImageUrl },
        new() { Id = 15, Title = "Grapes", Summary = "Fresh and sweet grapes.", Description = "Fresh and sweet grapes, perfect for snacking.", Price = "$2.99/lb", ImageUrl = ImageUrl },
        new() { Id = 16, Title = "Lettuce", Summary = "Fresh and crisp lettuce.", Description = "Fresh and crisp lettuce, perfect for salads.", Price = "$1.99/head", ImageUrl = ImageUrl },
        new() { Id = 17, Title = "Kale", Summary = "Fresh and nutritious kale.", Description = "Fresh and nutritious kale, great for salads and cooking.", Price = "$2.49/bunch", ImageUrl = ImageUrl },
        new() { Id = 18, Title = "Avocados", Summary = "Creamy and delicious avocados.", Description = "Creamy and delicious avocados, perfect for salads and guacamole.", Price = "$1.49/each", ImageUrl = ImageUrl },
        new() { Id = 19, Title = "Zucchini", Summary = "Fresh and versatile zucchini.", Description = "Fresh and versatile zucchini, great for grilling and cooking.", Price = "$1.29/lb", ImageUrl = ImageUrl },
        new() { Id = 20, Title = "Eggplant", Summary = "Fresh and delicious eggplant.", Description = "Fresh and delicious eggplant, perfect for grilling and cooking.", Price = "$1.99/each", ImageUrl = ImageUrl },
        new() { Id = 21, Title = "Cauliflower", Summary = "Fresh and nutritious cauliflower.", Description = "Fresh and nutritious cauliflower, great for roasting and cooking.", Price = "$2.49/head", ImageUrl = ImageUrl },
        new() { Id = 22, Title = "Green Beans", Summary = "Fresh and crisp green beans.", Description = "Fresh and crisp green beans, perfect for steaming and cooking.", Price = "$1.99/lb", ImageUrl = ImageUrl },
        new() { Id = 23, Title = "Asparagus", Summary = "Fresh and nutritious asparagus.", Description = "Fresh and nutritious asparagus, great for grilling and roasting.", Price = "$2.99/lb", ImageUrl = ImageUrl },
        new() { Id = 24, Title = "Celery", Summary = "Fresh and crisp celery.", Description = "Fresh and crisp celery, perfect for salads and snacking.", Price = "$1.49/bunch", ImageUrl = ImageUrl },
        new() { Id = 25, Title = "Radishes", Summary = "Fresh and spicy radishes.", Description = "Fresh and spicy radishes, perfect for salads and snacking.", Price = "$1.29/bunch", ImageUrl = ImageUrl },
        new() { Id = 26, Title = "Beets", Summary = "Fresh and nutritious beets.", Description = "Fresh and nutritious beets, great for roasting and salads.", Price = "$1.99/bunch", ImageUrl = ImageUrl },
        new() { Id = 27, Title = "Sweet Potatoes", Summary = "Fresh and sweet potatoes.", Description = "Fresh and sweet potatoes, perfect for baking and roasting.", Price = "$1.49/lb", ImageUrl = ImageUrl },
        new() { Id = 28, Title = "Onions", Summary = "Fresh and flavorful onions.", Description = "Fresh and flavorful onions, perfect for cooking and salads.", Price = "$0.99/lb", ImageUrl = ImageUrl },
        new() { Id = 29, Title = "Garlic", Summary = "Fresh and aromatic garlic.", Description = "Fresh and aromatic garlic, perfect for cooking and seasoning.", Price = "$0.49/each", ImageUrl = ImageUrl },
    ];

    public static void Seed(ProductDbContext db)
    {
        if (db.Products.Any())
        {
            return;
        }

        db.Products.AddRange(SeedProducts);
        db.SaveChanges();
    }
}
