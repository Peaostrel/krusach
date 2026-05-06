using System;
using System.Collections.Generic;
using System.Linq;

namespace krusach.Models
{
    public static class DatabaseInitializer
    {
        public static void SeedData(KrusachContext context)
        {
            // Создаем базу, если её нет
            context.Database.EnsureCreated();

            // Если данные уже есть, ничего не делаем
            if (context.Categories.Any()) return;

            // 0. Пользователи
            if (!context.Users.Any())
            {
                context.Users.Add(new User
                {
                    Username = "admin",
                    // Хэш пароля "admin"
                    Password = "8c6976e5b5410415bde908bd4dee15dfb167a9c873fc4bb8a81f6f2ab448a918",
                    Role = "Admin",
                    FullName = "Системный Администратор",
                    IsApproved = true,
                    CreatedAt = DateTime.Now
                });
                context.SaveChanges();
            }

            // 1. Категории
            var categories = new List<Category>
            {
                new() { Name = "Электроника" },
                new() { Name = "Офисная мебель" },
                new() { Name = "Канцтовары" },
                new() { Name = "Хозтовары" },
                new() { Name = "Продукты (Длительного хранения)" }
            };
            context.Categories.AddRange(categories);
            context.SaveChanges();

            // 2. Контрагенты (Поставщики и Покупатели)
            var contractors = new List<Contractor>
            {
                new() { CompanyName = "ООО ТехноМир", Type = "Поставщик", Inn = "7701234567", Rating = 5, ContactInfo = "г. Москва, ул. Ленина 10" },
                new() { CompanyName = "ИП Иванов", Type = "Покупатель", Inn = "5001002003", Rating = 4, ContactInfo = "г. Химки, пр-т Мира 5" },
                new() { CompanyName = "ЗАО Снабжение", Type = "Поставщик", Inn = "7809988776", Rating = 3, ContactInfo = "г. Санкт-Петербург, Набережная 44" },
                new() { CompanyName = "Магазин 'У Дома'", Type = "Покупатель", Inn = "3312004455", Rating = 5, ContactInfo = "г. Владимир, ул. Садовая 1" },
                new() { CompanyName = "Офис Плюс", Type = "Покупатель", Inn = "6600112233", Rating = 2, ContactInfo = "г. Екатеринбург, ул. Строителей 12" }
            };
            context.Contractors.AddRange(contractors);
            context.SaveChanges();

            // 3. Товары
            var products = new List<Product>
            {
                new() { Name = "Ноутбук Pro 15", Sku = "NB-001", Category = categories[0], PurchasePrice = 45000, WholesalePrice = 52000, StockQuantity = 15 },
                new() { Name = "Мышь беспроводная", Sku = "ACC-01", Category = categories[0], PurchasePrice = 800, WholesalePrice = 1200, StockQuantity = 120 },
                new() { Name = "Монитор 27' 4K", Sku = "MON-27", Category = categories[0], PurchasePrice = 18000, WholesalePrice = 23500, StockQuantity = 8 },
                new() { Name = "Кресло офисное 'Комфорт'", Sku = "FUR-01", Category = categories[1], PurchasePrice = 5000, WholesalePrice = 7200, StockQuantity = 10 },
                new() { Name = "Стол письменный", Sku = "FUR-02", Category = categories[1], PurchasePrice = 3000, WholesalePrice = 4500, StockQuantity = 5 },
                new() { Name = "Бумага A4 (коробка)", Sku = "ST-A4", Category = categories[2], PurchasePrice = 1200, WholesalePrice = 1800, StockQuantity = 50 },
                new() { Name = "Набор ручек (12 шт)", Sku = "ST-PEN", Category = categories[2], PurchasePrice = 200, WholesalePrice = 350, StockQuantity = 200 },
                new() { Name = "Степлер металлический", Sku = "ST-STP", Category = categories[2], PurchasePrice = 350, WholesalePrice = 550, StockQuantity = 45 },
                new() { Name = "Швабра профессиональная", Sku = "HH-01", Category = categories[3], PurchasePrice = 600, WholesalePrice = 950, StockQuantity = 30 },
                new() { Name = "Стиральный порошок 5кг", Sku = "HH-DET", Category = categories[3], PurchasePrice = 450, WholesalePrice = 700, StockQuantity = 100 },
                new() { Name = "Кофе в зернах 1кг", Sku = "FOOD-CF", Category = categories[4], PurchasePrice = 900, WholesalePrice = 1400, StockQuantity = 25 },
                new() { Name = "Чай черный (100 пак)", Sku = "FOOD-TEA", Category = categories[4], PurchasePrice = 150, WholesalePrice = 280, StockQuantity = 300 },
                new() { Name = "Принтер лазерный", Sku = "OFF-PRN", Category = categories[0], PurchasePrice = 12000, WholesalePrice = 15500, StockQuantity = 12 },
                new() { Name = "Картридж черный", Sku = "OFF-CRT", Category = categories[0], PurchasePrice = 2500, WholesalePrice = 3800, StockQuantity = 40 },
                new() { Name = "Доска маркерная", Sku = "FUR-BRD", Category = categories[1], PurchasePrice = 2200, WholesalePrice = 3100, StockQuantity = 7 },
                new() { Name = "Лампа настольная LED", Sku = "FUR-LMP", Category = categories[1], PurchasePrice = 850, WholesalePrice = 1450, StockQuantity = 22 },
                new() { Name = "Папка-регистратор", Sku = "ST-FLD", Category = categories[2], PurchasePrice = 120, WholesalePrice = 210, StockQuantity = 150 },
                new() { Name = "Точилка электрическая", Sku = "ST-SHP", Category = categories[2], PurchasePrice = 450, WholesalePrice = 750, StockQuantity = 18 },
                new() { Name = "Мыло жидкое 5л", Sku = "HH-SOAP", Category = categories[3], PurchasePrice = 300, WholesalePrice = 520, StockQuantity = 60 },
                new() { Name = "Перчатки рабочие", Sku = "HH-GLV", Category = categories[3], PurchasePrice = 25, WholesalePrice = 55, StockQuantity = 500 },
                new() { Name = "Печенье ассорти 1кг", Sku = "FOOD-CK", Category = categories[4], PurchasePrice = 180, WholesalePrice = 320, StockQuantity = 85 }
            };
            context.Products.AddRange(products);
            context.SaveChanges();

            // 4. Заказы и детали (Генерируем за последние 7 дней для красивых графиков)
            var rnd = new Random();
            for (int i = 6; i >= 0; i--)
            {
                var date = DateTime.Now.Date.AddDays(-i);
                
                // Создаем 1-2 заказа на каждый день
                int ordersCount = rnd.Next(1, 3);
                for (int j = 0; j < ordersCount; j++)
                {
                    var isExpense = rnd.Next(0, 10) > 2; // Больше расходов (продаж) для графиков
                    var contractor = isExpense 
                        ? contractors.Where(c => c.Type == "Покупатель").OrderBy(c => rnd.Next()).First()
                        : contractors.Where(c => c.Type == "Поставщик").OrderBy(c => rnd.Next()).First();

                    var order = new Order
                    {
                        Contractor = contractor,
                        OperationType = isExpense ? "Расход" : "Приход",
                        OrderDate = date.AddHours(rnd.Next(9, 18)),
                        Status = "Завершен",
                        IsProcessed = true,
                        Discount = isExpense ? rnd.Next(0, 11) : 0
                    };
                    context.Orders.Add(order);
                    context.SaveChanges();

                    // Добавляем 1-3 товара в заказ
                    int detailsCount = rnd.Next(1, 4);
                    var selectedProducts = products.OrderBy(p => rnd.Next()).Take(detailsCount).ToList();

                    foreach (var p in selectedProducts)
                    {
                        var qty = rnd.Next(1, 6);
                        var price = isExpense ? p.WholesalePrice : p.PurchasePrice;
                        
                        context.OrderDetails.Add(new OrderDetail 
                        { 
                            Order = order, 
                            Product = p, 
                            Quantity = qty, 
                            UnitPrice = price 
                        });
                        
                        // Если расход - уменьшаем склад, если приход - увеличиваем
                        if (isExpense) p.StockQuantity -= qty;
                        else p.StockQuantity += qty;
                    }
                }
            }
            context.SaveChanges();
        }
    }
}
