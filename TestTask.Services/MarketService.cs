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
        var data = await _testDbContext.UserItems
            .Include(ui => ui.Item)
            .Select(ui => new
            {
                Year = ui.PurchaseDate.Year,
                ItemId = ui.ItemId,
                ItemName = ui.Item.Name,
                PurchaseDate = ui.PurchaseDate.Date
            })
            .ToListAsync();

        var result = data
            .GroupBy(ui => new { ui.Year, ui.ItemId, ui.ItemName })
            .Select(g => new
            {
                Year = g.Key.Year,
                ItemName = g.Key.ItemName,
                MaxPurchasesOnSingleDay = g
                    .GroupBy(x => x.PurchaseDate)
                    .Max(x => x.Count())
            })
            .OrderByDescending(g => g.MaxPurchasesOnSingleDay)
            .GroupBy(g => g.Year)
            .SelectMany(g => g.Take(3))
            .ToList();

        return result;
    }
}