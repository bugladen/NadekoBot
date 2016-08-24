using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Services.Database.Models
{
    public class DbEntity
    {
        [Key]
        public int Id { get; set; }
        public DateTime DateAdded { get; } = DateTime.UtcNow;
    }
}
