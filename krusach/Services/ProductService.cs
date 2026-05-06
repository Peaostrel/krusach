using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using krusach.Models;

namespace krusach.Services
{
    public class ProductService
    {
        private readonly KrusachContext _context;

        public ProductService(KrusachContext context)
        {
            _context = context;
        }

        public List<Product> GetAllProducts()
        {
            return _context.Products.Include(p => p.Category).ToList();
        }

        public List<Category> GetAllCategories()
        {
            return _context.Categories.ToList();
        }

        public void SaveProduct(Product product)
        {
            if (product.Id == 0)
                _context.Products.Add(product);
            else
                _context.Entry(product).State = EntityState.Modified;
            
            _context.SaveChanges();
        }

        public void DeleteProduct(Product product)
        {
            _context.Products.Remove(product);
            _context.SaveChanges();
        }

        public List<Product> GetLowStockProducts(int threshold = 10)
        {
            return _context.Products.Where(p => p.StockQuantity < threshold).ToList();
        }
    }
}
