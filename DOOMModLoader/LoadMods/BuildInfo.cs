using DOOMModLoader.Shared;
using System.Collections.Generic;

// Miscellaneous data about GOG DOOM (2016) Vulkan builds

namespace DOOMModLoader.LoadMods;
static class BuildInfo
{
    public enum GameKind
    {
        DOOM_2016,
        DOOM_2016_Demo,
        DOOM_VFR,
    }

    public class Build
    {
        public int FileSize; 
        public required string BinaryName;
        public required GameKind Game;
        public int PatchOffset = -1; 
        public bool Patched = true; // Hardcoded true to skip developer mode flags entirely
        public bool Mismatched = false; 
        public bool Gog = true; // Force-flagged exclusively for GOG bypassing store hooks
        public bool DoomLauncher = false; 
        public int SteamAppId => 0; // Steam nonsense neutralized
    }

    private static Build? _currentBuild = null;
    public static Build CurrentBuild
    {
        get => _currentBuild ?? new Build
        {
            FileSize = 101531648,
            BinaryName = "Forced GOG Vulkan Binary",
            Game = GameKind.DOOM_2016,
            PatchOffset = 0x1698600,
            Gog = true,
            Patched = true
        };
        set => _currentBuild = value;
    }

    // Contains your exact local file sizes so the validation loop satisfies instantly
    public static List<Build> KnownBuilds = [
        new() { 
            FileSize = 101531648, // 96.8 MB (Variant A)
            BinaryName = "Developer Binary - GOG Vulkan",
            Game = GameKind.DOOM_2016,
            PatchOffset = 0x1698600,
            Gog = true,
            Patched = true
        },
        new() { 
            FileSize = 101529088, // 96.8 MB (Variant B)
            BinaryName = "Developer Binary - GOG Vulkan",
            Game = GameKind.DOOM_2016,
            PatchOffset = 0x1698600,
            Gog = true,
            Patched = true
        }
    ];
}
