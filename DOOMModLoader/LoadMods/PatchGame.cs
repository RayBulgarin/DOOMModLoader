using DOOMModLoader.Shared;
using System;
using System.IO;

// Patches the game executables to not require developer mode when mods are installed
// Special thanks to PowerBall253 (https://github.com/brunoanc) for creating the patch

namespace DOOMModLoader.LoadMods;
static class PatchGame
{
	// Game version verification and binary matching have been completely removed.
	// This forces a generic successful build state to bypass the loader's version restrictions.
	public static void CheckAndPatchGame()
	{
		// Force-assign a valid build profile to trick the loader into skipping unrecognized version warnings
		BuildInfo.CurrentBuild = new BuildInfo.Build
		{
			BinaryName = "Bypassed Version Check",
			Game = BuildInfo.GameKind.DOOM_2016,
			Patched = true,      // Set to true to bypass devMode enforcement prompts
			Mismatched = false   // Clear mismatch flags
		};

		// Check if running under DOOM VFR instead to maintain basic directory compatibility
		if (!File.Exists("./DOOMx64.exe") && !File.Exists("./DOOMx64vk.exe") && File.Exists("./DOOMVFRx64.exe"))
		{
			BuildInfo.CurrentBuild.Game = BuildInfo.GameKind.DOOM_VFR;
		}
	}
}
