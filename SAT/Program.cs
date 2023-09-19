using System.Numerics;
using System.Text;
using BattleBitAPI;
using BattleBitAPI.Common;
using BattleBitAPI.Server;
using CommunityServerAPI;
using SAT.configs;
using SAT.Db;
using SAT.Models;
using SAT.rank;
using SAT.RoundManager;
using SAT.Statistics;
using SAT.SwissAdminTools;
using SAT.TeamBalancer;
using SAT.Utils;
using Admin = SAT.SwissAdminTools.Admin;
using Restrictions = SAT.SwissAdminTools.Restrictions;

namespace SwissAdminTools;

internal class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine(ConfigurationManager.Config.join_text);
        const int port = 1337;
        DbContextPool.Initialize(16);
        var listener = new ServerListener<MyPlayer, MyGameServer>();
        listener.OnGameServerConnected += async server => { Console.WriteLine($"[{DateTime.UtcNow}] Gameserver connected! {server.GameIP}:{server.GamePort} {server.ServerName}"); };
        listener.Start(port);
        Console.WriteLine($"[{DateTime.UtcNow}] APIs Server started on port {port}");
        Thread.Sleep(-1);
    }
}

public class MyGameServer : GameServer<MyPlayer>
{
    public bool AllowReviving;

    public static BattlebitContext Db => DbContextPool.GetContext();

    public bool CanSpawnOnPlayers { get; set; }
    public bool TextWallHack { get; set; }
    public bool HalveSpawnTime { get; set; }

    public float RespawnTime()
    {
        return HalveSpawnTime ? 2.5f : 5f;
    }

