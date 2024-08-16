using Microsoft.EntityFrameworkCore;
using TodoList.Models;

namespace TodoList_back.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        { }
        public DbSet<TodoItem> TodoItems { get; set; }
    }
}