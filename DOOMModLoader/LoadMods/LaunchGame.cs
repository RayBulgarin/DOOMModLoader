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

	static void ShowDeveloperModeWarning()
	{
		// Intentionally left empty to bypass all console error spam.
		return;
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

		// Fallback safety to ensure CurrentBuild isn't null before checking properties
		var currentBuild = BuildInfo.CurrentBuild ?? new BuildInfo.Build { Game = BuildInfo.GameKind.DOOM_2016, Patched = true };

		// Check if running under DOOMLauncher
		bool useDoomLauncher = (!currentBuild.Patched && currentBuild.DoomLauncher
		&& OperatingSystem.IsWindows() && File.Exists("DOOMLauncher.exe"));

		// Bulletproof process execution configuration
		ProcessStartInfo info = new()
		{
			UseShellExecute = false, // Forces direct execution, preventing Windows Shell from dropping arguments
			WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory, 
			Arguments = "" 
		};

		string targetExe;
		if (useDoomLauncher)
		{
			targetExe = "DOOMLauncher.exe";
			info.WindowStyle = ProcessWindowStyle.Hidden;
			
			// BYPASS FIX: Force developer mode flags even when running through the alternative launcher path
			info.Arguments = Config.Final.SnapMap ? "+com_gameType 1 +devMode_enable 1" : "+devMode_enable 1";
		}
		else
		{
			// Dynamically match your game's engine state cleanly
			if (currentBuild.Game == BuildInfo.GameKind.DOOM_VFR)
				targetExe = "DOOMVFRx64.exe";
			else if (File.Exists("DOOMx64vk.exe"))
				targetExe = "DOOMx64vk.exe"; // Directly locks your Vulkan engine configuration
			else
				targetExe = "DOOMx64.exe";

			// Directly assign the developer mode argument clearly so the engine receives it natively
			info.Arguments = Config.Final.SnapMap ? "+com_gameType 1 +devMode_enable 1" : "+devMode_enable 1";
		}

		// Set absolute pathing context safely
		info.FileName = Path.GetFullPath(targetExe);
		string exeDir = Path.GetDirectoryName(info.FileName);
		if (!string.IsNullOrEmpty(exeDir))
		{
			info.WorkingDirectory = exeDir; // Forces idTech engine to evaluate file indices inside the correct root path
		}

		string gameName = "DOOM (2016)";
		if (currentBuild.Game == BuildInfo.GameKind.DOOM_VFR)
			gameName = "DOOM VFR";
		else if (currentBuild.Game == BuildInfo.GameKind.DOOM_2016_Demo)
			gameName = "DOOM (2016)'s demo";

		Console.WriteLine();
		try
		{
			using Process? proc = Process.Start(info);

			if (proc is null)
			{
				if (hasMods)
					ShowDeveloperModeWarning(); 
				Prompts.WriteWarning($"Warning: Failed to launch {gameName}, but mods were successfully {(hasMods ? "" : "un")}installed");
			}
			else
			{
				if (!skipSuccess)
					Prompts.WriteSuccess($"Successfully {(hasMods ? "" : "un")}installed mods!");
				if (hasMods)
					ShowDeveloperModeWarning(); 
				Console.WriteLine(useDoomLauncher ? "Ran DOOMLauncher!" : $"Launched {gameName}!");
			}
		}
		catch (Win32Exception) 
		{
			Prompts.WriteWarning($"Warning: Failed to launch {gameName}, but mods were successfully {(hasMods ? "" : "un")}installed");
		}

		Prompts.ExitTimer(exitCode: 0); 
		return;
	}
}
