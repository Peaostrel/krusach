using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;

namespace krusach.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public List<Product> Products { get; set; } = new();
    }

    public class Contractor : krusach.ViewModels.BaseViewModel, System.ComponentModel.IDataErrorInfo
    {
        private string? _companyName;
        private string? _inn;

        public int Id { get; set; }
        public string? Type { get; set; }
        
        public string? CompanyName 
        { 
            get => _companyName; 
            set => SetProperty(ref _companyName, value); 
        }

        public string? Inn 
        { 
            get => _inn; 
            set => SetProperty(ref _inn, value); 
        }

        public string? ContactInfo { get; set; } = "";
        public string? Email { get; set; } = "";
        private int? _rating;
        public int? Rating 
        { 
            get => _rating; 
            set => SetProperty(ref _rating, value); 
        }
        public List<Order> Orders { get; set; } = new();

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public string this[string columnName]
        {
            get
            {
                if (columnName == nameof(CompanyName) && string.IsNullOrWhiteSpace(CompanyName)) return "Название компании обязательно";
                if (columnName == nameof(Inn) && (Inn?.Length < 10 || Inn?.Length > 12)) return "ИНН должен быть 10-12 цифр";
                return string.Empty;
            }
        }

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public string Error => string.Empty;
    }

    public class Product : krusach.ViewModels.BaseViewModel, System.ComponentModel.IDataErrorInfo
    {
        private string _name = "";
        private string _sku = "";
        private decimal _purchasePrice;
        private decimal _wholesalePrice;
        private int _stockQuantity;
        private string? _imagePath;
        private int? _categoryId;

        public int Id { get; set; }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string Sku
        {
            get => _sku;
            set => SetProperty(ref _sku, value);
        }

        public int? CategoryId 
        { 
            get => _categoryId; 
            set => SetProperty(ref _categoryId, value); 
        }
        
        public Category? Category { get; set; }

        public decimal PurchasePrice
        {
            get => _purchasePrice;
            set => SetProperty(ref _purchasePrice, value);
        }

        public decimal WholesalePrice
        {
            get => _wholesalePrice;
            set => SetProperty(ref _wholesalePrice, value);
        }

        public int StockQuantity
        {
            get => _stockQuantity;
            set => SetProperty(ref _stockQuantity, value);
        }

        public string? ImagePath
        {
            get => _imagePath;
            set => SetProperty(ref _imagePath, value);
        }

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public int DaysRemaining => StockQuantity > 0 ? StockQuantity / 2 : 0;

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public string this[string columnName]
        {
            get
            {
                if (columnName == nameof(Name) && string.IsNullOrWhiteSpace(Name)) return "Название обязательно";
                if (columnName == nameof(Sku) && string.IsNullOrWhiteSpace(Sku)) return "Артикул обязателен";
                if (columnName == nameof(PurchasePrice) && PurchasePrice <= 0) return "Цена закупки должна быть > 0";
                if (columnName == nameof(WholesalePrice) && WholesalePrice <= PurchasePrice) return "Оптовая цена должна быть выше цены закупки";
                if (columnName == nameof(StockQuantity) && StockQuantity < 0) return "Количество не может быть отрицательным";
                return string.Empty;
            }
        }

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public string Error => string.Empty;
    }

    public class Order : krusach.ViewModels.BaseViewModel, System.ComponentModel.IDataErrorInfo
    {
        public int Id { get; set; }
        public int? ContractorId { get; set; } // Делаем nullable
        public Contractor? Contractor { get; set; }
        public string? OperationType { get; set; } = "";
        public DateTime OrderDate { get; set; }
        
        private string? _status = "";
        public string? Status 
        { 
            get => _status; 
            set => SetProperty(ref _status, value); 
        }

        private decimal _discount;
        public decimal Discount 
        { 
            get => _discount; 
            set { if (SetProperty(ref _discount, value)) RefreshTotalSum(); } 
        }
        public bool IsProcessed { get; set; } = false;
        public ObservableCollection<OrderDetail> OrderDetails { get; set; } = new();

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public string this[string columnName]
        {
            get
            {
                if (columnName == nameof(ContractorId) && (ContractorId == null || ContractorId == 0)) return "Выберите контрагента";
                if (columnName == nameof(Discount) && (Discount < 0 || Discount > 100)) return "Скидка от 0 до 100";
                return string.Empty;
            }
        }

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public string Error => string.Empty;

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public decimal TotalSum
        {
            get
            {
                var sum = OrderDetails.Sum(od => od.Quantity * od.UnitPrice);
                return sum * (1 - Discount / 100);
            }
        }

        public void RefreshTotalSum()
        {
            OnPropertyChanged(nameof(TotalSum));
        }
    }

    public class OrderDetail : krusach.ViewModels.BaseViewModel
    {
        private int _quantity;
        private decimal _unitPrice;

        public int Id { get; set; }
        public int OrderId { get; set; }
        public Order Order { get; set; }
        public int? ProductId { get; set; }
        public Product? Product { get; set; }

        public int Quantity 
        { 
            get => _quantity; 
            set { if (SetProperty(ref _quantity, value)) Order?.RefreshTotalSum(); } 
        }

        public decimal UnitPrice 
        { 
            get => _unitPrice; 
            set { if (SetProperty(ref _unitPrice, value)) Order?.RefreshTotalSum(); } 
        }
    }

    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string Role { get; set; } = "Manager"; // Admin, Manager
        public string FullName { get; set; } = "";
        public bool IsApproved { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class KrusachContext : DbContext
    {
        public DbSet<Category> Categories { get; set; }
        public DbSet<Contractor> Contractors { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var connString = krusach.Services.ConfigHelper.GetConnectionString("DefaultConnection");
            optionsBuilder.UseSqlServer(connString, builder =>
            {
                builder.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
            });
        }
    }
}