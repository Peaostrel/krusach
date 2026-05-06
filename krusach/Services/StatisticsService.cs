using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using krusach.Models;

namespace krusach.Services
{
    public class StatisticsService
    {
        private readonly KrusachContext _context;

        public StatisticsService(KrusachContext context)
        {
            _context = context;
        }

        public decimal GetTotalSales()
        {
            return _context.OrderDetails
                .Where(od => od.Order.OperationType == "Расход" && od.Order.Status == "Завершен")
                .Sum(od => od.Quantity * od.UnitPrice);
        }

        public decimal GetTotalProfit()
        {
            return _context.OrderDetails
                .Where(od => od.Order.OperationType == "Расход" && od.Order.Status == "Завершен")
                .Sum(od => (od.UnitPrice - (od.Product != null ? od.Product.PurchasePrice : 0)) * od.Quantity);
        }

        public Dictionary<string, int> GetProductCountByCategory()
        {
            return _context.Products
                .GroupBy(p => p.Category != null ? p.Category.Name ?? "Без категории" : "Без категории")
                .ToDictionary(g => g.Key, g => g.Count());
        }

        public Dictionary<string, decimal> GetSalesByDate(int days = 7)
        {
            var startDate = DateTime.Today.AddDays(-days + 1);
            return _context.Orders
                .Where(o => o.OperationType == "Расход" && o.Status == "Завершен" && o.OrderDate >= startDate)
                .AsEnumerable() // Переходим в память для группировки по дате без времени
                .GroupBy(o => o.OrderDate.Date)
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key.ToString("dd.MM"), g => g.Sum(o => o.OrderDetails.Sum(od => od.Quantity * od.UnitPrice)));
        }

        public Dictionary<string, decimal> GetProfitByDate(int days = 7)
        {
            var startDate = DateTime.Today.AddDays(-days + 1);
            return _context.OrderDetails
                .Where(od => od.Order.OperationType == "Расход" && od.Order.Status == "Завершен" && od.Order.OrderDate >= startDate)
                .Include(od => od.Product)
                .AsEnumerable()
                .GroupBy(od => od.Order.OrderDate.Date)
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key.ToString("dd.MM"), g => g.Sum(od => (od.UnitPrice - (od.Product?.PurchasePrice ?? 0)) * od.Quantity));
        }
    }
}
