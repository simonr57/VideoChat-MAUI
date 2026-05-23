using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Database
{
    public class AppDbContext : DbContext
    {
        public DbSet<dBMessage> Messages { get; set; }
        public DbSet<dBImage> Images { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }
    }

    public class dBMessage
    {
        [Key]
        public required string MessageId { get; set; }
        public required string SenderId { get; set; }
        public required string ReceiverId { get; set; }
        public required string Content { get; set; }
        public required string SelectedReceiverId { get; set; }
        public required bool IsClient { get; set; }
        public required bool IsHidden { get; set; }
        public bool IsView { get; set; }
        public bool IsRead { get; set; }
        public bool IsDelivered { get; set; }
        public required DateTime Timestamp { get; set; }
    }

    public class dBImage
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long dBImageId { get; set; }
        public required string SelectedReceiverId { get; set; }
        public required DateTime Timestamp { get; set; }
        public required byte[] ImageOriginal { get; set; }
    }
}
