using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using krusach.Models;

namespace krusach.Services
{
    public class OrderService
    {
        private readonly KrusachContext _context;

        public OrderService(KrusachContext context)
        {
            _context = context;
        }

        public List<Order> GetAllOrders()
        {
            return _context.Orders
                .Include(o => o.Contractor)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .ToList();
        }

        public void SaveOrder(Order order)
        {
            if (order.Id == 0)
                _context.Orders.Add(order);
            else
                _context.Entry(order).State = EntityState.Modified;

            // Обработка деталей
            foreach (var detail in order.OrderDetails)
            {
                detail.OrderId = order.Id; // Привязываем деталь к заказу
                
                if (detail.Id == 0)
                    _context.OrderDetails.Add(detail);
                else
                    _context.Entry(detail).State = EntityState.Modified;
            }

            _context.SaveChanges();
        }

        public void ProcessStock(Order order)
        {
            if (order.Status != "Завершен" || order.IsProcessed) return;

            foreach (var detail in order.OrderDetails)
            {
                var product = _context.Products.Find(detail.ProductId);
                if (product == null) continue;

                if (order.OperationType == "Расход")
                {
                    if (product.StockQuantity < detail.Quantity)
                        throw new InvalidOperationException($"Недостаточно товара '{product.Name}' на складе!");
                    
                    product.StockQuantity -= detail.Quantity;
                }
                else if (order.OperationType == "Приход")
                {
                    product.StockQuantity += detail.Quantity;
                }
            }
            order.IsProcessed = true;
            _context.SaveChanges();
        }

        public void DeleteOrder(Order order)
        {
            _context.Orders.Remove(order);
            _context.SaveChanges();
        }
    }
}
