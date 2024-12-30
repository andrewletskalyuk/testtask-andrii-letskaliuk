using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data;
using TestTask.Data;
using TestTask.Data.Entities;

namespace TestTask.Services;

public class MarketService : IMarketService
{
    readonly TestDbContext _testDbContext;
    readonly ILogger<MarketService> _logger;

    public MarketService(TestDbContext testDbContext, ILogger<MarketService> logger)
    {
        _testDbContext = testDbContext;
        _logger = logger;
    }

    public async Task BuyAsync(int userId, int itemId)
    {
        try
        {
            using var transaction = await _testDbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            var user = await _testDbContext.Users.FirstOrDefaultAsync(n => n.Id == userId);

            if (user == null)
                throw new Exception("User not found");

            var item = await _testDbContext.Items.FirstOrDefaultAsync(n => n.Id == itemId);
            if (item == null)
                throw new Exception("Item not found");

            if (user.Balance < item.Cost)
                throw new Exception("Not enough balance");

            var existingPurchase = await _testDbContext.UserItems
                .AsNoTracking()
                .FirstOrDefaultAsync(ui => ui.UserId == userId && ui.ItemId == itemId);

            if (existingPurchase != null)
                throw new Exception("Item already purchased.");

            user.Balance -= item.Cost;

            await _testDbContext.UserItems.AddAsync(new UserItem
            {
                UserId = userId,
                ItemId = itemId,
                PurchaseDate = DateTime.UtcNow, //added after migration
            });

            await transaction.CommitAsync();
            await _testDbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during purchase operation.");
            throw;
        }
    }

    public async Task<IEnumerable<object>> GetUserPurchasesForTodayAsync(int userId)
    {
        var purchases = await _testDbContext.UserItems
            .Include(ui => ui.User)
            .Include(ui => ui.Item)
            .Where(ui => ui.UserId == userId && ui.PurchaseDate.Date == DateTime.Today)
            .Select(ui => new
            {
                UserId = ui.UserId,
                UserEmail = ui.User.Email,
                ItemId = ui.ItemId,
                ItemName = ui.Item.Name,
                PurchaseDate = ui.PurchaseDate
            })
            .ToListAsync();

        return purchases;
    }

    public async Task<IEnumerable<object>> GetTopPopularItemsPerYearAsync()
    {
        var result = await _testDbContext.UserItems
                .GroupBy(ui => new { ui.PurchaseDate.Year, ui.ItemId, ui.Item.Name, ui.PurchaseDate.Date }) 
                .Select(g => new
                {
                    Year = g.Key.Year,
                    ItemId = g.Key.ItemId,
                    ItemName = g.Key.Name,
                    Date = g.Key.Date,
                    TotalPurchases = g.Count()
                })
                .GroupBy(g => new { g.Year, g.ItemId, g.ItemName }) 
                .Select(g => new
                {
                    Year = g.Key.Year,
                    ItemName = g.Key.ItemName,
                    MaxPurchasesOnSingleDay = g.Max(x => x.TotalPurchases) 
                })
                .OrderByDescending(g => g.MaxPurchasesOnSingleDay) 
                .GroupBy(g => g.Year)
                .SelectMany(g => g.Take(3)) 
                .ToListAsync();

        return result;
    }
}