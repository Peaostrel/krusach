using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using Microsoft.EntityFrameworkCore;
using krusach.Models;
using krusach.Services;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace krusach.ViewModels
{
    public class Notification
    {
        public string Message { get; set; } = "";
        public string Type { get; set; } = "Info"; // Info, Warning
    }

    public class CategoryStat
    {
        public string Name { get; set; } = "";
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    public class SalesStat
    {
        public string Date { get; set; } = "";
        public decimal Amount { get; set; }
        public double BarHeight { get; set; }
    }

    public class MainViewModel : BaseViewModel
    {
        private readonly KrusachContext _context;
        private readonly AuditService _auditService;
        private readonly ProductService _productService;
        private readonly OrderService _orderService;
        private readonly StatisticsService _statisticsService;

        private ObservableCollection<Product> _products = null!;
        private ObservableCollection<Category> _categories = null!;
        private ObservableCollection<Contractor> _contractors = null!;
        private ObservableCollection<Order> _orders = null!;
        private ObservableCollection<OrderDetail> _orderDetails = null!;
        private Order? _selectedOrder;
        private string _statusText = "Готов к работе";
        private int _totalProducts;
        private int _lowStockCount;
        private int _totalContractors;
        private decimal _totalSales;
        private decimal _totalProfit;
        private decimal _averageOrderValue;
        private string _searchTextProducts = "";
        private string _searchTextContractors = "";

        private bool _isLoggedIn = false;
        private string _username = "";
        private string _password = "";
        private string _registerFullName = "";
        private bool _isRegistering = false;
        private User _currentUser;
        public ObservableCollection<User> PendingUsers { get; set; } = new();

        public ICollectionView ProductsView { get; private set; } = null!;
        public ICollectionView CategoriesView { get; private set; } = null!;
        public ICollectionView ContractorsView { get; private set; } = null!;
        public ICollectionView OrdersView { get; private set; } = null!;
        
        public ObservableCollection<CategoryStat> CategoryStats { get; } = new();
        public ObservableCollection<AuditLog> AuditLogs { get; } = new();
        public ObservableCollection<Notification> Notifications { get; } = new();

        // LiveCharts properties
        private ISeries[] _salesSeries = Array.Empty<ISeries>();
        public ISeries[] SalesSeries { get => _salesSeries; set => SetProperty(ref _salesSeries, value); }

        private ISeries[] _profitSeries = Array.Empty<ISeries>();
        public ISeries[] ProfitSeries { get => _profitSeries; set => SetProperty(ref _profitSeries, value); }

        private Axis[] _xAxes = Array.Empty<Axis>();
        public Axis[] XAxes { get => _xAxes; set => SetProperty(ref _xAxes, value); }

        private string _searchTextLogs = "";
        public string SearchTextLogs { get => _searchTextLogs; set { if (SetProperty(ref _searchTextLogs, value)) AuditLogsView.Refresh(); } }

        private string _searchTextCategories = "";
        public string SearchTextCategories { get => _searchTextCategories; set { if (SetProperty(ref _searchTextCategories, value)) CategoriesView.Refresh(); } }

        private string _searchTextOrders = "";
        public string SearchTextOrders { get => _searchTextOrders; set { if (SetProperty(ref _searchTextOrders, value)) OrdersView.Refresh(); } }

        public ICollectionView AuditLogsView { get; private set; } = null!;

        private bool FilterLog(AuditLog l)
        {
            if (l == null) return false;
            return string.IsNullOrEmpty(SearchTextLogs) || 
                   (l.Username?.Contains(SearchTextLogs, StringComparison.OrdinalIgnoreCase) ?? false) ||
                   (l.Action?.Contains(SearchTextLogs, StringComparison.OrdinalIgnoreCase) ?? false) ||
                   (l.Details?.Contains(SearchTextLogs, StringComparison.OrdinalIgnoreCase) ?? false);
        }

        public MainViewModel()
        {
            _context = new KrusachContext();
            _auditService = new AuditService(_context);
            _productService = new ProductService(_context);
            _orderService = new OrderService(_context);
            _statisticsService = new StatisticsService(_context);

            Products = new ObservableCollection<Product>();
            Categories = new ObservableCollection<Category>();
            Contractors = new ObservableCollection<Contractor>();
            Orders = new ObservableCollection<Order>();
            OrderDetails = new ObservableCollection<OrderDetail>();
            
            InitializeViews();
            DatabaseInitializer.SeedData(_context);
            LoadData();
        }

        private void InitializeViews()
        {
            ProductsView = CollectionViewSource.GetDefaultView(Products);
            ProductsView.Filter = p => FilterProduct((Product)p);

            ContractorsView = CollectionViewSource.GetDefaultView(Contractors);
            ContractorsView.Filter = c => FilterContractor((Contractor)c);

            CategoriesView = CollectionViewSource.GetDefaultView(Categories);
            CategoriesView.Filter = c => FilterCategory((Category)c);

            OrdersView = CollectionViewSource.GetDefaultView(Orders);
            OrdersView.Filter = o => FilterOrder((Order)o);

            AuditLogsView = CollectionViewSource.GetDefaultView(AuditLogs);
            AuditLogsView.Filter = l => FilterLog((AuditLog)l);
        }

        private bool FilterCategory(Category c)
        {
            if (string.IsNullOrWhiteSpace(SearchTextCategories)) return true;
            return c.Name?.Contains(SearchTextCategories, StringComparison.OrdinalIgnoreCase) ?? true;
        }

        private bool FilterOrder(Order o)
        {
            if (string.IsNullOrWhiteSpace(SearchTextOrders)) return true;
            return o.Id.ToString().Contains(SearchTextOrders) || 
                   (o.Contractor?.CompanyName?.Contains(SearchTextOrders, StringComparison.OrdinalIgnoreCase) ?? false);
        }

        public ObservableCollection<Product> Products
        {
            get => _products;
            set => SetProperty(ref _products, value);
        }

        public ObservableCollection<Category> Categories
        {
            get => _categories;
            set => SetProperty(ref _categories, value);
        }

        public ObservableCollection<Contractor> Contractors
        {
            get => _contractors;
            set => SetProperty(ref _contractors, value);
        }

        public ObservableCollection<Order> Orders
        {
            get => _orders;
            set => SetProperty(ref _orders, value);
        }

        public ObservableCollection<OrderDetail> OrderDetails
        {
            get => _orderDetails;
            set => SetProperty(ref _orderDetails, value);
        }

        public Order? SelectedOrder
        {
            get => _selectedOrder;
            set
            {
                if (SetProperty(ref _selectedOrder, value))
                {
                    UpdateOrderDetails();
                }
            }
        }

        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        public int TotalProducts
        {
            get => _totalProducts;
            set => SetProperty(ref _totalProducts, value);
        }

        public int LowStockCount
        {
            get => _lowStockCount;
            set => SetProperty(ref _lowStockCount, value);
        }

        public int TotalContractors
        {
            get => _totalContractors;
            set => SetProperty(ref _totalContractors, value);
        }

        public decimal TotalSales
        {
            get => _totalSales;
            set => SetProperty(ref _totalSales, value);
        }

        public decimal TotalProfit
        {
            get => _totalProfit;
            set => SetProperty(ref _totalProfit, value);
        }

        public decimal AverageOrderValue
        {
            get => _averageOrderValue;
            set => SetProperty(ref _averageOrderValue, value);
        }

        public bool IsLoggedIn
        {
            get => _isLoggedIn;
            set => SetProperty(ref _isLoggedIn, value);
        }

        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public string RegisterFullName
        {
            get => _registerFullName;
            set => SetProperty(ref _registerFullName, value);
        }

        public bool IsRegistering
        {
            get => _isRegistering;
            set => SetProperty(ref _isRegistering, value);
        }

        public User CurrentUser
        {
            get => _currentUser;
            set => SetProperty(ref _currentUser, value);
        }

        private bool _rememberMe;
        public bool RememberMe
        {
            get => _rememberMe;
            set => SetProperty(ref _rememberMe, value);
        }

        public void Login()
        {
            try
            {
                string hashedPass = HashPassword(Password);
                var user = _context.Users.FirstOrDefault(u => u.Username == Username && u.Password == hashedPass);
                if (user != null)
                {
                    if (!user.IsApproved)
                    {
                        AddNotification("Ваш аккаунт еще не одобрен администратором", "Info");
                        return;
                    }

                    CurrentUser = user;
                    IsLoggedIn = true;
                    AddNotification($"Добро пожаловать, {user.FullName}!", "Success");
                    LogAction($"Вход в систему: {user.Username} ({user.Role})");
                    
                    if (RememberMe)
                    {
                        SaveSession(Username, hashedPass);
                    }
                    else
                    {
                        ClearSession();
                    }

                    if (user.Role == "Admin") LoadPendingUsers();
                }
                else
                {
                    AddNotification("Неверный логин или пароль", "Error");
                }
            }
            catch (Exception ex)
            {
                AddNotification("Ошибка БД при входе", "Error");
                LogAction($"Ошибка входа: {ex.Message}");
            }
        }

        private void SaveSession(string username, string hash)
        {
            try
            {
                var data = $"{username}|{hash}";
                System.IO.File.WriteAllText("session.dat", data);
            }
            catch { }
        }

        private void ClearSession()
        {
            if (System.IO.File.Exists("session.dat"))
                System.IO.File.Delete("session.dat");
        }

        public void TryAutoLogin()
        {
            try
            {
                if (System.IO.File.Exists("session.dat"))
                {
                    var data = System.IO.File.ReadAllText("session.dat").Split('|');
                    if (data.Length == 2)
                    {
                        var username = data[0];
                        var hash = data[1];
                        var user = _context.Users.FirstOrDefault(u => u.Username == username && u.Password == hash);
                        if (user != null && user.IsApproved)
                        {
                            Username = username;
                            CurrentUser = user;
                            IsLoggedIn = true;
                            RememberMe = true;
                            AddNotification($"Автоматический вход: {user.FullName}", "Success");
                            LogAction($"Авто-вход: {user.Username}");
                            if (user.Role == "Admin") LoadPendingUsers();
                        }
                    }
                }
            }
            catch { }
        }

        public void Register()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
                {
                    AddNotification("Заполните все поля", "Warning");
                    return;
                }

                if (_context.Users.Any(u => u.Username == Username))
                {
                    AddNotification("Логин уже занят", "Error");
                    return;
                }

                var newUser = new User
                {
                    Username = Username,
                    Password = HashPassword(Password),
                    FullName = RegisterFullName,
                    Role = "Manager",
                    IsApproved = false,
                    CreatedAt = DateTime.Now
                };

                _context.Users.Add(newUser);
                _context.SaveChanges();

                AddNotification("Заявка отправлена! Дождитесь одобрения админом.", "Success");
                IsRegistering = false;
                Username = "";
                Password = "";
                RegisterFullName = "";
            }
            catch (Exception ex)
            {
                AddNotification("Ошибка регистрации", "Error");
                LogAction($"Ошибка регистрации: {ex.Message}");
            }
        }

        public void LoadPendingUsers()
        {
            PendingUsers.Clear();
            var pending = _context.Users.Where(u => !u.IsApproved).ToList();
            foreach (var u in pending) PendingUsers.Add(u);
        }

        public void ApproveUser(User user, string role)
        {
            try
            {
                user.IsApproved = true;
                user.Role = role;
                _context.SaveChanges();
                LoadPendingUsers();
                AddNotification($"Пользователь {user.Username} одобрен!", "Success");
                LogAction($"Одобрен пользователь: {user.Username} как {role}");
            }
            catch (Exception ex)
            {
                AddNotification($"Ошибка при одобрении: {ex.Message}", "Error");
            }
        }

        public void Logout()
        {
            IsLoggedIn = false;
            CurrentUser = null;
            Password = "";
            ClearSession();
            AddNotification("Выход из системы", "Info");
        }

        public void LoadData()
        {
            try
            {
                _context.ChangeTracker.Clear();

                Products.Clear();
                foreach (var item in _productService.GetAllProducts()) Products.Add(item);

                Categories.Clear();
                foreach (var item in _productService.GetAllCategories()) Categories.Add(item);

                Contractors.Clear();
                foreach (var item in _context.Contractors.ToList()) Contractors.Add(item);

                Orders.Clear();
                foreach (var item in _orderService.GetAllOrders()) Orders.Add(item);

                OrderDetails.Clear();
                foreach (var item in _context.OrderDetails.Include(od => od.Product).ToList()) OrderDetails.Add(item);

                AuditLogs.Clear();
                foreach (var item in _auditService.GetLogs().Take(30)) AuditLogs.Add(item);

                UpdateStatistics();
                UpdateCharts();
                InitializeViews();
                LogAction("Данные синхронизированы с БД");
            }
            catch (Exception ex)
            {
                AddNotification($"Ошибка загрузки: {ex.Message}", "Warning");
            }
        }



        private void UpdateCharts()
        {
            try
            {
                var salesData = _statisticsService.GetSalesByDate(7);
                var profitData = _statisticsService.GetProfitByDate(7);

                // Создаем полный список за последние 7 дней
                var labels = new List<string>();
                var salesValues = new List<double>();
                var profitValues = new List<double>();

                for (int i = 6; i >= 0; i--)
                {
                    var date = DateTime.Today.AddDays(-i).ToString("dd.MM");
                    labels.Add(date);
                    salesValues.Add(salesData.ContainsKey(date) ? (double)salesData[date] : 0);
                    profitValues.Add(profitData.ContainsKey(date) ? (double)profitData[date] : 0);
                }

                XAxes = new Axis[]
                {
                    new Axis
                    {
                        Labels = labels.ToArray(),
                        LabelsPaint = new SolidColorPaint(SKColors.LightGray),
                        TextSize = 12,
                        SeparatorsPaint = new SolidColorPaint(SKColors.DimGray) { StrokeThickness = 0.5f }
                    }
                };

                SalesSeries = new ISeries[]
                {
                    new ColumnSeries<double>
                    {
                        Values = salesValues,
                        Name = "Выручка",
                        Fill = new SolidColorPaint(SKColor.Parse("#4ECDC4")),
                        Padding = 2,
                        MaxBarWidth = 40
                    }
                };

                ProfitSeries = new ISeries[]
                {
                    new LineSeries<double>
                    {
                        Values = profitValues,
                        Name = "Прибыль",
                        Stroke = new SolidColorPaint(SKColor.Parse("#FF6B6B")) { StrokeThickness = 4 },
                        Fill = new SolidColorPaint(SKColor.Parse("#33FF6B6B")),
                        GeometrySize = 10,
                        GeometryFill = new SolidColorPaint(SKColor.Parse("#FF6B6B")),
                        LineSmoothness = 0.4
                    }
                };
            }
            catch (Exception ex)
            {
                LogAction($"Ошибка обновления графиков: {ex.Message}");
            }
        }

        public void LogAction(string action, string details = "")
        {
            _auditService.Log(CurrentUser?.Username ?? "System", action, details);
            AuditLogs.Insert(0, new AuditLog 
            { 
                Timestamp = DateTime.Now, 
                Username = CurrentUser?.Username ?? "System", 
                Action = action, 
                Details = details 
            });
            if (AuditLogs.Count > 50) AuditLogs.RemoveAt(50);
        }

        public void ExportProductsToCsv(string filePath)
        {
            try
            {
                var lines = new System.Collections.Generic.List<string>();
                // Подсказка для Excel о разделителе (обязательно для WPS Office)
                lines.Add("sep=;");
                // Заголовки
                lines.Add("ID;Название;Артикул;Остаток;Цена");
                
                // Данные
                lines.AddRange(Products.Select(p => 
                    $"{p.Id};\"{p.Name?.Replace("\"", "\"\"")}\";\"{p.Sku?.Replace("\"", "\"\"")}\";{p.StockQuantity};{p.WholesalePrice}"));
                
                // Записываем с BOM для корректного отображения кириллицы в Excel
                System.IO.File.WriteAllLines(filePath, lines, new System.Text.UTF8Encoding(true));
                
                LogAction($"Товары экспортированы в {System.IO.Path.GetFileName(filePath)}");
                AddNotification("Экспорт завершен успешно", "Success");
            }
            catch (System.Exception ex)
            {
                AddNotification($"Ошибка экспорта: {ex.Message}", "Error");
            }
        }



        public string SearchTextProducts
        {
            get => _searchTextProducts;
            set
            {
                if (SetProperty(ref _searchTextProducts, value))
                    ProductsView.Refresh();
            }
        }

        public string SearchTextContractors
        {
            get => _searchTextContractors;
            set
            {
                if (SetProperty(ref _searchTextContractors, value))
                    ContractorsView.Refresh();
            }
        }

        private bool FilterProduct(Product p)
        {
            if (string.IsNullOrWhiteSpace(SearchTextProducts)) return true;
            return p.Name.Contains(SearchTextProducts, System.StringComparison.OrdinalIgnoreCase) ||
                   p.Sku.Contains(SearchTextProducts, System.StringComparison.OrdinalIgnoreCase);
        }

        private bool FilterContractor(Contractor c)
        {
            if (string.IsNullOrWhiteSpace(SearchTextContractors)) return true;
            return c.CompanyName.Contains(SearchTextContractors, System.StringComparison.OrdinalIgnoreCase) ||
                   c.Inn.Contains(SearchTextContractors, System.StringComparison.OrdinalIgnoreCase);
        }


        public void UpdateStatistics()
        {
            try
            {
                if (Products == null || Orders == null) return;

                TotalProducts = Products.Count;
                LowStockCount = Products.Count(p => p.StockQuantity < 10);
                TotalContractors = Contractors?.Count ?? 0;
                
                // Считаем общую выручку и прибыль (только расходные заказы)
                var saleOrders = Orders.Where(o => o != null && o.OperationType == "Расход").ToList();
                TotalSales = saleOrders.Sum(o => o?.TotalSum ?? 0);
                
                TotalProfit = saleOrders.Sum(o => 
                    o?.OrderDetails?.Sum(d => (d.UnitPrice - (d.Product?.PurchasePrice ?? 0)) * d.Quantity) ?? 0);

                AverageOrderValue = saleOrders.Any() ? TotalSales / saleOrders.Count : 0;
                
                OnPropertyChanged(nameof(TotalProducts));
                OnPropertyChanged(nameof(LowStockCount));
                OnPropertyChanged(nameof(TotalContractors));
                OnPropertyChanged(nameof(TotalSales));
                OnPropertyChanged(nameof(TotalProfit));
                OnPropertyChanged(nameof(AverageOrderValue));

                CategoryStats.Clear();
                if (Categories != null)
                {
                    var stats = Categories.Select(c => new CategoryStat
                    {
                        Name = c.Name ?? "Без категории",
                        Count = c.Products?.Count ?? 0
                    }).OrderByDescending(s => s.Count).Take(5).ToList();

                    int maxCount = stats.Any() ? stats.Max(s => s.Count) : 1;
                    if (maxCount == 0) maxCount = 1;

                    foreach (var s in stats)
                    {
                        s.Percentage = (double)s.Count / maxCount * 100;
                        CategoryStats.Add(s);
                    }
                }
                OnPropertyChanged(nameof(CategoryStats));

                if (IsLoggedIn)
                {
                    foreach (var p in Products.Where(p => p != null && p.StockQuantity < 5))
                    {
                        if (!Notifications.Any(n => n.Message.Contains(p.Name)))
                        {
                            AddNotification($"Критический остаток: {p.Name} ({p.StockQuantity} шт!)", "Warning");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogAction($"Ошибка обновления статистики: {ex.Message}");
            }
        }

        public async void AddNotification(string message, string type = "Info")
        {
            var notification = new Notification { Message = message, Type = type };
            Notifications.Insert(0, notification);
            LogAction($"Уведомление: {message}");
            
            // Если уведомлений слишком много, убираем старые сразу
            if (Notifications.Count > 3) Notifications.RemoveAt(Notifications.Count - 1);

            // Ждем 3 секунды и удаляем это конкретное уведомление
            await System.Threading.Tasks.Task.Delay(3000);
            if (Notifications.Contains(notification))
            {
                Notifications.Remove(notification);
            }
        }

        public void BulkUpdatePrices(decimal percent)
        {
            foreach (var p in Products)
            {
                p.WholesalePrice *= (1 + percent / 100);
            }
            LogAction($"Массовое обновление цен на {percent}%");
            AddNotification($"Цены обновлены на {percent}%");
            OnPropertyChanged(nameof(Products));
        }

        public void AutoReorder()
        {
            var lowStockProducts = Products.Where(p => p.StockQuantity < 5).ToList();
            if (!lowStockProducts.Any())
            {
                AddNotification("Нет товаров для автозакупки");
                return;
            }

            var newOrder = new Order
            {
                OperationType = "Приход",
                OrderDate = System.DateTime.Now,
                Status = "Черновик",
                ContractorId = Contractors.FirstOrDefault()?.Id
            };

            foreach (var p in lowStockProducts)
            {
                newOrder.OrderDetails.Add(new OrderDetail
                {
                    ProductId = p.Id,
                    Quantity = 20, // Закупаем по 20 штук
                    UnitPrice = p.PurchasePrice
                });
            }

            _context.Orders.Add(newOrder);
            Orders.Add(newOrder); // Добавляем в UI
            LogAction($"Автозакупка: создан черновик заказа для {lowStockProducts.Count} товаров");
            AddNotification("Черновик закупки создан");
            OnPropertyChanged(nameof(Orders));
            OrdersView.Refresh();
        }

        public void ExportPriceList(string filePath)
        {
            try
            {
                var lines = new System.Collections.Generic.List<string> { "ПРАЙС-ЛИСТ ОПТОВОЙ БАЗЫ" };
                lines.Add($"Дата: {System.DateTime.Now:dd.MM.yyyy}");
                lines.Add("------------------------------------");
                lines.Add("Товар;Артикул;Оптовая цена");
                lines.AddRange(Products.Select(p => $"{p.Name};{p.Sku};{p.WholesalePrice} ₽"));
                System.IO.File.WriteAllLines(filePath, lines, System.Text.Encoding.UTF8);
                LogAction("Прайс-лист сформирован");
                AddNotification("Прайс-лист сохранен");
            }
            catch (System.Exception ex)
            {
                LogAction($"Ошибка прайса: {ex.Message}");
            }
        }

        public void BackupData(string filePath)
        {
            try
            {
                var data = $"Products: {Products.Count}\nOrders: {Orders.Count}\nDate: {System.DateTime.Now}";
                System.IO.File.WriteAllText(filePath, data);
                LogAction($"Бэкап создан: {System.IO.Path.GetFileName(filePath)}");
                AddNotification("Резервная копия создана");
            }
            catch (System.Exception ex)
            {
                LogAction($"Ошибка бэкапа: {ex.Message}");
            }
        }

        // --- МЕТОДЫ ДОБАВЛЕНИЯ ---
        public void AddProduct(Product p) 
        { 
            if (p == null) return;
            Products.Add(p); 
            _context.Products.Add(p);
            SaveChanges(); 
            UpdateStatistics();
            AddNotification($"Товар '{p.Name}' добавлен", "Success");
        }
        public void AddCategory(Category c) 
        { 
            if (c == null) return;
            Categories.Add(c); 
            _context.Categories.Add(c);
            SaveChanges(); 
            UpdateStatistics();
            AddNotification($"Категория '{c.Name}' добавлена", "Success");
        }
        public void AddContractor(Contractor c) 
        { 
            if (c == null) return;
            Contractors.Add(c); 
            _context.Contractors.Add(c);
            SaveChanges(); 
            UpdateStatistics();
            AddNotification($"Контрагент '{c.CompanyName}' добавлен", "Success");
        }
        public void AddOrder(Order o) 
        { 
            if (o == null) return;
            Orders.Add(o); 
            _context.Orders.Add(o);
            SaveChanges(); 
            UpdateStatistics();
            AddNotification($"Заказ создан", "Success");
        }

        // --- МЕТОДЫ УДАЛЕНИЯ ---
        public void DeleteProduct(Product p) 
        { 
            if (p == null) return;
            if (_context.OrderDetails.Any(od => od.ProductId == p.Id))
            {
                AddNotification("Нельзя удалить: товар есть в заказах!", "Warning");
                return;
            }
            Products.Remove(p); SaveChanges(); UpdateStatistics(); UpdateCharts(); AddNotification("Товар удален", "Info"); 
        }
        public void DeleteCategory(Category c) 
        { 
            if (c == null) return;
            if (Products.Any(p => p.CategoryId == c.Id))
            {
                AddNotification("Нельзя удалить: в категории есть товары!", "Warning");
                return;
            }
            Categories.Remove(c); SaveChanges(); UpdateStatistics(); UpdateCharts(); AddNotification("Категория удалена", "Info"); 
        }
        public void DeleteContractor(Contractor c) 
        { 
            if (c == null) return;
            if (Orders.Any(o => o.ContractorId == c.Id))
            {
                AddNotification("Нельзя удалить: у контрагента есть заказы!", "Warning");
                return;
            }
            Contractors.Remove(c); SaveChanges(); UpdateStatistics(); UpdateCharts(); AddNotification("Контрагент удален", "Info"); 
        }
        public void DeleteOrder(Order o) 
        { 
            if (o != null) { Orders.Remove(o); SaveChanges(); UpdateStatistics(); UpdateCharts(); AddNotification("Заказ удален", "Info"); } 
        }

        public void SaveChanges()
        {
            try
            {
                // Логика автоматического обновления остатков
                foreach (var order in Orders.Where(o => o.Status == "Завершен" && !o.IsProcessed))
                {
                    foreach (var detail in order.OrderDetails)
                    {
                        var product = Products.FirstOrDefault(p => p.Id == detail.ProductId);
                        if (product != null)
                        {
                            if (order.OperationType == "Расход")
                            {
                                if (product.StockQuantity < detail.Quantity)
                                {
                                    throw new InvalidOperationException($"Недостаточно товара '{product.Name}' на складе! В наличии: {product.StockQuantity}, требуется: {detail.Quantity}.");
                                }
                                product.StockQuantity -= detail.Quantity;
                            }
                            else if (order.OperationType == "Приход")
                            {
                                product.StockQuantity += detail.Quantity;
                            }
                        }
                    }
                    order.IsProcessed = true; // Помечаем как обработанный
                    LogAction($"Заказ №{order.Id} проведен по складу. Остатки обновлены.");

                    // АВТО-ОТПРАВКА СЧЕТА НА EMAIL
                    if (order.Contractor != null && !string.IsNullOrEmpty(order.Contractor.Email))
                    {
                        var emailService = new Services.EmailService();
                        string contractorEmail = order.Contractor.Email;
                        int orderId = order.Id;
                        
                        // Проверка Email перед отправкой
                        if (string.IsNullOrWhiteSpace(contractorEmail))
                        {
                            App.Current.Dispatcher.Invoke(() => LogAction($"Предупреждение: у контрагента для заказа №{orderId} не указан Email"));
                            return;
                        }

                        // Запускаем в фоне, чтобы не тормозить UI
                        System.Threading.Tasks.Task.Run(async () => {
                            try {
                                await emailService.SendInvoiceAsync(contractorEmail, orderId);
                                App.Current.Dispatcher.Invoke(() => LogAction($"Счет для заказа №{orderId} автоматически отправлен на {contractorEmail}"));
                            } catch (Exception ex) {
                                App.Current.Dispatcher.Invoke(() => LogAction($"Ошибка авто-отправки счета: {ex.Message}"));
                            }
                        });
                    }
                }
                // Очистка и проверка данных перед сохранением
                foreach (var order in Orders.ToList())
                {
                    if (order.OrderDetails == null) continue;
                    var invalidDetails = order.OrderDetails.Where(d => d.ProductId == null || d.ProductId == 0 || d.Quantity == null || d.Quantity <= 0).ToList();
                    foreach (var d in invalidDetails) 
                    {
                        order.OrderDetails.Remove(d);
                        if (d.Id > 0) _context.OrderDetails.Remove(d); // Удаляем из БД, если строка уже существовала
                    }

                    foreach (var detail in order.OrderDetails)
                    {
                        if (detail.UnitPrice == null || detail.UnitPrice == 0)
                        {
                            var prod = Products.FirstOrDefault(p => p.Id == detail.ProductId);
                            if (prod != null) detail.UnitPrice = prod.WholesalePrice;
                        }
                    }
                }

                _context.SaveChanges();
                
                // Принудительно обновляем суммы заказов для UI
                foreach (var order in Orders)
                {
                    if (order != null) order.RefreshTotalSum();
                }
                
                UpdateStatistics();
                UpdateCharts();
                OnPropertyChanged(nameof(Products));

                LogAction("Все изменения сохранены в БД");
                StatusText = "Все изменения сохранены";
            }
            catch (System.Exception ex)
            {
                var fullError = ex.Message;
                if (ex.InnerException != null) fullError += "\nПодробности: " + ex.InnerException.Message;
                
                LogAction($"Ошибка сохранения: {fullError}");
                StatusText = "Ошибка сохранения!";
                MessageBox.Show(fullError, "Ошибка базы данных");
            }
        }

        private void UpdateOrderDetails()
        {
            if (SelectedOrder == null) return;
            OnPropertyChanged(nameof(FilteredOrderDetails));
        }

        public System.Collections.Generic.IEnumerable<OrderDetail> FilteredOrderDetails
        {
            get
            {
                if (SelectedOrder == null) return Enumerable.Empty<OrderDetail>();
                return OrderDetails.Where(od => od.Order == SelectedOrder || (od.OrderId != 0 && od.OrderId == SelectedOrder.Id));
            }
        }

        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes) builder.Append(b.ToString("x2"));
                return builder.ToString();
            }
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
