using Microsoft.EntityFrameworkCore;
using myChatGptTelegramBot.DTO_Models;

namespace myChatGptTelegramBot.Services
{
    public class PurchaseService
    {
        private readonly AppDbContext _context;

        public PurchaseService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Purchase> CreateAsync(Purchase purchase)
        {
            _context.Purchases.Add(purchase);
            await _context.SaveChangesAsync();
            return purchase;
        }

        public async Task<List<Purchase>> GetAllAsync()
        {
            return await _context.Purchases.ToListAsync();
        }

        public async Task<Purchase?> GetByIdAsync(int id)
        {
            return await _context.Purchases.FindAsync(id);
        }

        public async Task<Purchase> UpdateAsync(Purchase purchase)
        {
            _context.Purchases.Update(purchase);
            await _context.SaveChangesAsync();
            return purchase;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var purchase = await _context.Purchases.FindAsync(id);
            if (purchase == null) return false;

            _context.Purchases.Remove(purchase);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
