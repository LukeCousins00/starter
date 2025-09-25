using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Starter.Infrastructure.Data;

public class StarterContext(DbContextOptions<StarterContext> options) : DbContext(options)
{

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
