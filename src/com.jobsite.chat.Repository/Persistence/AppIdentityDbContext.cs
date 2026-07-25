using com.jobsite.chat.Repository.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace com.jobsite.chat.Repository.Persistence;

public sealed class AppIdentityDbContext(DbContextOptions<AppIdentityDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);   // MUST run first: builds AspNet* tables
        builder.Entity<ApplicationUser>().Property(u => u.DisplayName).HasMaxLength(100).IsRequired();
    }
}
