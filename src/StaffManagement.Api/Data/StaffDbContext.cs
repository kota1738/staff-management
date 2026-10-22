using Microsoft.EntityFrameworkCore;
using StaffManagement.Api.Models;

namespace StaffManagement.Api.Data;

public sealed class StaffDbContext(DbContextOptions<StaffDbContext> options) : DbContext(options)
{
    public DbSet<Staff> Staff => Set<Staff>();

    public DbSet<AppUser> Users => Set<AppUser>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        var staff = model.Entity<Staff>();
        staff.ToTable("Staff");
        staff.HasKey(s => s.Id);
        staff.Property(s => s.FirstName).IsRequired().HasMaxLength(80);
        staff.Property(s => s.LastName).IsRequired().HasMaxLength(80);
        staff.Property(s => s.Email).IsRequired().HasMaxLength(200);
        staff.Property(s => s.JobTitle).IsRequired().HasMaxLength(120);
        staff.Property(s => s.Department).IsRequired().HasMaxLength(80);
        staff.Property(s => s.Salary).HasPrecision(18, 2);
        staff.HasIndex(s => s.Email).IsUnique().HasDatabaseName("UX_Staff_Email");
        staff.HasIndex(s => s.Department);
        staff.HasIndex(s => s.LastName);

        var users = model.Entity<AppUser>();
        users.ToTable("Users");
        users.HasKey(u => u.Id);
        users.Property(u => u.Email).IsRequired().HasMaxLength(200);
        users.Property(u => u.PasswordHash).IsRequired().HasMaxLength(400);
        users.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);
        users.HasIndex(u => u.Email).IsUnique().HasDatabaseName("UX_Users_Email");
    }
}
