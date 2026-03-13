using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Mockstar.Web.Persistence;

public class HeatDbContextFactory : IDesignTimeDbContextFactory<HeatDbContext>
{
    public HeatDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<HeatDbContext>();
        optionsBuilder.UseSqlite("Data Source=mockstar.db");

        return new HeatDbContext(optionsBuilder.Options);
    }
}
