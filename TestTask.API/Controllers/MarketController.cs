using Microsoft.AspNetCore.Mvc;
using TestTask.Services;

namespace TestTask.API.Controllers;

[ApiController]
[Route("[controller]")]
public class MarketController : ControllerBase
{
    readonly IMarketService _marketService;

    public MarketController(IMarketService marketService)
    {
        _marketService = marketService;
    }

    [HttpPost]
    public async Task BuyAsync(int userId, int itemId)
    {
        await _marketService.BuyAsync(userId, itemId);
    }

    [HttpGet("purchases/{userId}")]
    public async Task GetPurchasesAsync(int userId)
    {
        await _marketService.GetUserPurchasesForTodayAsync(userId);
    }

    [HttpGet("popular-items")]
    public async Task GetPopularItemsReportAsync()
    {
        await _marketService.GetTopPopularItemsPerYearAsync();
    }
}