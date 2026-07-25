using com.jobsite.chat.Repository.Identity;
using com.jobsite.chat.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace com.jobsite.chat.Tests.Repository;

// Spec §4 behavior 22: AppIdentityDbContext round-trips ApplicationUser.DisplayName on its OWN
// connection (per the EnsureCreated multi-context pitfall). Exercises the identity context/config
// directly — expected to PASS from the compile-enablement skeletons.
public sealed class AppIdentityDbContextTests
{
    // Behavior 22: an ApplicationUser with DisplayName round-trips.
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
