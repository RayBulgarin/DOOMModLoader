using DOOMModLoader.Shared;
using System;
using System.Buffers;
using System.IO;
using System.Security;
using System.Security.Cryptography;

// Miscellaneous mod-loading methods

namespace DOOMModLoader.LoadMods;
static class HandleMiscellaneous
{
	static string videoDirectory = ""; // Only calculate the path to "./base/video/(snap_)mods/" once
	static SearchValues<char>? goodVideoNameChars;



	// Replaces one sequence with another in a span
	// Does not modify the original span
	static Span<byte> SpanReplaceSequence(Span<byte> span, ReadOnlySpan<byte> oldValue, ReadOnlySpan<byte> newValue)
	{
		int start = 0;
		while (true)
		{
			int match = MemoryExtensions.IndexOf(span[start .. ^0], oldValue);
			if (match == -1)
				return span;

			span = (byte[])[
				.. span[0 .. (start + match)],
				.. newValue,
				.. span[(start + match + oldValue.Length) .. ^0],
			];
			start += (match + newValue.Length);
		}
	}

	// Loads a resource, and replaces one sequence with another in it
	public static void LoadReplaceData(Stream source, FileStream destination, int length, ReadOnlySpan<byte> oldValue, ReadOnlySpan<byte> newValue)
	{
		// Todo: If "length" is big, then buffer the data and read, replace, and write one chunk at a time
		// Make sure to handle replacements properly at buffer boundaries
		// We expect less than 50 megabytes here, so we can just load it all at once for now
		Span<byte> bytes = new byte[length];
		try
		{
			source.ReadExactly(bytes);
			bytes = SpanReplaceSequence(bytes, oldValue, newValue);
			destination.Write(bytes);
		}
		catch (Exception e) when (e is InvalidDataException or IOException)
		{
			Console.WriteLine();
			Prompts.WriteError("ERROR: Failed to install mods!");
			Prompts.WriteWarning("Make sure that none of the game files are currently open. Try rebooting your computer and running DOOMModLoader again");
			Prompts.WriteVerbose($"({e.GetType().Name} in LoadReplaceData)");
			Prompts.ExitPrompt();
			return;
		}
	}

	// Aborts if the path contains anything but a-z, 0-9, "_", "/", and ".bik"/".bk2", or is forbidden on Windows
	static void ValidateVideoFileName(string relativePath)
	{
		// Make sure that it's in "video/mods/". Backing out with "/../" is forbidden later
		if (!relativePath.StartsWithOrdinal("video/mods/"))
		{
			Console.WriteLine();
			Prompts.WriteError("ERROR: Failed to install mods!");
			Prompts.WriteWarning($"\"{relativePath}\" is forbidden. Custom videos are only allowed within \"video/mods/\"");
			Prompts.ExitPrompt();
			return;
		}

		// Only ".bik" and ".bk2" videos are supported
		if (Path.GetExtension(relativePath) is not (".bik" or ".bk2"))
		{
			Console.WriteLine();
			Prompts.WriteError("ERROR: Failed to install mods!");
			Prompts.WriteWarning($"\"{relativePath}\" is forbidden. Custom videos must be Bink \".bik\" or \".bk2\" files");
			Prompts.ExitPrompt();
			return;
		}

		// Only allow a-z, 0-9, "_", and "/" before the extension
		// If file name limits are loosened later, make sure to still forbid "*:<>?\| and control characters,
		// and to either forbid uppercase A-Z or take special care to merge "case-conflicting" files/directories
		goodVideoNameChars ??= SearchValues.Create("abcdefghijklmnopqrstuvwxyz0123456789_/");

		if (MemoryExtensions.ContainsAnyExcept(relativePath.AsSpan()["video/mods/".Length .. ".bik".Length], goodVideoNameChars))
		{
			Console.WriteLine();
			Prompts.WriteError("ERROR: Failed to install mods!");
			Prompts.WriteWarning($"\"{relativePath}\" is forbidden. Custom video filenames must only contain a-z, 0-9, \"_\", \"/\", and a \".bik\" or \".bk2\" extension");
			Prompts.ExitPrompt();
			return;
		}

		// Windows doesn't allow files nor directories with these names
		// ".", "..", "COM¹", "COM²", "COM³", "LPT¹", "LPT²", and "LPT³" are forbidden by the previous check
		foreach (string x in relativePath["video/mods/".Length .. ^0].Split('/'))
		{
			string name = $"{x}.";
			name = name[0 .. name.IndexOfOrdinal('.')]; // The first period, not "Path.GetFileNameWithoutExtension"
			if (name is "" or "aux" or "com1" or "com2" or "com3" or "com4" or "com5"
			or "com6" or "com7" or "com8" or "com9" or "con" or "lpt1" or "lpt2" or "lpt3"
			or "lpt4" or "lpt5" or "lpt6" or "lpt7" or "lpt8" or "lpt9" or "nul" or "prn")
			{
				Console.WriteLine();
				Prompts.WriteError("ERROR: Failed to install mods!");
				Prompts.WriteWarning($"\"{relativePath}\" is forbidden. Windows doesn't allow \"{name.ToUpperInvariant()}\" as a filename.");
				Prompts.ExitPrompt();
				return;
			}
		}
	}

