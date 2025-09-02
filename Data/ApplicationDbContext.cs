using Financial_management_backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Financial_management_backend.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Grade> Grades { get; set; }
        public DbSet<Parent> Parents { get; set; }
        public DbSet<FeeStructure> FeeStructures { get; set; }
        public DbSet<OtherFee> OtherFees { get; set; }
        public DbSet<Payment> Payments { get; set; }

        public DbSet<FeePayment> FeePayments { get; set; }
        public DbSet<CustomFee> CustomFees { get; set; }

        public DbSet<FinancialTransaction> FinancialTransactions { get; set; }

        public DbSet<ExpenseCategory> ExpenseCategories { get; set; }

        public DbSet<Expense> Expenses { get; set; }

        public DbSet<RequiredItem> RequiredItems { get; set; }
        public DbSet<ItemReceived> ItemsReceived { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //Configuring of relationship

            modelBuilder.Entity<Student>()
                .HasOne(s => s.Grade)
                .WithMany(g => g.Students)
                .HasForeignKey(s => s.GradeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Student>()
                .HasOne(s => s.Parent)
                .WithMany()
                .HasForeignKey(s =>s.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FeeStructure>()
                .HasOne(fs => fs.Grade)
                .WithMany()
                .HasForeignKey(fs => fs.GradeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OtherFee>()
                .HasOne(of => of.Grade)
                .WithMany()
                .HasForeignKey(of => of.GradeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Student)
                .WithMany()
                .HasForeignKey(p => p.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FeePayment>()
                .HasOne(fp => fp.Payment)
                .WithMany(p => p.FeePayments) 
                .HasForeignKey(fp => fp.PaymentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FinancialTransaction>()
                .HasOne(ft => ft.Payment)
                .WithMany()
                .HasForeignKey(ft => ft.PaymentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FinancialTransaction>()
                .HasOne(ft => ft.Expense)
                .WithMany()
                .HasForeignKey(ft => ft.ExpenseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Expense>()
                .HasOne(fp => fp.Category)
                .WithMany()
                .HasForeignKey(fp => fp.ExpenseCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CustomFee>()
                .HasOne(cf => cf.Student)
                .WithMany()
                .HasForeignKey(cf => cf.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            // ItemReceived relationships
            modelBuilder.Entity<ItemReceived>()
                .HasOne(ir => ir.RequiredItem)
                .WithMany()
                .HasForeignKey(ir => ir.RequiredItemId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ItemReceived>()
                .HasOne(ir => ir.Student)
                .WithMany()
                .HasForeignKey(ir => ir.StudentId)
                .OnDelete(DeleteBehavior.Restrict);
        }

    }
}
