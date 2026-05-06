using System;
using System.ComponentModel.DataAnnotations;

namespace krusach.Models
{
    public class AuditLog
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public DateTime Timestamp { get; set; }
        
        [Required]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        public string Action { get; set; } = string.Empty;
        
        public string Details { get; set; } = string.Empty;
    }
}
