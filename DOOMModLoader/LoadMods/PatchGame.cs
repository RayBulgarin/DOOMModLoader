using DOOMModLoader.Shared;
using System;
using System.IO;

// Patches the game executables to not require developer mode when mods are installed
// Special thanks to PowerBall253 (https://github.com/brunoanc) for creating the patch

namespace DOOMModLoader.LoadMods;
static class PatchGame
{
	static bool hasShownPatchingMessage = false;
	static bool shouldShowFailedMessage = false;



	// Patches an executable and returns its information
	// Aborts if the game is DOOM (2016)'s demo
	static BuildInfo.Build? HandleExecutable(string path)
	{
		ReadOnlySpan<byte> oldBytes = [0x40, 0x55, 0x53, 0x56, 0x57, 0x41, 0x56];
		ReadOnlySpan<byte> newBytes = [0xB8, 0x01, 0x00, 0x00, 0x00, 0xC3, 0x90]; // mov eax, 1; ret; nop;

		BuildInfo.Build? result = null;
		bool shouldPatch = false;

		try
		{
			// Open the file for reading, while being as lenient as we can with sharing the file
			using (FileStream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
			{
				result = BuildInfo.KnownBuilds.Find(x => x.FileSize == stream.Length);

				if (result is not null && result.PatchOffset != -1)
				{
					if (Utility.StreamCheckSequence(stream, result.PatchOffset, newBytes))
						result.Patched = true;
					else if (Config.Final.PatchGame && Utility.StreamCheckSequence(stream, result.PatchOffset, oldBytes))
						shouldPatch = true;
				}
				if (result is not null && result.Game == BuildInfo.GameKind.DOOM_2016_Demo)
				{
					Console.WriteLine();
					Console.WriteLine($"Installing {(Config.Final.SnapMap ? "SnapM" : "m")}ods...");
					Console.WriteLine();
					Prompts.WriteError("ERROR: Failed to install mods!");
					Prompts.WriteWarning("DOOM (2016)'s demo is unsupported");
					Prompts.ExitPrompt();
					return result;
				}
			}
			if (!shouldPatch)
				return result;

			if (!hasShownPatchingMessage)
			{
				Console.WriteLine();
				Console.WriteLine("Patching game executables...");
				Console.WriteLine("    Backing up game executables to the \"base\" directory...");
				hasShownPatchingMessage = true;
			}
			File.Copy(path, $"./base/{Path.GetFileNameWithoutExtension(path)} (Pre-DOOMModLoader backup).exe", true);

			Console.WriteLine($"    Patching \"{Path.GetFileName(path)}\"...");

			// Open the file for writing
			using (FileStream stream = File.Open(path, FileMode.Open, FileAccess.Write, FileShare.Read))
			{
				stream.Position = result!.PatchOffset;
				stream.Write(newBytes);
			}
			result.Patched = true;
		}
		catch (Exception e) when (e is ArgumentException or DirectoryNotFoundException
		or FileNotFoundException or IOException or UnauthorizedAccessException)
		{
			if (shouldPatch && !result!.Patched)
				shouldShowFailedMessage = true;
		}

		return result;
	}

	// Patches the game executables if possible, and sets the installed game build
	// Aborts if the game is DOOM (2016)'s demo
	public static void CheckAndPatchGame()
	{
		BuildInfo.Build? build       = HandleExecutable("./DOOMx64.exe");
		BuildInfo.Build? buildVulkan = HandleExecutable("./DOOMx64vk.exe");
		BuildInfo.Build? buildVfr    = HandleExecutable("./DOOMVFRx64.exe");

		if (shouldShowFailedMessage)
			Prompts.WriteWarning("Warning: Failed to patch game executables");

		// Both the OpenGL and Vulkan executables are recognised
		if (build is not null && buildVulkan is not null)
		{
			if (build.BinaryName != buildVulkan.BinaryName
			&& !(build.BinaryName == "Developer Binary - Feb  6 2025 17:35:11 " && buildVulkan.BinaryName == "Developer Binary - Feb 24 2025 20:36:27 "))
				build.Mismatched = true;
			if (build.Game == BuildInfo.GameKind.DOOM_VFR) // Resist renamed executables
				build.Game = BuildInfo.GameKind.DOOM_2016;
			build.Patched = (build.Patched && buildVulkan.Patched);
			BuildInfo.CurrentBuild = build;
			return;
		}

		BuildInfo.CurrentBuild = new BuildInfo.Build // An empty fallback for unrecognised game builds
		{
			BinaryName = "",
			Game = BuildInfo.GameKind.DOOM_2016,
			Gog = File.Exists("./Galaxy64.dll"),
		};

		// DOOM VFR
		if (build is null && buildVulkan is null && !File.Exists("./DOOMx64.exe") && !File.Exists("./DOOMx64vk.exe"))
		{
			if (buildVfr is not null)
			{
				buildVfr.Game = BuildInfo.GameKind.DOOM_VFR; // Resist renamed executables
				BuildInfo.CurrentBuild = buildVfr;
			}
			else if (File.Exists("./DOOMVFRx64.exe"))
				BuildInfo.CurrentBuild.Game = BuildInfo.GameKind.DOOM_VFR;
			return;
		}

		// There's only an OpenGL executable
		if (buildVulkan is null && !File.Exists("./DOOMx64vk.exe"))
		{
			if (build is not null)
			{
				if (build.Game == BuildInfo.GameKind.DOOM_VFR) // Resist renamed executables
					build.Game = BuildInfo.GameKind.DOOM_2016;
				BuildInfo.CurrentBuild = build;
			}
			return;
		}

		// The OpenGL or Vulkan executable was recognised, but not both
		build ??= buildVulkan;
		if (build is not null)
		{
			if (build.Game == BuildInfo.GameKind.DOOM_VFR) // Resist renamed executables
				build.Game = BuildInfo.GameKind.DOOM_2016;
			build.Patched = false;
			build.Mismatched = true;
			BuildInfo.CurrentBuild = build;
			return;
		}

		// Neither the OpenGL nor Vulkan executable was recognised. Keep the fallback set earlier
	}
}