	// Writes a custom video to -/DOOM/base/video/(snap_)mods/
	public static void LoadVideo(Stream source, string relativePath, int length)
	{
		ValidateVideoFileName(relativePath);

		string outPath = $"./base/{relativePath}";
		if (Config.Final.SnapMap) // Use "video/snap_mods/" instead of "video/mods/" for SnapMap
			outPath = outPath.Insert("./base/video/".Length, "snap_");

		Span<byte> bytes = new byte[length];
		try
		{
			if (string.IsNullOrEmpty(videoDirectory))
				videoDirectory = Path.GetFullPath($"./base/video/{(Config.Final.SnapMap ? "snap_" : "")}mods/");

			outPath = Path.GetFullPath(outPath);
			if (!outPath.StartsWithOrdinal(videoDirectory)) // Double-check that it's definitely still in "video/mods/"
			{
				Console.WriteLine();
				Prompts.WriteError("ERROR: Failed to install mods!");
				Prompts.WriteWarning($"\"{relativePath}\" is forbidden. Custom videos are only allowed within \"video/mods/\"");
				Prompts.ExitPrompt();
				return;
			}
			source.ReadExactly(bytes);
			Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
			File.WriteAllBytes(outPath, bytes);
		}
		catch (Exception e) when (e is DirectoryNotFoundException or InvalidDataException or IOException
		or NotSupportedException or PathTooLongException or UnauthorizedAccessException)
		{
			Console.WriteLine();
			Prompts.WriteError("ERROR: Failed to install mods!");
			Prompts.WriteWarning("Make sure that none of the game files are currently open. Try rebooting your computer and running DOOMModLoader again");
			Prompts.WriteVerbose($"({e.GetType().Name} in LoadVideo)");
			Prompts.ExitPrompt();
			return;
		}

		HandleMods.Data.ModResources.Add(relativePath);
		Prompts.WriteVerbose($"       Installed {relativePath}");
	}

