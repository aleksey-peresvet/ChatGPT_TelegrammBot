using Microsoft.EntityFrameworkCore;
using myChatGptTelegramBot.DTO_Models;

namespace myChatGptTelegramBot.Services
{
    public class TaskItemService
    {
        private readonly AppDbContext _context;

        public TaskItemService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<TaskItem> CreateAsync(TaskItem taskItem)
        {
            _context.Tasks.Add(taskItem);
            await _context.SaveChangesAsync();
            return taskItem;
        }

        public async Task<List<TaskItem>> GetAllAsync()
        {
            return await _context.Tasks.ToListAsync();
        }

        public async Task<TaskItem?> GetByIdAsync(int id)
        {
            return await _context.Tasks.FindAsync(id);
        }

        public async Task<TaskItem> UpdateAsync(TaskItem taskItem)
        {
            _context.Tasks.Update(taskItem);
            await _context.SaveChangesAsync();
            return taskItem;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var taskItem = await _context.Tasks.FindAsync(id);
            if (taskItem == null) return false;

            _context.Tasks.Remove(taskItem);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
