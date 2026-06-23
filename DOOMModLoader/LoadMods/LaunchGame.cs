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
			Arguments = "" // FIXED: Initialize as empty string to prevent append failures
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

			// FIXED: Directly assign the developer mode argument clearly so the engine receives it
			if (Config.Final.SnapMap)
				info.Arguments = "+com_gameType 1 +devMode_enable 1";
			else
				info.Arguments = "+devMode_enable 1";
		}

		string gameName = "DOOM (2016) Vulkan";
		if (BuildInfo.CurrentBuild.Game == BuildInfo.GameKind.DOOM_VFR)
			gameName = "DOOM VFR";
		else if (BuildInfo.CurrentBuild.Game == BuildInfo.GameKind.DOOM_2016_Demo)
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
