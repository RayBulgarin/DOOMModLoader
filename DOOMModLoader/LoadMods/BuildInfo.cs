using DOOMModLoader.Shared;
using System.Collections.Generic;

// Miscellaneous data about all DOOM (2016) and DOOM VFR builds

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
		public int FileSize; // A crude, fast way to tell builds apart - No conflicts as of 2025
		public required string BinaryName;
		public required GameKind Game;
		public int PatchOffset = -1; // The offset that needs to be patched to not require developer mode
		public bool Patched = false; // Whether DOOMModLoader has patched this game build - Will be set later
		public bool Mismatched = false; // If true, the OpenGL and Vulkan executables are mismatched - Will be set later
		public bool Gog = false; // Is this the GOG version or the Steam version?
		public bool DoomLauncher = false; // Whether https://github.com/brunoanc/DOOMLauncher v3.0 supports this build
		public int SteamAppId
		{
			get => Game switch
			{
				GameKind.DOOM_2016      => 379720,
				GameKind.DOOM_2016_Demo => 479030,
				GameKind.DOOM_VFR       => 650000,
				_ => 379720,
			};
		}
	}

	public static Build? CurrentBuild = null;



	// All builds of DOOM (2016), its demo, and DOOM VFR that have been released on Steam and GOG as of 2025
	public static List<Build> KnownBuilds = [
		// Latest DOOM (2016) version, listed first for search efficiency
		new() { // Release date: 2024-04-11 (OpenGL)
			FileSize = 0x49D8400,
			BinaryName = "20240321-104810-ginger-fuchsia",
			Game = GameKind.DOOM_2016,
			PatchOffset = 0x169C0E0,
			DoomLauncher = true,
		},
		new() { // Release date: 2024-04-11 (Vulkan)
			FileSize = 0x60D3600,
			BinaryName = "20240321-104810-ginger-fuchsia",
			Game = GameKind.DOOM_2016,
			PatchOffset = 0x169B260,
			DoomLauncher = true,
		},
		new() { // Release date: 2025-04-18 (GOG, OpenGL)
			FileSize = 0x49D6000,
			// The GOG version number is "20240321-110145-gentle-wolf", but the binary name is...
			BinaryName = "Developer Binary - Feb  6 2025 17:35:11 ", // Ends with a space
			Game = GameKind.DOOM_2016,
			PatchOffset = 0x169A200,
			Gog = true,
		},
		new() { // Release date: 2025-04-18 (GOG, Vulkan)
			FileSize = 0x60D1400,
			// The GOG version number is "20240321-110145-gentle-wolf", but the binary name is...
			BinaryName = "Developer Binary - Feb 24 2025 20:36:27 ", // Ends with a space
			Game = GameKind.DOOM_2016,
			PatchOffset = 0x1698600,
			Gog = true,
		},


		// Latest DOOM (2016) demo version
		new() { // Release date: 2016-07-27 (OpenGL)
			FileSize = 0x6F4FE00,
			BinaryName = "20160720-180331-purple-razzmatazz",
			Game = GameKind.DOOM_2016_Demo,
		},
		new() { // Release date: 2016-07-27 (Vulkan)
			FileSize = 0x87CDC00,
			BinaryName = "20160720-180331-purple-razzmatazz",
			Game = GameKind.DOOM_2016_Demo,
		},


		// Latest DOOM VFR version
		new() { // Release date: 2018-01-30
			FileSize = 0xC6A18D0,
			BinaryName = "20180119-133016-fuchsia-ash",
			Game = GameKind.DOOM_VFR,
			Patched = true, // Developer mode is unnecessary by default!
		},


		// Previous DOOM (2016) versions
		new() { // Release date: 2016-05-15
			FileSize = 0x68E3A00,
			BinaryName = "20160506-195230-olive-harlequin",
			Game = GameKind.DOOM_2016,
		},
		new() { // Release date: 2016-05-27
			FileSize = 0x6F57200,
			BinaryName = "20160526-223240-denim-purple",
			Game = GameKind.DOOM_2016,
		},
		new() { // Release date: 2016-06-30
			FileSize = 0x6AC8C00,
			BinaryName = "20160627-151936-ultramarine-fulvous",
			Game = GameKind.DOOM_2016,
		},
		new() { // Release date: 2016-07-11 (OpenGL)
			FileSize = 0x6F22C00,
			BinaryName = "20160706-141600-denim-ginger",
			Game = GameKind.DOOM_2016,
		},
		new() { // Release date: 2016-07-11 (Vulkan)
			FileSize = 0x833D800,
			BinaryName = "20160706-141600-denim-ginger",
			Game = GameKind.DOOM_2016,
		},
		new() { // Release date: 2016-07-29 (OpenGL)
			FileSize = 0x6FFAE00,
			BinaryName = "20160725-103004-ecru-ultramarine",
			Game = GameKind.DOOM_2016,
		},
		new() { // Release date: 2016-07-29 (Vulkan)
			FileSize = 0x89C4E00,
			BinaryName = "20160725-103004-ecru-ultramarine",
			Game = GameKind.DOOM_2016,
		},
		new() { // Release date: 2016-08-04 (OpenGL)
			FileSize = 0x6F56C00,
			BinaryName = "20160803-143116-cherry-orchid",
			Game = GameKind.DOOM_2016,
		},
		new() { // Release date: 2016-08-04 (Vulkan)
			FileSize = 0x8377E00,
			BinaryName = "20160803-143116-cherry-orchid",
			Game = GameKind.DOOM_2016,
		},
		new() { // Release date: 2016-08-15 (OpenGL)
			FileSize = 0x705BC00,
			BinaryName = "20160808-123534-peach-almond",
			Game = GameKind.DOOM_2016,
		},
		new() { // Release date: 2016-08-15 (Vulkan)
			FileSize = 0x8AEDE00,
			BinaryName = "20160808-123534-peach-almond",
			Game = GameKind.DOOM_2016,
		},
		new() { // Release date: 2016-09-22 (OpenGL)
			FileSize = 0x70C2200,
			BinaryName = "20160920-100028-amber-viridian",
			Game = GameKind.DOOM_2016,
		},
		new() { // Release date: 2016-09-22 (Vulkan)
			FileSize = 0x9447000,
			BinaryName = "20160920-100028-amber-viridian",
			Game = GameKind.DOOM_2016,
		},
		new() { // Release date: 2016-10-19 (OpenGL)
			FileSize = 0x7102A00,
			BinaryName = "20161012-142702-slate-sapphire",
			Game = GameKind.DOOM_2016,
		},
		new() { // Release date: 2016-10-19 (Vulkan)
			FileSize = 0x87DFA00,
			BinaryName = "20161012-142702-slate-sapphire",
			Game = GameKind.DOOM_2016,
		},
		new() { // Release date: 2016-11-14 (OpenGL)
			FileSize = 0x6EFAC00,
			BinaryName = "20161109-141734-blue-coral",
			Game = GameKind.DOOM_2016,
		},
		new() { // Release date: 2016-11-14 (Vulkan)
			FileSize = 0x87DAA00,
			BinaryName = "20161109-141734-blue-coral",
			Game = GameKind.DOOM_2016,
		},
		new() { // Release date: 2016-12-07 (OpenGL)
			FileSize = 0x4695CD0,
			BinaryName = "20161201-145834-khaki-liver",
			Game = GameKind.DOOM_2016,
		},
		new() { // Release date: 2016-12-07 (Vulkan)
			FileSize = 0x5DA12D0,
			BinaryName = "20161201-145834-khaki-liver",
			Game = GameKind.DOOM_2016,
		},
		new() { // Release date: 2016-12-20 (OpenGL)
			FileSize = 0x46960D0,
			BinaryName = "20161219-101605-purple-viridian",
			Game = GameKind.DOOM_2016,
		},
		new() { // Release date: 2016-12-20 (Vulkan)
			FileSize = 0x5DA14D0,
			BinaryName = "20161219-101605-purple-viridian",
			Game = GameKind.DOOM_2016,
		},
		new() { // Release date: 2017-07-19 (OpenGL)
			FileSize = 0x46DEA00,
			BinaryName = "20170531-000033-lavender-mint",
			Game = GameKind.DOOM_2016,
		},
		new() { // Release date: 2017-07-19 (Vulkan)
			FileSize = 0x5DEA000,
			BinaryName = "20170531-000033-lavender-mint",
			Game = GameKind.DOOM_2016,
		},
		new() { // Release date: 2017-08-24 (OpenGL)
			FileSize = 0x45CB0D0,
			BinaryName = "20170818-165442-iceberg-brown",
			Game = GameKind.DOOM_2016,
		},
		new() { // Release date: 2017-08-24 (Vulkan)
			FileSize = 0x5CD62D0,
			BinaryName = "20170818-165442-iceberg-brown",
			Game = GameKind.DOOM_2016,
		},
		new() { // Release date: 2018-03-29 (OpenGL)
			FileSize = 0x48802D0,
			BinaryName = "20180321-154133-liver-goldenrod",
			Game = GameKind.DOOM_2016,
			DoomLauncher = true,
		},
		new() { // Release date: 2018-03-29 (Vulkan)
			FileSize = 0x5F7D6D0,
			BinaryName = "20180321-154133-liver-goldenrod",
			Game = GameKind.DOOM_2016,
			DoomLauncher = true,
		},
		// The 2024-04-11 Steam and 2025-04-18 GOG builds are at the top


		// Previous DOOM (2016) demo versions
		new() { // Release date: 2016-06-13
			FileSize = 0x6A07200,
			BinaryName = "20160601-092857-purple-pink",
			Game = GameKind.DOOM_2016_Demo,
		},
		new() { // Release date: Also 2016-06-13
			FileSize = 0x685B200,
			BinaryName = "20160613-132958-kelly-taupe",
			Game = GameKind.DOOM_2016_Demo,
		},
		// The 2016-07-27 builds are at the top


		// Previous DOOM VFR version
		new() { // Release date: 2017-11-30
			FileSize = 0xC572CD0,
			BinaryName = "20171130-121024-pink-vanilla",
			Game = GameKind.DOOM_VFR,
			Patched = true, // Developer mode is unnecessary by default!
		},
		// The 2018-01-30 build is at the top
	];
}
