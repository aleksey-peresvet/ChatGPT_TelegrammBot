using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace myChatGptTelegramBot.DTO_Models
{
    public class TaskItem
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public DateTime? Deadline { get; set; }
        public string? ReminderFrequency { get; set; } // "ежедневно", "еженедельно"
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
