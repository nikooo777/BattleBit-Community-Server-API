using System.Collections.Concurrent;
using BattleBitAPI.Common;
using MySql.Data.MySqlClient;

namespace CommunityServerAPI.Storage;

public class SwissAdminToolsMysql : SwissAdminToolsStore
{
    private const int MaxMessages = 50;
    private const int TimerInterval = 10000; // 10 seconds
    private static readonly ConcurrentQueue<(string Message, int PlayerId)> MessageQueue = new();
    private static Timer _timer;
    private readonly MySqlConnection mConnection;

    public SwissAdminToolsMysql(string connectionString)
    {
        mConnection = new MySqlConnection(connectionString);
        mConnection.Open();
        _timer = new Timer(FlushToDatabase, null, TimerInterval, TimerInterval);
    }

    public void StoreProgression(ulong steamId, PlayerStats stats)
    {
        throw new NotImplementedException();
    }

    public void StoreChatLog(ulong steamId, string message)
    {
        var playerId = GetPlayer(steamId);
        if (!playerId.HasValue) return;
        MessageQueue.Enqueue((message, playerId.Value));
        if (MessageQueue.Count >= MaxMessages)
            Task.Run(() => { FlushToDatabase(null); });
    }

    public List<ChatLog> GetChatLogs(ulong steamId, DateTime startDate, DateTime endDate)
    {
        throw new NotImplementedException();
    }

    public void AddAdmin(AdminData admin)
    {
        throw new NotImplementedException();
    }

    public void RemoveAdmin(int adminId)
    {
        throw new NotImplementedException();
    }

    public AdminData GetAdmin(int adminId)
    {
        throw new NotImplementedException();
    }

    public List<AdminData> GetAllAdmins()
    {
        throw new NotImplementedException();
    }

    public void AddBlock(BlockData block)
    {
        throw new NotImplementedException();
    }

    public void RemoveBlock(int blockId)
    {
        throw new NotImplementedException();
    }

    public BlockData GetBlock(int blockId)
    {
        throw new NotImplementedException();
    }

    public List<BlockData> GetAllBlocksForPlayer(ulong steamId)
    {
        throw new NotImplementedException();
    }

    public void ReportPlayer(ReportData report)
    {
        throw new NotImplementedException();
    }

    public void UpdateReportStatus(int reportId, ReportStatus status, string adminNotes)
    {
        throw new NotImplementedException();
    }

    public List<ReportData> GetReportsForPlayer(ulong steamId)
    {
        throw new NotImplementedException();
    }

    public List<ReportData> GetAllPendingReports()
    {
        throw new NotImplementedException();
    }

    public void StorePlayer(ulong steamId, PlayerJoiningArguments args)
    {
        var command = new MySqlCommand(
            "INSERT INTO player (steam_id, is_banned, roles, achievements, selections, tool_progress, created_at, updated_at) " +
            "VALUES (@steamId, @isBanned, @roles, @achievements, @selections, @toolProgress, NOW(), NOW()) " +
            "ON DUPLICATE KEY UPDATE " +
            "is_banned = @isBannedUpdate, " +
            "roles = @rolesUpdate, " +
            "achievements = @achievementsUpdate, " +
            "selections = @selectionsUpdate, " +
            "tool_progress = @toolProgressUpdate, " +
            "updated_at = NOW()",
            mConnection);

        command.Parameters.AddWithValue("@steamId", steamId);
        command.Parameters.AddWithValue("@isBanned", args.Stats.IsBanned);
        command.Parameters.AddWithValue("@roles", args.Stats.Roles);
        command.Parameters.AddWithValue("@achievements", args.Stats.Achievements);
        command.Parameters.AddWithValue("@selections", args.Stats.Selections);
        command.Parameters.AddWithValue("@toolProgress", args.Stats.ToolProgress);

        // Duplicate parameters for the UPDATE section
        command.Parameters.AddWithValue("@isBannedUpdate", args.Stats.IsBanned);
        command.Parameters.AddWithValue("@rolesUpdate", args.Stats.Roles);
        command.Parameters.AddWithValue("@achievementsUpdate", args.Stats.Achievements);
        command.Parameters.AddWithValue("@selectionsUpdate", args.Stats.Selections);
        command.Parameters.AddWithValue("@toolProgressUpdate", args.Stats.ToolProgress);

        command.ExecuteNonQuery();
    }


    public int? GetPlayer(ulong steamId)
    {
        var command = new MySqlCommand("SELECT * FROM player WHERE steam_id = @steamId", mConnection);
        command.Parameters.AddWithValue("@steamId", steamId);

        var result = command.ExecuteScalar();
        if (result != null)
            return Convert.ToInt32(result);
        return null;
    }

    private void FlushToDatabase(object state)
    {
        if (!MessageQueue.Any())
            return;

        using var transaction = mConnection.BeginTransaction();
        try
        {
            var command =
                new MySqlCommand(
                    "INSERT INTO chat_logs (message, player_id, timestamp) VALUES (@message, @playerId, @timestamp)",
                    mConnection, transaction);
            command.Parameters.Add("@message", MySqlDbType.Text);
            command.Parameters.Add("@playerId", MySqlDbType.Int32);
            command.Parameters.Add("@timestamp", MySqlDbType.DateTime);

            while (MessageQueue.TryDequeue(out var log))
            {
                command.Parameters["@message"].Value = log.Message;
                command.Parameters["@playerId"].Value = log.PlayerId;
                command.Parameters["@timestamp"].Value = DateTime.Now;

                command.ExecuteNonQuery();
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public void Close()
    {
        _timer.Dispose();
        mConnection.Close();
    }
}