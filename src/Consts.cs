﻿using TerrariaApi.Server;

namespace Chireiden.TShock.Omni;

public partial class Plugin : TerrariaPlugin
{
    public static class Consts
    {
        public static class Permissions
        {
            public readonly static string Whynot = "chireiden.omni.whynot";
            public readonly static string TogglePvP = "chireiden.omni.togglepvp";
            public readonly static string ToggleTeam = "chireiden.omni.toggleteam";
            public readonly static string PvPCommand = "chireiden.omni.setpvp";
            public readonly static string TeamCommand = "chireiden.omni.setteam";
            public readonly static string SyncLoadout = "chireiden.omni.syncloadout";
            public readonly static string TimeoutCommand = "chireiden.omni.timeout";
            public readonly static string IntervalCommand = "chireiden.omni.interval";
            public readonly static string ClearInterval = "chireiden.omni.cleartimeout";
            public readonly static string ShowTimeout = "chireiden.omni.showtimeout";
            public static class Admin
            {
                public readonly static string Ghost = "chireiden.omni.ghost";
                public readonly static string SetLanguage = "chireiden.omni.setlang";
                public readonly static string SetPvp = "chireiden.omni.admin.setpvp";
                public readonly static string SetTeam = "chireiden.omni.admin.setteam";
                public readonly static string TriggerGarbageCollection = "chireiden.omni.admin.gc";
                public readonly static string DebugStat = "chireiden.omni.admin.debugstat";
                public readonly static string MaxPlayers = "chireiden.omni.admin.maxplayers";
                public readonly static string TileProvider = "chireiden.omni.admin.tileprovider";
                public readonly static string RawBroadcast = "chireiden.omni.admin.rawbroadcast";
                public readonly static string Sudo = "chireiden.omni.admin.sudo";
                public readonly static string DetailedPermissionStackTrace = "chireiden.omni.whynot.detailed";
                public readonly static string ListClients = "chireiden.omni.admin.listclients";
                public readonly static string DumpBuffer = "chireiden.omni.admin.dumpbuffer";
                public readonly static string TerminateSocket = "chireiden.omni.admin.terminatesocket";
            }
        }
        public static class Commands
        {
            public readonly static string Whynot = "whynot";
            public readonly static string Ghost = "ghost";
            public readonly static string SetLanguage = "setlang";
            public readonly static string SetPvp = "_pvp";
            public readonly static string SetTeam = "_team";
            public readonly static string TriggerGarbageCollection = "_gc";
            public readonly static string DebugStat = "_debugstat";
            public readonly static string MaxPlayers = "maxplayers";
            public readonly static string TileProvider = "tileprovider";
            public readonly static string Timeout = "settimeout";
            public readonly static string Interval = "setinterval";
            public readonly static string ClearInterval = "clearinterval";
            public readonly static string ShowTimeout = "showdelay";
            public readonly static string RawBroadcast = "rbc";
            public readonly static string Sudo = "runas";
            public readonly static string ListClients = "listclients";
            public readonly static string DumpBuffer = "dumpbuffer";
            public readonly static string TerminateSocket = "kc";
        }
        public static class DataKey
        {
            public readonly static string Ghost = "chireiden.data.ghost";
            public readonly static string PermissionHistory = "chireiden.data.permissionhistory";
            public readonly static string DetectPE = "chireiden.data.ped";
            public readonly static string IsPE = "chireiden.data.ispe";
            public readonly static string DelayCommands = "chireiden.data.delaycommands";
            public readonly static string PendingRevertHeal = "chireiden.data.pendingheal";
        }
        public readonly static string ConfigFile = "chireiden.omni.json";
        public readonly static string VanillaGroup = "chireiden_vanilla";
    }
}