	// Writes DoomLegacyMod ("dinput8.dll") to disk
	public static void LoadDoomLegacyMod(Stream source, int length)
	{
		const int expectedLength = 0x7C00;

		if (File.Exists("./dinput8.dll"))
		{
			Console.WriteLine("        DoomLegacyMod (\"dinput8.dll\") is already installed");
			return;
		}

		// Check DoomLegacyMod's version
		if (length != expectedLength)
		{
			Prompts.WriteWarning("        Warning: Failed to recognise this version of DoomLegacyMod (\"dinput8.dll\"). Skipping");
			return;
		}

		Span<byte> bytes = new byte[length];
		try
			{source.ReadExactly(bytes);}
		catch (Exception e) when (e is InvalidDataException or IOException)
		{
			Console.WriteLine();
			Prompts.WriteError("ERROR: Failed to install mods!");
			Prompts.WriteWarning("Make sure that none of the game files are currently open. Try rebooting your computer and running DOOMModLoader again");
			Prompts.WriteVerbose($"({e.GetType().Name} in LoadDoomLegacyMod > source.ReadExactly)");
			Prompts.ExitPrompt();
			return;
		}
		ReadOnlySpan<byte> hash = SHA256.HashData(bytes); // We use SHA-256 elsewhere anyway, so just use it here too

		int version;
		if (MemoryExtensions.SequenceEqual<byte>(hash, [
			0x6C, 0xCA, 0x6C, 0xBB, 0x8D, 0x79, 0xA5, 0x13, 0xE1, 0x9B, 0x5C, 0xB6, 0x77, 0xD7, 0x15, 0x0B,
			0x75, 0x54, 0x87, 0xB0, 0x61, 0x5B, 0x17, 0x32, 0x22, 0x86, 0xC4, 0x53, 0xA0, 0xE6, 0x73, 0x30,
		]))
			version = 2024;
		else if (MemoryExtensions.SequenceEqual<byte>(hash, [
			0x78, 0x1E, 0xEE, 0xF0, 0x67, 0xA7, 0xF9, 0x7F, 0x7E, 0x2B, 0xC5, 0x26, 0x2E, 0xD6, 0x8B, 0x35,
			0xF1, 0x24, 0xF9, 0x33, 0x34, 0x2A, 0x67, 0x85, 0x89, 0x78, 0xC6, 0xB4, 0xB7, 0x56, 0xE4, 0xE4,
		]))
			version = 2018;
		else
		{
			Prompts.WriteWarning("        Warning: Failed to recognise this version of DoomLegacyMod (\"dinput8.dll\"). Skipping");
			return;
		}

		// Check the game's version
		if (!File.Exists("./DOOMx64.exe"))
		{
			Prompts.WriteWarning($"        Warning: This version of DoomLegacyMod (\"dinput8.dll\") only supports the {version} version of DOOM (2016). Skipping");
			return;
		}

		try
		{
			if ((version == 2024 && BuildInfo.CurrentBuild!.BinaryName == "20240321-104810-ginger-fuchsia")
			||  (version == 2018 && BuildInfo.CurrentBuild!.BinaryName == "20180321-154133-liver-goldenrod"))
			{
				File.WriteAllBytes("./dinput8.dll", bytes);
				Console.WriteLine("        Installed DoomLegacyMod (\"dinput8.dll\")");
			}
			else
				Prompts.WriteWarning($"        Warning: This version of DoomLegacyMod (\"dinput8.dll\") only supports the {version} version of DOOM (2016). Skipping");
		}
		catch (Exception e) when (e is DirectoryNotFoundException or IOException or SecurityException or UnauthorizedAccessException)
		{
			try
				{File.Delete("./dinput8.dll");} // Clean up any potential incomplete "dinput8.dll" file
			catch (Exception e2) when (e2 is DirectoryNotFoundException or IOException or UnauthorizedAccessException)
				{}
			Console.WriteLine();
			Prompts.WriteError("ERROR: Failed to install mods!");
			Prompts.WriteWarning("Make sure that none of the game files are currently open. Try rebooting your computer and running DOOMModLoader again");
			Prompts.WriteVerbose($"({e.GetType().Name} in LoadDoomLegacyMod > File.WriteAllBytes)");
			Prompts.ExitPrompt();
			return;
		}
	}

