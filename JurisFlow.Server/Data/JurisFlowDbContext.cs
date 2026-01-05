using Microsoft.EntityFrameworkCore;
using JurisFlow.Server.Models;

namespace JurisFlow.Server.Data
{
    public class JurisFlowDbContext : DbContext
    {
        public JurisFlowDbContext(DbContextOptions<JurisFlowDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Client> Clients { get; set; }
        public DbSet<Matter> Matters { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<JurisFlow.Server.Models.Task> Tasks { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<CalendarEvent> CalendarEvents { get; set; }
        public DbSet<TrustTransaction> TrustTransactions { get; set; }
        public DbSet<TrustBankAccount> TrustBankAccounts { get; set; }
        public DbSet<ClientTrustLedger> ClientTrustLedgers { get; set; }
        public DbSet<ReconciliationRecord> ReconciliationRecords { get; set; }
        public DbSet<Lead> Leads { get; set; }
        public DbSet<OpposingParty> OpposingParties { get; set; }
        public DbSet<ConflictCheck> ConflictChecks { get; set; }
        public DbSet<ConflictResult> ConflictResults { get; set; }
        public DbSet<SignatureRequest> SignatureRequests { get; set; }
        public DbSet<PaymentTransaction> PaymentTransactions { get; set; }
        public DbSet<CourtRule> CourtRules { get; set; }
        public DbSet<Deadline> Deadlines { get; set; }
        public DbSet<EmailMessage> EmailMessages { get; set; }
        public DbSet<EmailAccount> EmailAccounts { get; set; }
        public DbSet<SmsMessage> SmsMessages { get; set; }
        public DbSet<SmsTemplate> SmsTemplates { get; set; }
        public DbSet<SmsReminder> SmsReminders { get; set; }
        public DbSet<IntakeForm> IntakeForms { get; set; }
        public DbSet<IntakeSubmission> IntakeSubmissions { get; set; }
        public DbSet<ResearchSession> ResearchSessions { get; set; }
        public DbSet<ContractAnalysis> ContractAnalyses { get; set; }
        public DbSet<CasePrediction> CasePredictions { get; set; }
        public DbSet<StaffMessage> StaffMessages { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<ClientMessage> ClientMessages { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<BillingLock> BillingLocks { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<InvoiceLineItem> InvoiceLineItems { get; set; }
        public DbSet<Holiday> Holidays { get; set; }
        public DbSet<DocumentVersion> DocumentVersions { get; set; }
        public DbSet<AppointmentRequest> AppointmentRequests { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Client>()
                .HasIndex(c => c.Email)
                .IsUnique();

            modelBuilder.Entity<Employee>()
                .HasIndex(e => e.Email)
                .IsUnique();

            modelBuilder.Entity<StaffMessage>()
                .HasIndex(m => new { m.SenderId, m.RecipientId, m.CreatedAt });

            modelBuilder.Entity<Notification>()
                .HasIndex(n => new { n.UserId, n.ClientId, n.CreatedAt });

            modelBuilder.Entity<ClientMessage>()
                .HasIndex(m => new { m.ClientId, m.CreatedAt });

            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => new { a.CreatedAt, a.Action });

            modelBuilder.Entity<BillingLock>()
                .HasIndex(b => new { b.PeriodStart, b.PeriodEnd });

            modelBuilder.Entity<Invoice>()
                .HasIndex(i => i.Number);

            modelBuilder.Entity<InvoiceLineItem>()
                .HasIndex(li => li.InvoiceId);

            modelBuilder.Entity<Holiday>()
                .HasIndex(h => new { h.Date, h.Jurisdiction });

            modelBuilder.Entity<DocumentVersion>()
                .HasIndex(v => v.DocumentId);

            modelBuilder.Entity<AppointmentRequest>()
                .HasIndex(a => new { a.ClientId, a.RequestedDate });
        }
    }
}
