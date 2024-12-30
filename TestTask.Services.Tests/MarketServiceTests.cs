using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TestTask.Data;
using TestTask.Data.Entities;

namespace TestTask.Services.Tests;

public class MarketServiceTests
{
    readonly TestDbContext _dbContext;
    readonly Mock<ILogger<MarketService>> _loggerMock;
    readonly IMarketService _marketService;

    public MarketServiceTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        _dbContext = new TestDbContext(options);
        _dbContext.Database.OpenConnection();
        _dbContext.Database.EnsureCreated();

        _loggerMock = new Mock<ILogger<MarketService>>();
        _marketService = new MarketService(_dbContext, _loggerMock.Object);
    }

    private async Task SeedDatabase()
    {
        var users = new List<User>
            {
                new User { Id = 1, Email = "user1@example.com", Balance = 100 },
                new User { Id = 2, Email = "user2@example.com", Balance = 10 }
            };

        var items = new List<Item>
            {
                new Item { Id = 1, Name = "Item1", Cost = 50 },
                new Item { Id = 2, Name = "Item2", Cost = 75 },
                new Item { Id = 3, Name = "Item3", Cost = 100 }
            };

        _dbContext.Users.AddRange(users);
        _dbContext.Items.AddRange(items);

        await _dbContext.SaveChangesAsync();
    }


    [Fact]
    public async Task BuyAsync_ValidPurchase_ShouldDeductBalanceAndAddUserItem()
    {
        // Arrange
        await SeedDatabase();

        // Act
        await _marketService.BuyAsync(1, 1);

        // Assert
        var user = await _dbContext.Users.FirstAsync();
        Assert.Equal(50, user.Balance);

        var userItem = await _dbContext.UserItems.FirstOrDefaultAsync();
        Assert.NotNull(userItem);
        Assert.Equal(1, userItem.UserId);
        Assert.Equal(1, userItem.ItemId);
    }

    [Fact]
    public async Task BuyAsync_InsufficientBalance_ShouldThrowException()
    {
        // Arrange
        await SeedDatabase();
        var user = await _dbContext.Users.FirstAsync();
        user.Balance = 0;
        await _dbContext.SaveChangesAsync();

        // Act
        var exception = await Assert.ThrowsAsync<Exception>(async () => await _marketService.BuyAsync(2, 2));

        // Assert
        Assert.Equal("Not enough balance", exception.Message);
    }

    [Fact]
    public async Task GetUserPurchasesForTodayAsync_ShouldReturnPurchasesForToday()
    {
        // Arrange
        await SeedDatabase();
        _dbContext.UserItems.Add(new UserItem
        {
            UserId = 1,
            ItemId = 1,
            PurchaseDate = DateTime.Today
        });
        await _dbContext.SaveChangesAsync();

        // Act
        var purchases = await _marketService.GetUserPurchasesForTodayAsync(1);

        // Assert
        Assert.Single(purchases);
    }

    [Fact]
    public async Task GetTopPopularItemsPerYearAsync_ShouldReturnTopItems()
    {
        // Arrange
        await SeedDatabase();

        _dbContext.UserItems.AddRange(
            new UserItem { UserId = 1, ItemId = 1, PurchaseDate = DateTime.Today },
            new UserItem { UserId = 1, ItemId = 2, PurchaseDate = DateTime.Today },
            new UserItem { UserId = 2, ItemId = 2, PurchaseDate = DateTime.Today },
            new UserItem { UserId = 2, ItemId = 3, PurchaseDate = DateTime.Today }
        );

        await _dbContext.SaveChangesAsync();

        // Act
        var topItems = await _marketService.GetTopPopularItemsPerYearAsync();

        // Assert
        Assert.NotEmpty(topItems);
    }

    [Fact]
    public void Dispose()
    {
        _dbContext.Dispose();
    }

    ///more popular write tests with FluentAssertions
    [Fact]
    public async Task BuyAsync1_ValidPurchase_ShouldDeductBalanceAndAddUserItem()
    {
        // Arrange
        await SeedDatabase();

        // Act
        await _marketService.BuyAsync(1, 1);

        // Assert
        var user = await _dbContext.Users.FirstAsync();
        user.Should().NotBeNull();
        user.Balance.Should().Be(50);

        var userItem = await _dbContext.UserItems.FirstOrDefaultAsync();
        userItem.Should().NotBeNull();
        userItem!.UserId.Should().Be(1);
        userItem.ItemId.Should().Be(1);
    }

    [Fact]
    public async Task BuyAsync1_InsufficientBalance_ShouldThrowException()
    {
        // Arrange
        await SeedDatabase();
        var user = await _dbContext.Users.FirstAsync();
        user.Balance = 0;
        await _dbContext.SaveChangesAsync();

        // Act
        Func<Task> act = async () => await _marketService.BuyAsync(2, 2);

        // Assert
        await act.Should().ThrowAsync<Exception>()
            .WithMessage("Not enough balance");
    }

    [Fact]
    public async Task GetUserPurchasesForTodayAsync1_ShouldReturnPurchasesForToday()
    {
        // Arrange
        await SeedDatabase();
        _dbContext.UserItems.Add(new UserItem
        {
            UserId = 1,
            ItemId = 1,
            PurchaseDate = DateTime.Today
        });
        await _dbContext.SaveChangesAsync();

        // Act
        var purchases = await _marketService.GetUserPurchasesForTodayAsync(1);

        // Assert
        purchases.Should().HaveCount(1);
        purchases.First().Should().Match<object>(p =>
            (int)p.GetType().GetProperty("UserId")!.GetValue(p)! == 1 &&
            (string)p.GetType().GetProperty("UserEmail")!.GetValue(p)! == "user1@example.com");
    }

    [Fact]
    public async Task GetTopPopularItemsPerYearAsync1_ShouldReturnTopItems()
    {
        // Arrange
        await SeedDatabase();

        _dbContext.UserItems.AddRange(
            new UserItem { UserId = 1, ItemId = 1, PurchaseDate = DateTime.Today },
            new UserItem { UserId = 1, ItemId = 2, PurchaseDate = DateTime.Today },
            new UserItem { UserId = 2, ItemId = 2, PurchaseDate = DateTime.Today },
            new UserItem { UserId = 2, ItemId = 3, PurchaseDate = DateTime.Today }
        );

        await _dbContext.SaveChangesAsync();

        // Act
        var topItems = await _marketService.GetTopPopularItemsPerYearAsync();

        // Assert
        topItems.Should().NotBeEmpty();
    }
}
