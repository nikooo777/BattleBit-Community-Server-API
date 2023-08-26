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

    public virtual DbSet<Admin> Admins { get; set; }

    public virtual DbSet<Block> Blocks { get; set; }

    public virtual DbSet<ChatLog> ChatLogs { get; set; }

    public virtual DbSet<Player> Players { get; set; }

    public virtual DbSet<PlayerProgress> PlayerProgresses { get; set; }

    public virtual DbSet<PlayerReport> PlayerReports { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see http://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseMySql("server=localhost;database=battlebit;user id=battlebit;password=battlebit", Microsoft.EntityFrameworkCore.ServerVersion.Parse("8.0.33-mysql"));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_unicode_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<Admin>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("admins");

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

            entity.ToTable("blocks");

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

            entity.ToTable("chat_logs");

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

        modelBuilder.Entity<Player>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("player");

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

            entity.ToTable("player_progress");

            entity.HasIndex(e => e.PlayerId, "player_progress_player_uniq").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AssaultKills)
                .HasDefaultValueSql("'0'")
                .HasColumnName("assault_kills");
            entity.Property(e => e.AssaultPlayTime)
                .HasDefaultValueSql("'0'")
                .HasColumnName("assault_play_time");
            entity.Property(e => e.AssaultScore)
                .HasDefaultValueSql("'0'")
                .HasColumnName("assault_score");
            entity.Property(e => e.Assists)
                .HasDefaultValueSql("'0'")
                .HasColumnName("assists");
            entity.Property(e => e.CompletedObjectives)
                .HasDefaultValueSql("'0'")
                .HasColumnName("completed_objectives");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("created_at");
            entity.Property(e => e.CurrentRank)
                .HasDefaultValueSql("'0'")
                .HasColumnName("current_rank");
            entity.Property(e => e.DeathCount)
                .HasDefaultValueSql("'0'")
                .HasColumnName("death_count");
            entity.Property(e => e.EngineerKills)
                .HasDefaultValueSql("'0'")
                .HasColumnName("engineer_kills");
            entity.Property(e => e.EngineerPlayTime)
                .HasDefaultValueSql("'0'")
                .HasColumnName("engineer_play_time");
            entity.Property(e => e.EngineerScore)
                .HasDefaultValueSql("'0'")
                .HasColumnName("engineer_score");
            entity.Property(e => e.Exp)
                .HasDefaultValueSql("'0'")
                .HasColumnName("exp");
            entity.Property(e => e.FriendlyKills)
                .HasDefaultValueSql("'0'")
                .HasColumnName("friendly_kills");
            entity.Property(e => e.FriendlyShots)
                .HasDefaultValueSql("'0'")
                .HasColumnName("friendly_shots");
            entity.Property(e => e.Headshots)
                .HasDefaultValueSql("'0'")
                .HasColumnName("headshots");
            entity.Property(e => e.HealedHps)
                .HasDefaultValueSql("'0'")
                .HasColumnName("healed_hps");
            entity.Property(e => e.KillCount)
                .HasDefaultValueSql("'0'")
                .HasColumnName("kill_count");
            entity.Property(e => e.LeaderKills)
                .HasDefaultValueSql("'0'")
                .HasColumnName("leader_kills");
            entity.Property(e => e.LeaderPlayTime)
                .HasDefaultValueSql("'0'")
                .HasColumnName("leader_play_time");
            entity.Property(e => e.LeaderScore)
                .HasDefaultValueSql("'0'")
                .HasColumnName("leader_score");
            entity.Property(e => e.LongestKill)
                .HasDefaultValueSql("'0'")
                .HasColumnName("longest_kill");
            entity.Property(e => e.LoseCount)
                .HasDefaultValueSql("'0'")
                .HasColumnName("lose_count");
            entity.Property(e => e.MedicKills)
                .HasDefaultValueSql("'0'")
                .HasColumnName("medic_kills");
            entity.Property(e => e.MedicPlayTime)
                .HasDefaultValueSql("'0'")
                .HasColumnName("medic_play_time");
            entity.Property(e => e.MedicScore)
                .HasDefaultValueSql("'0'")
                .HasColumnName("medic_score");
            entity.Property(e => e.PlayTimeSeconds)
                .HasDefaultValueSql("'0'")
                .HasColumnName("play_time_seconds");
            entity.Property(e => e.PlayerId).HasColumnName("player_id");
            entity.Property(e => e.Prestige)
                .HasDefaultValueSql("'0'")
                .HasColumnName("prestige");
            entity.Property(e => e.ReconKills)
                .HasDefaultValueSql("'0'")
                .HasColumnName("recon_kills");
            entity.Property(e => e.ReconPlayTime)
                .HasDefaultValueSql("'0'")
                .HasColumnName("recon_play_time");
            entity.Property(e => e.ReconScore)
                .HasDefaultValueSql("'0'")
                .HasColumnName("recon_score");
            entity.Property(e => e.Revived)
                .HasDefaultValueSql("'0'")
                .HasColumnName("revived");
            entity.Property(e => e.RevivedTeamMates)
                .HasDefaultValueSql("'0'")
                .HasColumnName("revived_team_mates");
            entity.Property(e => e.RoadKills)
                .HasDefaultValueSql("'0'")
                .HasColumnName("road_kills");
            entity.Property(e => e.ShotsFired)
                .HasDefaultValueSql("'0'")
                .HasColumnName("shots_fired");
            entity.Property(e => e.ShotsHit)
                .HasDefaultValueSql("'0'")
                .HasColumnName("shots_hit");
            entity.Property(e => e.Suicides)
                .HasDefaultValueSql("'0'")
                .HasColumnName("suicides");
            entity.Property(e => e.SupportKills)
                .HasDefaultValueSql("'0'")
                .HasColumnName("support_kills");
            entity.Property(e => e.SupportPlayTime)
                .HasDefaultValueSql("'0'")
                .HasColumnName("support_play_time");
            entity.Property(e => e.SupportScore)
                .HasDefaultValueSql("'0'")
                .HasColumnName("support_score");
            entity.Property(e => e.TotalScore)
                .HasDefaultValueSql("'0'")
                .HasColumnName("total_score");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("updated_at");
            entity.Property(e => e.VehicleHpRepaired)
                .HasDefaultValueSql("'0'")
                .HasColumnName("vehicle_hp_repaired");
            entity.Property(e => e.VehiclesDestroyed)
                .HasDefaultValueSql("'0'")
                .HasColumnName("vehicles_destroyed");
            entity.Property(e => e.WinCount)
                .HasDefaultValueSql("'0'")
                .HasColumnName("win_count");

            entity.HasOne(d => d.Player).WithOne(p => p.PlayerProgress)
                .HasForeignKey<PlayerProgress>(d => d.PlayerId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("player_progress_ibfk_1");
        });

        modelBuilder.Entity<PlayerReport>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("player_reports");

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

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
