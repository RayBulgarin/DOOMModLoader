using DOOMModLoader.Shared;
using System;
using System.IO;

// Miscellaneous resource-handling methods

namespace DOOMModLoader.LoadMods;
static class HandleResource
{
	// Call before writing a resource's data. Pads and sets the resource's offset
	public static void StartData(ResourceArchiveEntry entry, FileStream destination)
	{
		try
		{
			destination.Position = ((destination.Position + 0xF) & ~0xF); // Pad to start at a 0x10 alignment
			entry.Offset = destination.Position;
		}
		catch (IOException)
		{
			Console.WriteLine();
			Prompts.WriteError("ERROR: Failed to install mods!");
			Prompts.WriteWarning("Make sure that none of the game files are currently open. Try rebooting your computer and running DOOMModLoader again");
			Prompts.WriteVerbose("(IOException in StartData)");
			Prompts.ExitPrompt();
			return;
		}

		// Compressed resources don't start with padding, but we don't compress modified resources anyhow

		// Across DOOM (2016) and DOOM VFR, 4 of 5 data files start with a padded resource (even if it's compressed!),
		// so we will also pad the first custom resource (and all others)
	}

	// Call after writing a resource's data. Sets the resource's length and patch number
	public static void FinishData(ResourceArchiveEntry entry, FileStream destination)
	{
		try
			{entry.Length = (int)(destination.Position - entry.Offset);}
		catch (IOException)
		{
			Console.WriteLine();
			Prompts.WriteError("ERROR: Failed to install mods!");
			Prompts.WriteWarning("Make sure that none of the game files are currently open. Try rebooting your computer and running DOOMModLoader again");
			Prompts.WriteVerbose("(IOException in FinishData)");
			Prompts.ExitPrompt();
			return;
		}
		entry.CompressedLength = entry.Length;
		entry.Patch = (byte)HandleMods.Data.PatchNumber;

		// If an entry's length is zero, then resources with a patch number of zero use an offset of 0x10,
		// while resources with a non-zero patch number (which includes mods) use an offset of 0
		if (entry.Length == 0)
			entry.Offset = 0;
		// Todo: Remove the full name from resources with a length of zero (or completely delete the resource?)
		// Some duplicate resources exist, though, so that on its own would just make the game use the duplicate entry

		// None of DOOM (2016)'s nor DOOM VFR's data files end with padding, so don't pad it until the next resource
		// (If the last mod resource is empty, then "StartData" will still add padding, but that's not problematic)
	}

	// Writes a resource to the destination container data
	static void LoadData(Stream source, ResourceArchiveEntry entry, FileStream destination, int length, bool added)
	{
		if (Config.Final.SnapMap && entry.Type == "material")
		{
			// To avoid SnapMap and non-SnapMap video mods conflicting with each other, use "video/snap_mods/" instead
			// This should also apply to mapInfo decls, had that not been irrelevant for SnapMap
			// (For mapInfo, only "checkpointLoadingVideo"/"loadingVideos"/"loopingLoadingVideo" should be changed)
			HandleMiscellaneous.LoadReplaceData(source, destination, length, "\"video/mods/"u8, "\"video/snap_mods/"u8);
		}
		else if (Config.Final.UncapCutscenes
		&& (entry.Type == "entityDef" || (entry.Type == "file" && Path.GetExtension(entry.FullName) == ".entities")))
		{
			// To cap cutscenes, sync entities enable "disableAdaptiveTick" or "disableAdaptiveTickForAllParticipants"
			// The simplest way to avoid this is to just replace that with something unrecognised, like an underscore
			// (This will not affect "idTarget_AdaptiveTickToggle" entities)
			HandleMiscellaneous.LoadReplaceData(source, destination, length, "disableAdaptiveTick"u8, "_"u8);
		}
		else
		{
			try
				{source.CopyTo(destination);} // Load raw bytes
			catch (IOException)
			{
				Console.WriteLine();
				Prompts.WriteError("ERROR: Failed to install mods!");
				Prompts.WriteWarning("Make sure that none of the game files are currently open. Try rebooting your computer and running DOOMModLoader again");
				Prompts.WriteVerbose("(IOException in LoadData)");
				Prompts.ExitPrompt();
				return;
			}
		}

		Prompts.WriteVerbose($"        {(added ? "   Added" : "Replaced")} {entry.FullName}");
	}

