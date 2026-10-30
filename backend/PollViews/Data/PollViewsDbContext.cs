using Microsoft.EntityFrameworkCore;
using PollViews.Entities;

namespace PollViews.Data;

public sealed class PollViewsDbContext(DbContextOptions<PollViewsDbContext> options) : DbContext(options)
{
    public DbSet<State> States => Set<State>();
    public DbSet<LocalGovernmentArea> LocalGovernmentAreas => Set<LocalGovernmentArea>();
    public DbSet<Ward> Wards => Set<Ward>();
    public DbSet<PollingUnit> PollingUnits => Set<PollingUnit>();
    public DbSet<SurveyResponse> SurveyResponses => Set<SurveyResponse>();
    public DbSet<SurveyPoliticalPreference> SurveyPoliticalPreferences => Set<SurveyPoliticalPreference>();
    public DbSet<SurveyReason> SurveyReasons => Set<SurveyReason>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<AdminReview> AdminReviews => Set<AdminReview>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<State>().HasIndex(entity => entity.Code).IsUnique();
        modelBuilder.Entity<LocalGovernmentArea>().HasIndex(entity => new { entity.StateId, entity.Code }).IsUnique();
        modelBuilder.Entity<Ward>().HasIndex(entity => new { entity.LocalGovernmentAreaId, entity.Code }).IsUnique();
        modelBuilder.Entity<PollingUnit>().HasIndex(entity => new { entity.WardId, entity.Code }).IsUnique();
        modelBuilder.Entity<PollingUnit>().HasIndex(entity => entity.FullCode).IsUnique();

        modelBuilder.Entity<LocalGovernmentArea>()
            .HasOne(entity => entity.State)
            .WithMany(entity => entity.LocalGovernmentAreas)
            .HasForeignKey(entity => entity.StateId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Ward>()
            .HasOne(entity => entity.LocalGovernmentArea)
            .WithMany(entity => entity.Wards)
            .HasForeignKey(entity => entity.LocalGovernmentAreaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PollingUnit>()
            .HasOne(entity => entity.Ward)
            .WithMany(entity => entity.PollingUnits)
            .HasForeignKey(entity => entity.WardId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SurveyResponse>()
            .HasOne(entity => entity.State)
            .WithMany()
            .HasForeignKey(entity => entity.StateId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SurveyResponse>()
            .HasOne(entity => entity.LocalGovernmentArea)
            .WithMany()
            .HasForeignKey(entity => entity.LocalGovernmentAreaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SurveyResponse>()
            .HasOne(entity => entity.Ward)
            .WithMany()
            .HasForeignKey(entity => entity.WardId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SurveyResponse>()
            .HasOne(entity => entity.PollingUnit)
            .WithMany(entity => entity.SurveyResponses)
            .HasForeignKey(entity => entity.PollingUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SurveyPoliticalPreference>()
            .HasOne(entity => entity.SurveyResponse)
            .WithMany(entity => entity.PoliticalPreferences)
            .HasForeignKey(entity => entity.SurveyResponseId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SurveyReason>()
            .HasOne(entity => entity.SurveyResponse)
            .WithMany(entity => entity.Reasons)
            .HasForeignKey(entity => entity.SurveyResponseId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SurveyReason>()
            .HasOne(entity => entity.SurveyPoliticalPreference)
            .WithMany(entity => entity.Reasons)
            .HasForeignKey(entity => entity.SurveyPoliticalPreferenceId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AdminUser>().HasIndex(entity => entity.Username).IsUnique();

        modelBuilder.Entity<AdminReview>()
            .HasKey(entity => entity.SurveyResponseId);

        modelBuilder.Entity<AdminReview>()
            .HasOne(entity => entity.SurveyResponse)
            .WithOne()
            .HasForeignKey<AdminReview>(entity => entity.SurveyResponseId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AdminReview>()
            .HasOne(entity => entity.ReviewedByAdminUser)
            .WithMany(entity => entity.Reviews)
            .HasForeignKey(entity => entity.ReviewedByAdminUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
