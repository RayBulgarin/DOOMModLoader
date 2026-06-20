using DOOMModLoader.Shared;
using System.Collections.Generic;

// Miscellaneous data about GOG DOOM (2016) builds

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
        public int FileSize; // A crude, fast way to tell builds apart
        public required string BinaryName;
        public required GameKind Game;
        public int PatchOffset = -1; // The offset that needs to be patched to not require developer mode
        public bool Patched = false; // Whether DOOMModLoader has patched this game build
        public bool Mismatched = false; // If true, the OpenGL and Vulkan executables are mismatched
        public bool Gog = true; // Defaulting strictly to true for GOG deployment
        public bool DoomLauncher = false; 
    }

    public static Build? CurrentBuild = null;

    // Purged all Steam configurations. Only official GOG builds remain.
    public static List<Build> KnownBuilds = [
        // Release date: 2025-04-18 (GOG, Vulkan) - Prioritized for DOOMx64vk.exe
        new() { 
            FileSize = 0x60D1400,
            BinaryName = "Developer Binary - Feb 24 2025 20:36:27 ", // Ends with a space
            Game = GameKind.DOOM_2016,
            PatchOffset = 0x1698600,
            Gog = true,
        },
        // Release date: 2025-04-18 (GOG, OpenGL)
        new() { 
            FileSize = 0x49D6000,
            BinaryName = "Developer Binary - Feb  6 2025 17:35:11 ", // Ends with a space
            Game = GameKind.DOOM_2016,
            PatchOffset = 0x169A200,
            Gog = true,
        }
    ];
}