	// Loads a custom file, and determines what to do with it
	public static void LoadThing(Stream source, ResourceArchive container, FileStream destination, string relativePath, long length)
	{
		if (length >= 512*1024*1024)
		{
			// We mostly expect files smaller than 20 megabytes. Videos may fill more,
			// but if any one file fills more than half a gigabyte, that's suspicious
			Console.WriteLine();
			Prompts.WriteError("ERROR: Failed to install mods!");
			Prompts.WriteWarning($"\"{relativePath}\" fills {length / (1024*1024)} MiB, which doesn't seem right");
			Prompts.ExitPrompt();
			return;
		}

		relativePath = relativePath.Replace('\\', '/');

		if (relativePath.ContainsOrdinal(';'))
		{
			HandleWarnings.AddContainsSemicolon();
			relativePath = relativePath[0 .. relativePath.IndexOfOrdinal(';')];
		}
		string upperPath = relativePath;
		if (MemoryExtensions.ContainsAnyInRange(relativePath, 'A', 'Z')) // Only worry about ASCII letters
		{
			if (relativePath.StartsWithOrdinalIgnoreCase("video/")) // Explicitly forbid uppercase video paths
			{
				Console.WriteLine();
				Prompts.WriteError("ERROR: Failed to install mods!");
				Prompts.WriteWarning($"\"{relativePath}\" is forbidden. Custom video filenames must only contain a-z, 0-9, \"_\", \"/\", and a \".bik\" or \".bk2\" extension");
				Prompts.ExitPrompt();
				return;
			}
			HandleWarnings.AddUppercaseName(relativePath);
			if (Path.GetFileNameWithoutExtension(relativePath).EndsWithOrdinal(" (Original)"))
				return; // Don't load DOOMModLoader's own backup files
			relativePath = relativePath.ToLowerInvariant();
		}
		if (HandleMods.Data.ModResources.Contains(relativePath))
		{
			if (Config.Final.Verbose)
				Prompts.WriteWarning($"     Conflicting {relativePath}");
			HandleWarnings.AddModConflicts(relativePath);
			return; // We load from highest-priority to lowest-priority, so don't override the conflict
		}

		// In case of resources with the same path, DOOM (2016) loads the last duplicate, so use "FindLast" here
		ResourceArchiveEntry? entry = container.Entries.FindLast(x => x.FullName == relativePath);

		bool added = false;
		if (entry is null)
		{
			added = true;
			(string Type, string Name)? guess = ResourceType.GetTypeAndShortName(relativePath);
			if (guess is null)
			{
				if (relativePath == "mod.decl")
					HandleModDecl.LoadModDecl(source, (int)length);
				else if (relativePath == "fileids.txt")
					HandleFileIds.LoadFileIds(source, (int)length);
				else if (relativePath.StartsWithOrdinal("video/"))
					HandleMiscellaneous.LoadVideo(source, relativePath, (int)length);
				else if (relativePath == "dinput8.dll")
					HandleMiscellaneous.LoadDoomLegacyMod(source, (int)length);
				else
				{
					if (Config.Final.Verbose)
						Prompts.WriteWarning($"         Skipped {upperPath}");
					HandleWarnings.AddType(relativePath);
				}
				return;
			}
			else if (guess.Value.Type == "binaryFile")
			{
				if (relativePath.StartsWithOrdinal("generated/binaryfile/strings/"))
				{
					Console.WriteLine();
					Prompts.WriteError("ERROR: Failed to install mods!");
					Prompts.WriteWarning($"\"{relativePath}\" is forbidden. Cannot add new languages. Use \"generated/binaryfile/strings/english.bfile\" instead");
					Prompts.ExitPrompt();
					return;
				}
				else // We DID determine the type, but modders probably shouldn't add new CFGs like this
				{
					if (Config.Final.Verbose)
						Prompts.WriteWarning($"         Skipped {upperPath}");
					HandleWarnings.AddType(relativePath);
				}
				return;
			}

			entry = new ResourceArchiveEntry(container)
			{
				Id        = container.Entries.Count,
				Type      = guess.Value.Type,
				ShortName = guess.Value.Name,
				FullName  = relativePath,
			};
			container.Entries.Add(entry);
		}
		else if (entry.Type == "binaryFile") // Matched an existing "binaryFile", for custom strings
		{
			if (entry.FullName.StartsWithOrdinal("generated/binaryfile/strings/"))
				HandleStrings.LoadCustomStrings(source, entry, (int)length);
			else // Don't allow modding binary CFGs directly
			{
				Console.WriteLine();
				Prompts.WriteError("ERROR: Failed to install mods!");
				Prompts.WriteWarning($"\"{relativePath}\" is forbidden. Use \"mod.decl\" > \"autoExec\" instead");
				Prompts.ExitPrompt();
			}
			return;
		}

		StartData(entry, destination);
		LoadData(source, entry, destination, (int)length, added);
		FinishData(entry, destination);
		HandleMods.Data.ModResources.Add(relativePath);
	}
}
