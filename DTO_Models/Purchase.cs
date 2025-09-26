using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace myChatGptTelegramBot.DTO_Models
{
    public class Purchase
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public decimal? Cost { get; set; }
        public string? Purpose { get; set; } // "еда", "транспорт" и т.д.
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
