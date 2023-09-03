using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace SAT.Models;

public partial class BattlebitContext : DbContext
{
    public BattlebitContext()
    {
    }

    public BattlebitContext(DbContextOptions<BattlebitContext> options)
        : base(options)
    {
    }

    public virtual DbSet<RankResponse> RankResponses { get; set; }
    public virtual DbSet<Admin> Admins { get; set; }

    public virtual DbSet<Block> Blocks { get; set; }

    public virtual DbSet<ChatLog> ChatLogs { get; set; }

    public virtual DbSet<GorpMigration> GorpMigrations { get; set; }

    public virtual DbSet<Player> Players { get; set; }

    public virtual DbSet<PlayerProgress> PlayerProgresses { get; set; }

    public virtual DbSet<PlayerReport> PlayerReports { get; set; }

    public virtual DbSet<Stat> Stats { get; set; }

    public virtual DbSet<Suggestion> Suggestions { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see http://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseMySql("server=localhost;database=battlebit;user id=battlebit;password=battlebit", Microsoft.EntityFrameworkCore.ServerVersion.Parse("8.0.33-mysql"));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb3_unicode_ci")
            .HasCharSet("utf8mb3");

        modelBuilder.Entity<Admin>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("admins")
                .HasCharSet("utf8mb4")
                .UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.SteamId, "steam_id").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Flags)
                .HasMaxLength(255)
                .HasColumnName("flags");
            entity.Property(e => e.Immunity).HasColumnName("immunity");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.SteamId).HasColumnName("steam_id");
        });

        modelBuilder.Entity<Block>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("blocks")
                .HasCharSet("utf8mb4")
                .UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.IssuerAdminId, "issuer_admin_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AdminIp)
                .HasMaxLength(45)
                .HasColumnName("admin_ip");
            entity.Property(e => e.BlockType)
                .HasColumnType("enum('BAN','GAG','MUTE')")
                .HasColumnName("block_type");
            entity.Property(e => e.ExpiryDate)
                .HasColumnType("datetime")
                .HasColumnName("expiry_date");
            entity.Property(e => e.IssuerAdminId).HasColumnName("issuer_admin_id");
            entity.Property(e => e.Reason)
                .HasMaxLength(255)
                .HasColumnName("reason");
            entity.Property(e => e.SteamId).HasColumnName("steam_id");
            entity.Property(e => e.TargetIp)
                .HasMaxLength(45)
                .HasColumnName("target_ip");

            entity.HasOne(d => d.IssuerAdmin).WithMany(p => p.Blocks)
                .HasForeignKey(d => d.IssuerAdminId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("blocks_ibfk_1");
        });

        modelBuilder.Entity<ChatLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("chat_logs")
                .HasCharSet("utf8mb4")
                .UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.PlayerId, "player_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Message)
                .HasColumnType("text")
                .HasColumnName("message");
            entity.Property(e => e.PlayerId).HasColumnName("player_id");
            entity.Property(e => e.Timestamp)
                .HasColumnType("datetime")
                .HasColumnName("timestamp");

            entity.HasOne(d => d.Player).WithMany(p => p.ChatLogs)
                .HasForeignKey(d => d.PlayerId)
                .HasConstraintName("chat_logs_ibfk_1");
        });

        modelBuilder.Entity<GorpMigration>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("gorp_migrations")
                .UseCollation("utf8mb3_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AppliedAt)
                .HasColumnType("datetime")
                .HasColumnName("applied_at");
        });

        modelBuilder.Entity<Player>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("player")
                .HasCharSet("utf8mb4")
                .UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.SteamId, "steam_id").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Achievements)
                .HasColumnType("blob")
                .HasColumnName("achievements");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("created_at");
            entity.Property(e => e.IsBanned).HasColumnName("is_banned");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.Roles).HasColumnName("roles");
            entity.Property(e => e.Selections)
                .HasColumnType("blob")
                .HasColumnName("selections");
            entity.Property(e => e.SteamId).HasColumnName("steam_id");
            entity.Property(e => e.ToolProgress)
                .HasColumnType("blob")
                .HasColumnName("tool_progress");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<PlayerProgress>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("player_progress")
                .HasCharSet("utf8mb4")
                .UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => new { e.PlayerId, e.IsOfficial }, "player_progress_player_uniq").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AssaultKills).HasColumnName("assault_kills");
            entity.Property(e => e.AssaultPlayTime).HasColumnName("assault_play_time");
            entity.Property(e => e.AssaultScore).HasColumnName("assault_score");
            entity.Property(e => e.Assists).HasColumnName("assists");
            entity.Property(e => e.CompletedObjectives).HasColumnName("completed_objectives");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("created_at");
            entity.Property(e => e.CurrentRank).HasColumnName("current_rank");
            entity.Property(e => e.DeathCount).HasColumnName("death_count");
            entity.Property(e => e.EngineerKills).HasColumnName("engineer_kills");
            entity.Property(e => e.EngineerPlayTime).HasColumnName("engineer_play_time");
            entity.Property(e => e.EngineerScore).HasColumnName("engineer_score");
            entity.Property(e => e.Exp).HasColumnName("exp");
            entity.Property(e => e.FriendlyKills).HasColumnName("friendly_kills");
            entity.Property(e => e.FriendlyShots).HasColumnName("friendly_shots");
            entity.Property(e => e.Headshots).HasColumnName("headshots");
            entity.Property(e => e.HealedHps).HasColumnName("healed_hps");
            entity.Property(e => e.IsOfficial)
                .HasComment("when 1 it means the stats are from the official game, when 0 it's from our own servers")
                .HasColumnName("is_official");
            entity.Property(e => e.KillCount).HasColumnName("kill_count");
            entity.Property(e => e.LeaderKills).HasColumnName("leader_kills");
            entity.Property(e => e.LeaderPlayTime).HasColumnName("leader_play_time");
            entity.Property(e => e.LeaderScore).HasColumnName("leader_score");
            entity.Property(e => e.LongestKill).HasColumnName("longest_kill");
            entity.Property(e => e.LoseCount).HasColumnName("lose_count");
            entity.Property(e => e.MedicKills).HasColumnName("medic_kills");
            entity.Property(e => e.MedicPlayTime).HasColumnName("medic_play_time");
            entity.Property(e => e.MedicScore).HasColumnName("medic_score");
            entity.Property(e => e.PlayTimeSeconds).HasColumnName("play_time_seconds");
            entity.Property(e => e.PlayerId).HasColumnName("player_id");
            entity.Property(e => e.Prestige).HasColumnName("prestige");
            entity.Property(e => e.ReconKills).HasColumnName("recon_kills");
            entity.Property(e => e.ReconPlayTime).HasColumnName("recon_play_time");
            entity.Property(e => e.ReconScore).HasColumnName("recon_score");
            entity.Property(e => e.Revived).HasColumnName("revived");
            entity.Property(e => e.RevivedTeamMates).HasColumnName("revived_team_mates");
            entity.Property(e => e.RoadKills).HasColumnName("road_kills");
            entity.Property(e => e.ShotsFired).HasColumnName("shots_fired");
            entity.Property(e => e.ShotsHit).HasColumnName("shots_hit");
            entity.Property(e => e.Suicides).HasColumnName("suicides");
            entity.Property(e => e.SupportKills).HasColumnName("support_kills");
            entity.Property(e => e.SupportPlayTime).HasColumnName("support_play_time");
            entity.Property(e => e.SupportScore).HasColumnName("support_score");
            entity.Property(e => e.TotalScore).HasColumnName("total_score");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("updated_at");
            entity.Property(e => e.VehicleHpRepaired).HasColumnName("vehicle_hp_repaired");
            entity.Property(e => e.VehiclesDestroyed).HasColumnName("vehicles_destroyed");
            entity.Property(e => e.WinCount).HasColumnName("win_count");

            entity.HasOne(d => d.Player).WithMany(p => p.PlayerProgresses)
                .HasForeignKey(d => d.PlayerId)
                .HasConstraintName("player_progress_ibfk_1");
        });

        modelBuilder.Entity<PlayerReport>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("player_reports")
                .HasCharSet("utf8mb4")
                .UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.ReporterId, "player_reports_ibfk_1");

            entity.HasIndex(e => e.ReportedPlayerId, "player_reports_ibfk_2");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AdminNotes)
                .HasColumnType("text")
                .HasColumnName("admin_notes");
            entity.Property(e => e.Reason)
                .HasColumnType("text")
                .HasColumnName("reason");
            entity.Property(e => e.ReportedPlayerId).HasColumnName("reported_player_id");
            entity.Property(e => e.ReporterId).HasColumnName("reporter_id");
            entity.Property(e => e.Status)
                .HasColumnType("enum('Pending','Reviewed','Resolved','Dismissed')")
                .HasColumnName("status");
            entity.Property(e => e.Timestamp)
                .HasColumnType("datetime")
                .HasColumnName("timestamp");

            entity.HasOne(d => d.ReportedPlayer).WithMany(p => p.PlayerReportReportedPlayers)
                .HasForeignKey(d => d.ReportedPlayerId)
                .HasConstraintName("player_reports_ibfk_2");

            entity.HasOne(d => d.Reporter).WithMany(p => p.PlayerReportReporters)
                .HasForeignKey(d => d.ReporterId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("player_reports_ibfk_1");
        });

        modelBuilder.Entity<Stat>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("stats")
                .HasCharSet("utf8mb4")
                .UseCollation("utf8mb4_unicode_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("created_at");
            entity.Property(e => e.PlayerCount).HasColumnName("player_count");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<Suggestion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("suggestions")
                .HasCharSet("utf8mb4")
                .UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.PlayerId, "player_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("created_at");
            entity.Property(e => e.Feedback)
                .HasColumnType("text")
                .HasColumnName("feedback");
            entity.Property(e => e.PlayerId).HasColumnName("player_id");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Player).WithMany(p => p.Suggestions)
                .HasForeignKey(d => d.PlayerId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("suggestions_ibfk_1");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
