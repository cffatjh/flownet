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
        public DbSet<TimeEntry> TimeEntries { get; set; }
        public DbSet<Expense> Expenses { get; set; }
        public DbSet<OpposingParty> OpposingParties { get; set; }
        public DbSet<ConflictCheck> ConflictChecks { get; set; }
        public DbSet<ConflictResult> ConflictResults { get; set; }
        public DbSet<SignatureRequest> SignatureRequests { get; set; }
        public DbSet<PaymentTransaction> PaymentTransactions { get; set; }
        public DbSet<PaymentPlan> PaymentPlans { get; set; }
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
        public DbSet<AuthSession> AuthSessions { get; set; }
        public DbSet<MfaChallenge> MfaChallenges { get; set; }
        public DbSet<RetentionPolicy> RetentionPolicies { get; set; }
        public DbSet<BillingLock> BillingLocks { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<InvoiceLineItem> InvoiceLineItems { get; set; }
        public DbSet<Holiday> Holidays { get; set; }
        public DbSet<DocumentVersion> DocumentVersions { get; set; }
        public DbSet<AppointmentRequest> AppointmentRequests { get; set; }
        public DbSet<BillingSettings> BillingSettings { get; set; }
        public DbSet<FirmSettings> FirmSettings { get; set; }
        public DbSet<ClientStatusHistory> ClientStatusHistories { get; set; }
        public DbSet<FirmEntity> FirmEntities { get; set; }
        public DbSet<Office> Offices { get; set; }
        public DbSet<DocumentContentIndex> DocumentContentIndexes { get; set; }
        public DbSet<DocumentContentToken> DocumentContentTokens { get; set; }

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

            modelBuilder.Entity<AuthSession>()
                .HasIndex(s => new { s.UserId, s.ClientId, s.ExpiresAt });

            modelBuilder.Entity<MfaChallenge>()
                .HasIndex(c => new { c.UserId, c.ExpiresAt });

            modelBuilder.Entity<RetentionPolicy>()
                .HasIndex(r => r.EntityName)
                .IsUnique();

            modelBuilder.Entity<BillingLock>()
                .HasIndex(b => new { b.PeriodStart, b.PeriodEnd });

            modelBuilder.Entity<Invoice>()
                .HasIndex(i => i.Number);

            modelBuilder.Entity<InvoiceLineItem>()
                .HasIndex(li => li.InvoiceId);

            modelBuilder.Entity<TimeEntry>()
                .HasIndex(t => new { t.MatterId, t.Date });

            modelBuilder.Entity<Expense>()
                .HasIndex(e => new { e.MatterId, e.Date });

            modelBuilder.Entity<Holiday>()
                .HasIndex(h => new { h.Date, h.Jurisdiction });

            modelBuilder.Entity<DocumentVersion>()
                .HasIndex(v => v.DocumentId);

            modelBuilder.Entity<AppointmentRequest>()
                .HasIndex(a => new { a.ClientId, a.RequestedDate });

            modelBuilder.Entity<BillingSettings>()
                .HasIndex(s => s.Id)
                .IsUnique();

            modelBuilder.Entity<FirmSettings>()
                .HasIndex(s => s.Id)
                .IsUnique();

            modelBuilder.Entity<ClientStatusHistory>()
                .HasIndex(h => new { h.ClientId, h.CreatedAt });

            modelBuilder.Entity<Matter>()
                .HasIndex(m => new { m.EntityId, m.OfficeId });

            modelBuilder.Entity<Invoice>()
                .HasIndex(i => new { i.EntityId, i.OfficeId });

            modelBuilder.Entity<Employee>()
                .HasIndex(e => new { e.EntityId, e.OfficeId });

            modelBuilder.Entity<TrustBankAccount>()
                .HasIndex(a => new { a.EntityId, a.OfficeId });

            modelBuilder.Entity<ClientTrustLedger>()
                .HasIndex(l => new { l.EntityId, l.OfficeId });

            modelBuilder.Entity<TrustTransaction>()
                .HasIndex(t => new { t.EntityId, t.OfficeId });

            modelBuilder.Entity<PaymentTransaction>()
                .HasIndex(p => p.PaymentPlanId);

            modelBuilder.Entity<PaymentPlan>()
                .HasIndex(p => new { p.ClientId, p.Status });

            modelBuilder.Entity<PaymentPlan>()
                .HasIndex(p => p.NextRunDate);

            modelBuilder.Entity<FirmEntity>()
                .HasIndex(e => e.Name);

            modelBuilder.Entity<FirmEntity>()
                .HasIndex(e => e.IsDefault);

            modelBuilder.Entity<Office>()
                .HasIndex(o => new { o.EntityId, o.Name });

            modelBuilder.Entity<DocumentContentIndex>()
                .HasIndex(i => i.ContentHash);

            modelBuilder.Entity<DocumentContentToken>()
                .HasKey(t => new { t.DocumentId, t.Token });

            modelBuilder.Entity<DocumentContentToken>()
                .HasIndex(t => t.Token);

            modelBuilder.Entity<Office>()
                .HasOne(o => o.Entity)
                .WithMany(e => e.Offices)
                .HasForeignKey(o => o.EntityId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
