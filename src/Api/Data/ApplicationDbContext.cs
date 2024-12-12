using Microsoft.EntityFrameworkCore;

namespace HardikDhuri.TaskManager.Api.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Models.Task> Tasks { get; set; } 
}
