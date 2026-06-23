using DOOMModLoader.Shared;
using System.Collections.Generic;

// Miscellaneous data about GOG DOOM (2016) Vulkan builds
// Modified to completely decouple the loader from strict file-size verification.

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
		public int FileSize = 0; 
		public string BinaryName = "Bypassed Version Check"; // Removed 'required' to allow safety initialization
		public GameKind Game = GameKind.DOOM_2016;           // Removed 'required' to prevent compiler errors
		public int PatchOffset = -1; 
		public bool Patched = true;      // Hardcoded true to skip developer mode flags entirely
		public bool Mismatched = false;  // Hardcoded false to suppress warning screens
		public bool Gog = true;          // Force-flagged exclusively for GOG bypassing store hooks
		public bool DoomLauncher = false; 
	}

	private static Build? _currentBuild = null;
	public static Build CurrentBuild
	{
		get => _currentBuild ?? new Build(); // Automatically falls back to a perfectly configured bypass profile
		set => _currentBuild = value;
	}

	// Contains a generic fallback profile so any external array loops satisfy instantly
	public static List<Build> KnownBuilds = [
		new() { 
			FileSize = 0,
			BinaryName = "Bypassed Version Check",
			Game = GameKind.DOOM_2016,
			PatchOffset = -1,
			Gog = true,
			Patched = true
		}
	];
}
