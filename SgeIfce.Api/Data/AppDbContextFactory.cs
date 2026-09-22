using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SgeIfce.Api.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(
    "Host=localhost;Port=5432;Database=sge_ifce;Username=leonardo;Password=ifce123"
);

        return new AppDbContext(optionsBuilder.Options);
    }
}
