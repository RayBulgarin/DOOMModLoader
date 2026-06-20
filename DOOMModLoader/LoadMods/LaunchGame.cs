using DOOMModLoader.Shared;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;

// Asks whether to launch the game, and launches it

namespace DOOMModLoader.LoadMods;
static class LaunchGame
{
    static bool skipSuccess = false;

    // Asks whether or not to launch the game, if the user hasn't previously chosen
    public static void AskToLaunch(bool hasMods)
    {
        if (Config.Final.LaunchGame is not null) // Don't ask if it's already set
            return;

        Console.WriteLine();
        Prompts.WriteSuccess($"\nSuccessfully {(hasMods ? "" : "un")}installed mods!");
        skipSuccess = true;
        Console.Write(
            ""
            + "\n"
            + "\nDo you want to automatically launch the game after installing mods?"
            + "\nThis can be changed later by editing \"DOOMModLoaderSettings.txt\""
            + "\n"
            + "\n(Press [Y] to launch the game)"
            + "\n(Press [N] to deny and exit)"
        );
        Config.File.LaunchGame = Prompts.GetYesOrNo();

        if (Config.File.LaunchGame is null)
            Prompts.WriteWarning("Warning: Failed to detect keystroke");
        else if (Config.File.LaunchGame == true)
            Config.ShouldSave = true;
        else
        {
            Config.ShouldSave = true;
            Config.File.Save();
            Environment.Exit(0); // Exit immediately if the user pressed N
            return;
        }
    }

    // Shows a warning about requiring "+devMode_enable 1" if the game is unpatched
    static void ShowDeveloperModeWarning(bool useDoomLauncher)
    {
        if (BuildInfo.CurrentBuild!.Mismatched || string.IsNullOrEmpty(BuildInfo.CurrentBuild.BinaryName))
        {
            Console.WriteLine();
            Prompts.WriteWarning("Warning: Failed to recognise game version");
            if (!BuildInfo.CurrentBuild.Patched && !useDoomLauncher)
                Prompts.WriteWarning("You will need \"+devMode_enable 1\" set as a launch option while mods are installed, and in-game settings will not be saved correctly");
            Console.WriteLine();
            return;
        }
        else if (BuildInfo.CurrentBuild.Patched || useDoomLauncher)
            return;

        Console.WriteLine();
        Prompts.WriteWarning("Warning: You will need \"+devMode_enable 1\" set as a launch option while mods are installed, and in-game settings will not be saved correctly");
        if (BuildInfo.CurrentBuild.PatchOffset != -1 && !Config.Final.PatchGame)
        {
            if (Config.File.PatchGame)
                Prompts.WriteWarning("Use \"-patchgame\" to fix this"); 
            else
                Prompts.WriteWarning("Set \"patchGame\" to \"true\" in \"DOOMModLoaderSettings.txt\" to fix this");
        }
        else
        {
            Prompts.WriteWarning("Verify/Repair DOOM (2016)'s installation through GOG GALAXY or the game's installer to fix this");
        }
        Console.WriteLine();
    }

    // Launches the game and exits after a timer, or waits for a keystroke and exits
    public static void LaunchAndExit(bool hasMods)
    {
        if (Config.Final.LaunchGame != true)
        {
            if (!skipSuccess)
            {
                Console.WriteLine();
                Prompts.WriteSuccess($"Successfully {(hasMods ? "" : "un")}installed mods!");
            }
            Prompts.ExitPrompt(exitCode: 0); // Exit after a keystroke
            return;
        }

        // If the game executables weren't patched, use DOOMLauncher if it exists
        bool useDoomLauncher = (!BuildInfo.CurrentBuild!.Patched && BuildInfo.CurrentBuild.DoomLauncher
        && OperatingSystem.IsWindows() && File.Exists("./DOOMLauncher.exe"));

        ProcessStartInfo info = new()
        {
            UseShellExecute = true, 
        };

        if (useDoomLauncher)
        {
            info.FileName = $".{Path.DirectorySeparatorChar}DOOMLauncher.exe"; 
            if (Config.Final.SnapMap)
                info.Arguments = "+com_gameType 1";
            info.WindowStyle = ProcessWindowStyle.Hidden;
        }
        else
        {
            // Cleaned up exclusively for GOG deployment targeting Vulkan build directly
            if (BuildInfo.CurrentBuild.Game == BuildInfo.GameKind.DOOM_VFR)
                info.FileName = $".{Path.DirectorySeparatorChar}DOOMVFRx64.exe";
            else
                info.FileName = $".{Path.DirectorySeparatorChar}DOOMx64vk.exe";

            if (Config.Final.SnapMap)
                info.Arguments = "+com_gameType 1";
            if (!BuildInfo.CurrentBuild.Patched)
                info.Arguments += $"{(Config.Final.SnapMap ? " " : "")}+devMode_enable 1";
        }

        string gameName = "DOOM (2016) Vulkan";
        if (BuildInfo.CurrentBuild.Game == BuildInfo.GameKind.DOOM_VFR)
            gameName = "DOOM VFR";
        else if (BuildInfo.CurrentBuild.Game == BuildInfo.GameKind.DOOM_2016_Demo)
            gameName = "DOOM (20
