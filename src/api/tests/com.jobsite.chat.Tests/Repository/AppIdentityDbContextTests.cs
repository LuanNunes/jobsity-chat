using com.jobsite.chat.Domain.Entities;
using com.jobsite.chat.Repository.Persistence;
using com.jobsite.chat.Repository.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace com.jobsite.chat.Tests.Repository;

public sealed class AppIdentityDbContextTests
{

    [Fact]
    public async Task ApplicationUser_RoundTrip_PreservesDisplayName()
    {
        using SqliteInMemoryFixture fixture = new();

        string userId = Guid.CreateVersion7().ToString();
        await using (AppIdentityDbContext writeContext = fixture.NewIdentityContext())
        {
            ApplicationUser user = new()
            {
                Id = userId,
                UserName = "ana",
                NormalizedUserName = "ANA",
                Email = "ana@example.com",
                DisplayName = "Ana Silva",
            };
            writeContext.Users.Add(user);
            await writeContext.SaveChangesAsync();
        }

        await using AppIdentityDbContext readContext = fixture.NewIdentityContext();
        ApplicationUser persisted = await readContext.Users.AsNoTracking().SingleAsync(u => u.Id == userId);

        Assert.Equal("Ana Silva", persisted.DisplayName);
        Assert.Equal("ana", persisted.UserName);
    }
}