	// Uncaps cutscenes in vanilla resources that cap them, letting cutscenes run at more than 60 FPS
	public static void UncapVanillaResources(ResourceArchive container, FileStream destination)
	{
		if (!Config.Final.UncapCutscenes)
			return;

		Console.WriteLine("    [Built-in: Uncap Cutscenes]...");

		// Vanilla entityDefs and .entities files in DOOM (2016), its demo, SnapMap, and DOOM VFR that cap cutscenes
		ReadOnlySpan<string> list = [ // Must be pre-sorted by "StringComparer.Ordinal"
			"generated/decls/entitydef/interact/syncentity/doomsuit_pickup.decl",
			"generated/decls/entitydef/zion/cineractive/maps/intro/elevator/elevator/sync11_elevator_idsync.decl",
			"generated/decls/entitydef/zion/cineractive/maps/resource_ops/satellite_room_b/satellite_roomb/sync11_satellite_roomb_idsync.decl",
			"generated/decls/entitydef/zion/syncanims/cineractive/maps/bloodkeep_c/talisman_guard_intro/sync11_combo_guard_cine_checkpoint_idsync.decl",
			"generated/decls/entitydef/zion/syncanims/cineractive/maps/bloodkeep_c/talisman_guard_intro/sync11_combo_guard_cine_idsync.decl",
			"generated/decls/entitydef/zion/syncanims/cineractive/maps/bloodkeep_c/talisman_guard_intro/sync11_twins_cine_checkpoint_idsync.decl",
			"generated/decls/entitydef/zion/syncanims/cineractive/maps/bloodkeep_c/talisman_guard_intro/sync11_twins_cine_idsync.decl",
			"generated/decls/entitydef/zion/syncanims/cineractive/maps/lazarus_labs/cyberdemon_intro/sync11_cine_idsync.decl",
			"generated/decls/entitydef/zion/syncanims/cineractive/maps/lazarus_labs/cyberdemon_intro/sync11_cine_idsync_checkpoint.decl",
			"generated/decls/entitydef/zion/syncanims/cineractive/maps/lazarus_labs/cyberdemon_res_checkpoint/sync11_cine_idsync.decl",
			"generated/decls/entitydef/zion/syncanims/cineractive/maps/titan/spider_mastermind_intro/sync11_cine_checkpoint_idsync.decl",
			"generated/decls/entitydef/zion/syncanims/cineractive/maps/titan/spider_mastermind_intro/sync11_cine_idsync.decl",
			"generated/decls/entitydef/zion/syncmelee/archvile.decl",
			"generated/decls/entitydef/zion/syncmelee/archvile_coop.decl",
			"generated/decls/entitydef/zion/syncmelee/baron.decl",
			"generated/decls/entitydef/zion/syncmelee/baron_coop.decl",
			"generated/decls/entitydef/zion/syncmelee/cacodemon.decl",
			"generated/decls/entitydef/zion/syncmelee/cacodemon_coop.decl",
			"generated/decls/entitydef/zion/syncmelee/cyberdemon.decl",
			"generated/decls/entitydef/zion/syncmelee/hellified_soldier.decl",
			"generated/decls/entitydef/zion/syncmelee/hellified_soldier_beam.decl",
			"generated/decls/entitydef/zion/syncmelee/hellified_soldier_beam_coop.decl",
			"generated/decls/entitydef/zion/syncmelee/hellified_soldier_coop.decl",
			"generated/decls/entitydef/zion/syncmelee/hellknight.decl",
			"generated/decls/entitydef/zion/syncmelee/hellknight_coop.decl",
			"generated/decls/entitydef/zion/syncmelee/imp.decl",
			"generated/decls/entitydef/zion/syncmelee/imp_coop.decl",
			"generated/decls/entitydef/zion/syncmelee/mancubus.decl",
			"generated/decls/entitydef/zion/syncmelee/mancubus_coop.decl",
			"generated/decls/entitydef/zion/syncmelee/mancubus_cyber.decl",
			"generated/decls/entitydef/zion/syncmelee/mancubus_cyber_coop.decl",
			"generated/decls/entitydef/zion/syncmelee/pinky.decl",
			"generated/decls/entitydef/zion/syncmelee/pinky_coop.decl",
			"generated/decls/entitydef/zion/syncmelee/player_pinky_pvp_chainsaw.decl",
			"generated/decls/entitydef/zion/syncmelee/playerdeath/baron.decl",
			"generated/decls/entitydef/zion/syncmelee/playerdeath/hellknight.decl",
			"generated/decls/entitydef/zion/syncmelee/playerdeath/imp.decl",
			"generated/decls/entitydef/zion/syncmelee/playerdeath/revenant.decl",
			"generated/decls/entitydef/zion/syncmelee/playerdeath/zombie.decl",
			"generated/decls/entitydef/zion/syncmelee/revenant.decl",
			"generated/decls/entitydef/zion/syncmelee/revenant_coop.decl",
			"generated/decls/entitydef/zion/syncmelee/spider_mastermind.decl",
			"generated/decls/entitydef/zion/syncmelee/talismanguard.decl",
			"generated/decls/entitydef/zion/syncmelee/zombie.decl",
			"generated/decls/entitydef/zion/syncmelee/zombie_coop.decl",
			"generated/decls/entitydef/zion/syncmelee/zombie_hell_coop.decl",
			"generated/decls/entitydef/zion/syncmelee/zombie_uac_security_coop.decl",
			"generated/decls/entitydef/zion/syncmelee/zombie_welder_coop.decl",
			"maps/dev/snap_box.entities",
			"maps/game/sp/blood_keep_c/blood_keep_c.entities",
			"maps/game/sp/lazarus_2/lazarus_2.entities",
			"maps/game/sp/resource_ops_foundry/resource_ops_foundry.entities",
			"maps/game/sp/titan/titan.entities",
		];

		foreach (ResourceArchiveEntry entry in container.Entries)
		{
			if (entry.Type is not ("entityDef" or "file")
			|| MemoryExtensions.BinarySearch(list, entry.FullName, StringComparer.Ordinal) <= -1 // Not in the list
			|| HandleMods.Data.ModResources.Contains(entry.FullName)) // Already replaced, thus also already uncapped
				continue;

			// To cap cutscenes, sync entities enable "disableAdaptiveTick" or "disableAdaptiveTickForAllParticipants"
			// The simplest way to avoid this is to just replace that with something unrecognised, like an underscore
			// (This will not affect "idTarget_AdaptiveTickToggle" entities)
			using (Stream source = entry.Open())
			{
				HandleResource.StartData(entry, destination); // "StartData" must happen after "entry.Open()"
				LoadReplaceData(source, destination, entry.Length, "disableAdaptiveTick"u8, "_"u8);
			}
			HandleResource.FinishData(entry, destination);
		}

		Prompts.WriteVerbose("        Uncapped generated/decls/entitydef/*.decl");
		Prompts.WriteVerbose("        Uncapped maps/*.entities");
	}
}
