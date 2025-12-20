using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CollabEditor.Infrastructure.Persistence;

public class CollabEditorDbContextFactory : IDesignTimeDbContextFactory<CollabEditorDbContext>
{
    public CollabEditorDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CollabEditorDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=collab_editor;Username=postgres;Password=postgres",
            npgsqlOptions => npgsqlOptions.MigrationsAssembly("CollabEditor.Infrastructure"));

        return new CollabEditorDbContext(optionsBuilder.Options);
    }
}