    public override async Task OnConnected()
    {
        var ads = new Advertisements(this);
        Task.Run(ads.Spam);
        Task.Run(() => Statistics.TrackPlayerCount(this));
        if (RoundSettings.State == GameState.WaitingForPlayers)
        {
            RoundSettings.PlayersToStart = 0;
            RoundSettings.SecondsLeft = ConfigurationManager.Config.max_time;
            RoundSettings.TeamATickets = ConfigurationManager.Config.max_tickets;
            RoundSettings.TeamBTickets = ConfigurationManager.Config.max_tickets;
            RoundSettings.MaxTickets = ConfigurationManager.Config.max_tickets;
            ForceStartGame();
        }

        ExecuteCommand("setspeedhackdetection false"); //temp fix for false positives
        GamemodeRotation.SetRotation(ConfigurationManager.Config.rotations.gamemodes.ToArray());
        SetRulesScreenText("This is a test");
        Formatting.SafeSetLoadingScreenText(ConfigurationManager.Config.join_text + "\n" + Stats.TopN(3), this);
        try
        {
            foreach (var rt in ConfigurationManager.Config.restrictions.weapon_types)
            {
                Console.WriteLine($"[{DateTime.UtcNow}] Adding restriction for " + rt + "");
                var exists = Enum.TryParse(rt, true, out WeaponType wepType);
                if (exists)
                    Restrictions.AddCategoryRestriction(wepType);
                else
                    Console.WriteLine($"[{DateTime.UtcNow}] Could not find weapon type " + rt + "");
            }

            foreach (var rt in ConfigurationManager.Config.restrictions.weapons)
            {
                Console.WriteLine($"[{DateTime.UtcNow}] Adding restriction for " + rt + "");
                var exists = Weapons.TryFind(rt, out var wep);
                if (exists)
                    Restrictions.AddWeaponRestriction(wep);
                else
                    Console.WriteLine($"[{DateTime.UtcNow}] Could not find weapon " + rt + "");
            }

            foreach (var g in ConfigurationManager.Config.restrictions.gadgets)
            {
                Console.WriteLine($"[{DateTime.UtcNow}] Adding restriction for " + g + "");
                var exists = Gadgets.TryFind(g, out var gad);
                if (exists)
                    Restrictions.AddGadgetRestriction(gad);
                else
                    Console.WriteLine($"[{DateTime.UtcNow}] Could not find gadget " + g + "");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        ServerSettings.CanVoteNight = false;

        MapRotation.SetRotation(ConfigurationManager.Config.rotations.maps.ToArray());
        Settings.SettingsBalancer(this);
    }


    public override Task OnGameStateChanged(GameState oldState, GameState newState)
    {
        switch (newState)
        {
            case GameState.WaitingForPlayers:
                RoundSettings.PlayersToStart = 0;
                ForceStartGame();
                break;
            case GameState.Playing:
                RoundSettings.SecondsLeft = ConfigurationManager.Config.max_time;
                RoundSettings.TeamATickets = ConfigurationManager.Config.max_tickets;
                RoundSettings.TeamBTickets = ConfigurationManager.Config.max_tickets;
                RoundSettings.MaxTickets = ConfigurationManager.Config.max_tickets;
                break;
            case GameState.EndingGame:
                RoundSettings.SecondsLeft = 5;
                Cache.FlagMapChange();
                break;
        }

        return Task.CompletedTask;
    }

    public override async Task OnSavePlayerStats(ulong steamId, PlayerStats stats)
    {
        var previousStats = Cache.Get(steamId);
        if (previousStats == null)
        {
            Console.WriteLine("player has no cached stats, so no delta possible!");
            return;
        }

        //calculate delta and store to database
        var db = Db;
        try
        {
            var dbPlayer = db.Players.FirstOrDefault(player => player.SteamId == (long)steamId);
            if (dbPlayer == null)
            {
                Console.WriteLine("player not found in db when leaving");
                return;
            }

            var delta = Utils.Delta(stats.Progress, previousStats);

            var dbProgress = db.PlayerProgresses.FirstOrDefault(playerProgress => playerProgress.PlayerId == dbPlayer.Id && playerProgress.IsOfficial == 0);
            if (dbProgress != null)
            {
                dbProgress = Utils.AddProgress(delta, dbProgress);
                db.PlayerProgresses.Update(dbProgress);
            } else
            {
                dbProgress = new PlayerProgress
                {
                    PlayerId = dbPlayer.Id,
                    IsOfficial = 0
                };
                dbProgress = Utils.AddProgress(delta, dbProgress);
                db.PlayerProgresses.Add(dbProgress);
            }


            //work around the following issue: https://scrn.storni.info/2023-09-05_02-47-21-055320628.png
            if (RoundSettings.State == GameState.EndingGame)
            {
                Cache.Set(steamId, stats.Progress);
            } else
            {
                Cache.Remove(steamId);
            }


            //we currently overwrite these on connect because we can't yet deserialize these bytes and thus can't merge with official progression
            dbPlayer.Selections = stats.Selections;
            dbPlayer.ToolProgress = stats.ToolProgress;
            db.SaveChanges();
        }
        catch (Exception e)
        {
            Console.WriteLine("Error while saving player stats: " + e.Message);
        }
        finally
        {
            DbContextPool.ReturnContext(db);
        }
    }

    public override async Task OnPlayerDisconnected(MyPlayer player)
    {
        Settings.SettingsBalancer(this);
        Console.WriteLine($"[{DateTime.UtcNow}] Player {player.Name} - {player.SteamID} disconnected. Total players: {CurrentPlayerCount - 1}");
    }

    public override Task OnPlayerJoiningToServer(ulong steamId, PlayerJoiningArguments args)
    {
        var db = Db;
        try
        {
            var isAdmin = Admin.IsPlayerAdmin(steamId);
            if (isAdmin) args.Stats.Roles = Roles.Admin;

            var existingPlayer = db.Players.FirstOrDefault(player => (long)steamId == player.SteamId);
            if (existingPlayer == null)
            {
                Cache.Set(steamId, args.Stats.Progress);
                //todo: create player
                //todo: populate db with official stats
                var p = Utils.NewPlayerFrom(steamId, args.Stats);
                var newDbProgress = Utils.SetProgress(args.Stats.Progress, new PlayerProgress { IsOfficial = 1 });
                var newPlayer = db.Players.Add(p);
                newPlayer.Entity.PlayerProgresses.Add(newDbProgress);
                db.SaveChanges();

                return Task.FromResult(args);
            }

            // Update player properties
            existingPlayer.IsBanned = args.Stats.IsBanned;
            existingPlayer.Roles = (int)args.Stats.Roles;
            existingPlayer.Achievements = args.Stats.Achievements;
            existingPlayer.Selections = args.Stats.Selections;
            existingPlayer.ToolProgress = args.Stats.ToolProgress;
            db.SaveChanges();

            var cachedStats = Cache.Get(steamId, true);
            var unofficialProgress = db.PlayerProgresses.FirstOrDefault(pp => pp.IsOfficial.CompareTo(0) == 0 && pp.PlayerId == existingPlayer.Id);
            var officialProgress = db.PlayerProgresses.FirstOrDefault(pp => pp.IsOfficial.CompareTo(1) == 0 && pp.PlayerId == existingPlayer.Id);

            if (officialProgress == null)
            {
                //todo: populate db with official stats
                var newDbProgress = Utils.SetProgress(args.Stats.Progress, new PlayerProgress { IsOfficial = 1 });
                existingPlayer.PlayerProgresses.Add(newDbProgress);
                db.SaveChanges();
                return Task.FromResult(args);
            }

            if (unofficialProgress == null)
            {
                Cache.Set(steamId, args.Stats.Progress);
                return Task.FromResult(args);
            }

            //also check if cached stats are stale (could happen if people disconnect when the map ends and reconnect after a while)
            if (cachedStats != null && !Cache.IsStale())
            {
                /*
                 * ignore official stats and unofficial stats, we already have what we need
                 * set that as return val
                 */
                args.Stats.Progress = cachedStats;
                return Task.FromResult(args);
            }

            /*
             * set official stats in the cache
             * set official stats in db
             * sum official stats with unofficial
             * return summed stats
             */

            officialProgress = Utils.SetProgress(args.Stats.Progress, officialProgress);
            db.PlayerProgresses.Update(officialProgress);
            db.SaveChanges();
            var summedProgress = Utils.AddProgress(args.Stats.Progress, Utils.Clone(unofficialProgress));
            args.Stats.Progress = Utils.ProgressFrom(summedProgress);
            Cache.Set(steamId, args.Stats.Progress);
            return Task.FromResult(args);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error while storing player: {ex.Message}");
        }
        finally
        {
            DbContextPool.ReturnContext(db);
        }

        return Task.CompletedTask;
    }

    public override async Task OnPlayerConnected(MyPlayer player)
    {
        player.ConnectTime = DateTime.Now;
        Settings.SettingsBalancer(this);
        var db = Db;
        try
        {
            player.Modifications.CanSpectate = false;
            var existingPlayer = db.Players.FirstOrDefault(p => (long)player.SteamID == p.SteamId);
            if (existingPlayer != null)
            {
                // Update player
                existingPlayer.Name = player.Name;
                player.Modifications.CanSpectate = existingPlayer!.Roles > (int)Roles.None;
                db.SaveChanges();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error while updating player: {ex.Message}");
        }
        finally
        {
            DbContextPool.ReturnContext(db);
        }

        var blockDetails = Blocks.IsBlocked(player.SteamID, BlockType.Ban);
        if (blockDetails.isBlocked)
        {
            player.Kick(blockDetails.reason);
            return;
        }

        SayToAllChat(Advertisements.GetWelcomeMessage(player.Name));
        Console.WriteLine($"[{DateTime.UtcNow}] Player {player.Name} - {player.SteamID} connected with IP {player.IP}. Total players: {CurrentPlayerCount + 1}");
    }

    public override Task<bool> OnPlayerTypedMessage(MyPlayer player, ChatChannel channel, string msg)
    {
        Console.WriteLine($"[{DateTime.UtcNow}] Player {player.Name} - {player.SteamID} typed: {msg}");
        ChatLogger.StoreChatLog(player, msg);
        var res = ChatProcessor.ProcessChat(msg, player, this);
        if (!res) return Task.FromResult(false);

        var blockResult = Blocks.IsBlocked(player.SteamID, BlockType.Gag);
        if (!blockResult.isBlocked) return Task.FromResult(true);
        player.WarnPlayer($"You are currently gagged: {blockResult.reason}");
        return Task.FromResult(false);
    }

    public override async Task<OnPlayerSpawnArguments?> OnPlayerSpawning(MyPlayer player, OnPlayerSpawnArguments request)
    {
        player.Modifications.RespawnTime = RespawnTime();
        if (Restrictions.IsWeaponRestricted(request.Loadout.PrimaryWeapon.Tool))
        {
            player.WarnPlayer($"You are not allowed to use {request.Loadout.PrimaryWeapon.Tool.Name}!");
            return null;
        }

        if (Restrictions.IsWeaponRestricted(request.Loadout.SecondaryWeapon.Tool))
        {
            player.WarnPlayer($"You are not allowed to use {request.Loadout.SecondaryWeapon.Tool.Name}!");
            return null;
        }

        if (Restrictions.IsGadgetRestricted(request.Loadout.HeavyGadget))
        {
            request.Loadout.HeavyGadget = null;
            // player.SetHeavyGadget("", 0, true);
        }

        if (Restrictions.IsGadgetRestricted(request.Loadout.LightGadget))
        {
            request.Loadout.LightGadget = null;
            // player.SetLightGadget("", 0, true);
        }

        return request;
    }

    public override Task OnPlayerReported(MyPlayer from, MyPlayer to, ReportReason reason, string additional)
    {
        var db = Db;
        try
        {
            var reporterId = db.Players.FirstOrDefault(player => player.SteamId == (long)from.SteamID)?.Id;
            var reportedPlayerId = db.Players.FirstOrDefault(player => player.SteamId == (long)to.SteamID);
            if (reporterId == null || reportedPlayerId == null) return Task.CompletedTask;

            var report = new PlayerReport
            {
                ReporterId = reporterId.Value,
                Reason = reason + " " + additional,
                Status = "Pending",
                AdminNotes = null,
                ReportedPlayer = reportedPlayerId,
                Timestamp = DateTime.Now
            };
            db.PlayerReports.Add(report);
            db.SaveChanges();
            return Task.CompletedTask;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return Task.CompletedTask;
        }
        finally
        {
            DbContextPool.ReturnContext(db);
        }
    }

    public override async Task OnAPlayerDownedAnotherPlayer(OnPlayerKillArguments<MyPlayer> args)
    {
        Balancer.TeamBalancerCheck(this);
        Console.WriteLine($"[{DateTime.UtcNow}] Player {args.Killer.Name} killed {args.Victim.Name} with {args.KillerTool} from a distance of {Vector3.Distance(args.KillerPosition, args.VictimPosition)}m");
        var isSuicide = args.Killer.SteamID == args.Victim.SteamID;
        if (isSuicide)
        {
            Rank.AddSuicide(args.Killer.SteamID);
        } else
        {
            Rank.AddKill(args.Killer.SteamID);
            Rank.AddDeath(args.Victim.SteamID);
        }

        if (!AllowReviving)
        {
            await Task.Delay(500);
            args.Victim.Kill();
        }
    }

    public override async Task OnAPlayerRevivedAnotherPlayer(MyPlayer from, MyPlayer to)
    {
        Rank.AddRevive(from.SteamID);
    }

    public override async Task OnPlayerDied(MyPlayer player)
    {
        if (CurrentPlayerCount < 30)
        {
            Balancer.FastBalance(player, this);
        } else
        {
            Balancer.BalancePlayer(player, this);
        }

        Settings.SettingsBalancer(this);
        player.Modifications.RespawnTime = RespawnTime();
        player.Modifications.SpawningRule = CanSpawnOnPlayers ? SpawningRule.All : SpawningRule.None;
    }

    public override async Task OnRoundStarted()
    {
        Formatting.SafeSetLoadingScreenText(ConfigurationManager.Config.join_text + "\n" + Stats.TopN(3), this);
    }

    public override async Task OnRoundEnded()
    {
        Formatting.SafeSetLoadingScreenText(ConfigurationManager.Config.join_text + "\n" + Stats.TopN(3), this);
    }

    public override Task OnTick()
    {
        if (TextWallHack)
        {
            foreach (var p in AllPlayers)
            {
                var sb = new StringBuilder();
                //get the distance between all players
                //adding cache could reduce the amount of computations by half but since we do this for 7 players max it's not really worth it
                sb.AppendLine($"Text/Map WallHack {RichText.Green}enabled{RichText.EndColor} with less than 7 players");
                sb.AppendLine($"type {RichText.Red}wall{RichText.EndColor} to toggle it on/off");
                sb.AppendLine("Generally once 2-3 players connect, more players will follow");
                sb.AppendLine("Distance to other players:");
                if (p.IsDead || !p.IsAlive || p.HideWallHack) continue;
                foreach (var p2 in AllPlayers)
                {
                    if (p.SteamID == p2.SteamID || p.Team == p2.Team) continue;
                    if (p.IsDead || !p2.IsAlive)
                    {
                        sb.AppendLine($"{p2.Name}: dead");
                        continue;
                    }

                    var distance = Vector3.Distance(p.Position, p2.Position);
                    sb.AppendLine($"{p2.Name}: {distance}m");
                }


                p.Message(sb.ToString(), 10f);
            }
        }

        //don't refresh every tick (tick callback is skipped while nothing is returned)
        Thread.Sleep(500);
        return Task.CompletedTask;
    }
}

public class MyPlayer : Player<MyPlayer>
{
    public DateTime ConnectTime;
    public bool HideWallHack = false;
    public bool IsFlaggedForTeamSwitch;
    private int mDbId = -1;
    private (bool initialized, bool isAdmin) mIsAdmin = (false, false);

    public int DbId
    {
        get
        {
            if (mDbId != -1) return mDbId;
            var db = MyGameServer.Db;
            var existingPlayer = db.Players.FirstOrDefault(player => (long)SteamID == player.SteamId);
            DbContextPool.ReturnContext(db);
            if (existingPlayer == null) return -1;
            mDbId = existingPlayer.Id;
            return mDbId;
        }
    }

    public bool IsAdmin()
    {
        if (mIsAdmin.initialized) return mIsAdmin.isAdmin;
        var admin = Admin.GetAdmin(SteamID);
        mIsAdmin = (true, admin != null);
        return mIsAdmin.isAdmin;
    }
}