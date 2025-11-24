using Financial_management_backend.Models;
using Financial_management_backend.Models.ItemManagement;
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

        // New database sets for fee history and obligations
        public DbSet<FeeStructureHistory> FeeStructureHistories { get; set; }
        public DbSet<StudentFeeObligation> StudentFeeObligations { get; set; }

        // Additional DbSets
        public DbSet<RequirementList> RequirementLists { get; set; }
        public DbSet<RequirementItem> RequirementItems { get; set; }
        public DbSet<StudentRequirement> StudentRequirements { get; set; }
        public DbSet<ItemTransaction> ItemTransactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //Configuring of relationships

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

            // New Fee Structure History relationships
            modelBuilder.Entity<FeeStructureHistory>()
                .HasOne(fsh => fsh.Grade)
                .WithMany()
                .HasForeignKey(fsh => fsh.GradeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Student Fee Obligation relationships
            modelBuilder.Entity<StudentFeeObligation>()
                .HasOne(sfo => sfo.Student)
                .WithMany()
                .HasForeignKey(sfo => sfo.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

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

            // Item Ledger relationships
            modelBuilder.Entity<RequirementList>()
                .HasOne(rl => rl.Creator)
                .WithMany()
                .HasForeignKey(rl => rl.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RequirementItem>()
                .HasOne(ri => ri.RequirementList)
                .WithMany(rl => rl.Items)
                .HasForeignKey(ri => ri.RequirementListId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StudentRequirement>()
                .HasOne(sr => sr.Student)
                .WithMany()
                .HasForeignKey(sr => sr.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StudentRequirement>()
                .HasOne(sr => sr.RequirementList)
                .WithMany()
                .HasForeignKey(sr => sr.RequirementListId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StudentRequirement>()
                .HasOne(sr => sr.Assigner)
                .WithMany()
                .HasForeignKey(sr => sr.AssignedBy)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ItemTransaction>()
                .HasOne(it => it.StudentRequirement)
                .WithMany(sr => sr.Transactions)
                .HasForeignKey(it => it.StudentRequirementId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ItemTransaction>()
                .HasOne(it => it.RequirementItem)
                .WithMany()
                .HasForeignKey(it => it.RequirementItemId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ItemTransaction>()
                .HasOne(it => it.Recorder)
                .WithMany()
                .HasForeignKey(it => it.RecordedBy)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
