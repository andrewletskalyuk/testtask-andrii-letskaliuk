namespace TestTask.Services;

public interface IMarketService
{
    /// <summary>
    /// Buying proccess
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="itemId"></param>
    /// <returns></returns>
    Task BuyAsync(int userId, int itemId);

    /// <summary>
    /// Get purchases
    /// </summary>
    /// <returns></returns>
    Task<IEnumerable<object>> GetUserPurchasesForTodayAsync(int userId);

    /// <summary>
    /// Get top popular items per year
    /// </summary>
    /// <returns></returns>
    Task<IEnumerable<object>> GetTopPopularItemsPerYearAsync();
}
