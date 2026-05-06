using System;
using System.Linq;
using krusach.Models;

namespace krusach.Services
{
    public class AuditService
    {
        private readonly KrusachContext _context;

        public AuditService(KrusachContext context)
        {
            _context = context;
        }

        public void Log(string username, string action, string details = "")
        {
            try
            {
                var log = new AuditLog
                {
                    Timestamp = DateTime.Now,
                    Username = username,
                    Action = action,
                    Details = details
                };
                
                _context.AuditLogs.Add(log);
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to log action: {ex.Message}");
            }
        }

        public IQueryable<AuditLog> GetLogs()
        {
            return _context.AuditLogs.OrderByDescending(l => l.Timestamp);
        }
    }
}
