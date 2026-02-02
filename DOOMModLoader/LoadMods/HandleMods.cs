using DOOMModLoader.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security;

// Uninstalls old mods and loads new mods

namespace DOOMModLoader.LoadMods;
static class HandleMods
{
	public static class Data
	{
		public static string ModName = ""; // The zip or loose directory being processed
		public static bool LoadingZip; // Whether currently loading a zipped mod or a loose directory
		public static int PatchNumber = 0; // Which "gameresources_###" container to install mods to - This is set later
		public static List<string> ModFileNames = new(16); // Mod file/directory names - Not a hard limit
		public static List<string> ModResources = new(128); // Files found within mods

		public static string CurrentMod
		{
			get
			{
				if (ModName == "[Loose files]") // Return the name of the "Mods" directory
					return Path.GetFileName(Path.TrimEndingDirectorySeparator(Config.Cli.In));
				else // Return the name of the zip or loose directory being loaded
					return $"{ModName}{(LoadingZip ? ".zip" : "")}";
			}
		}
	}



	// Makes sure that no invalid arguments are used
	public static void ProcessArguments()
	{
		string text = "";
		if (Config.Cli.DryRun) // This doesn't differentiate between "-dry-run" and "-simulate"
			text += "-dry-run, ";
		if (Config.Cli.Filters.Count != 0)
			text += "-filter, ";
		if (Config.Cli.Force)
			text += "-force, ";
		if (Config.Cli.Iv is not null)
			text += "-iv, ";
		if (!string.IsNullOrEmpty(Config.Cli.Out))
			text += "-out, ";
		if (Config.Cli.Salt is not null)
			text += "-salt, ";
		if (Config.Cli.Types.Count != 0)
			text += "-type, ";
		if (!string.IsNullOrEmpty(text))
		{
			Console.WriteLine();
			Prompts.WriteError("ERROR: The following cannot be used without \"-decrypt\", \"-encrypt\", or \"-extract\":");
			Prompts.WriteWarning($"    {text[0 .. ^2]}"); // Trim the last comma and space
			Prompts.ExitPrompt();
			return;
		}
	}

	// Validates that DOOMModLoader is placed in DOOM (2016)'s or DOOM VFR's installation,
	// creates the "Mods" directory if it doesn't exist, and appends a directory separator to custom "Mods" paths
	public static void ValidatePaths()
	{
		if (!File.Exists("./DOOMx64.exe") && !File.Exists("./DOOMVFRx64.exe"))
		{
			Console.WriteLine();
			Prompts.WriteError("ERROR: Couldn't find \"DOOMx64.exe\"!");
			Prompts.WriteWarning("Make sure that DOOMModLoader is placed in your DOOM (2016) installation");
			Prompts.ExitPrompt();
			return;
		}
		if (!File.Exists("./base/gameresources.index.verify"))
		{
			Console.WriteLine();
			Prompts.WriteError("ERROR: Couldn't find \"base/gameresources.index.verify\"!");
			Prompts.WriteWarning("Make sure that DOOMModLoader is placed in your DOOM (2016) installation");
			Prompts.ExitPrompt();
			return;
		}

		bool defaultModsPath = string.IsNullOrEmpty(Config.Cli.In); // Whether to use the default "Mods" path

		if (defaultModsPath)
			Config.Cli.In = "./Mods/";
		else if (!Path.EndsInDirectorySeparator(Config.Cli.In))
			Config.Cli.In += Path.DirectorySeparatorChar; // Make it end with a directory separator

		if (Directory.Exists(Config.Cli.In))
			return;

		// The default changed from "mods" to "Mods", and file systems can be case-sensitive, so check for "mods" too
		if (Directory.Exists(Config.Cli.In.ToLowerInvariant()))
			Config.Cli.In = Config.Cli.In.ToLowerInvariant();
		else if (defaultModsPath) // Only create the default "Mods" directory
		{
			try
				{Directory.CreateDirectory(Config.Cli.In);}
			catch (Exception e) when (e is ArgumentException or DirectoryNotFoundException or IOException
			or NotSupportedException or PathTooLongException or UnauthorizedAccessException)
			{
				Console.WriteLine();
				Console.WriteLine($"Installing {(Config.Final.SnapMap ? "SnapM" : "m")}ods...");
				Console.WriteLine();
				Prompts.WriteError("ERROR: Failed to install mods!");
				Prompts.WriteWarning("Could not access the \"Mods\" directory. Try rebooting your computer and running DOOMModLoader again");
				Prompts.ExitPrompt();
				return;
			}
		}
		else // Custom "Mods" path not found
		{
			Console.WriteLine();
			Console.WriteLine($"Installing {(Config.Final.SnapMap ? "SnapM" : "m")}ods...");
			Console.WriteLine();
			Prompts.WriteError("ERROR: Failed to install mods!");
			Prompts.WriteWarning($"Could not access \"{Utility.HideUserName(Config.Cli.In)}\"");
			Prompts.ExitPrompt();
			return;
		}
	}

	// Uninstalls all currently-installed mods, except DoomLegacyMod ("dinput8.dll"),
	// and sets the "highest found vanilla patch container" number
	public static void UninstallModsAndSetPatchNumber()
	{
		string container = $"{(Config.Final.SnapMap ? "snap_" : "")}gameresources";
		int baseLength = $"./base/{container}_???".Length;
		try
		{
			if (File.Exists($"./base/{container}.patch.verify") || File.Exists($"./base/{container}.pindex.verify"))
				Data.PatchNumber = 1; // A vanilla unnumbered file
			else // Delete custom unnumbered files
			{
				File.Delete($"./base/{container}.patch");
				File.Delete($"./base/{container}.pindex");
			}

			// Delete custom "gameresources_002.[patch/pindex]" mod containers
			foreach (string path in Directory.EnumerateFiles("./base/", $"{container}_???.*"))
			{
				if ((path[baseLength - 3] is not (>= '0' and <= '9'))
				||  (path[baseLength - 2] is not (>= '0' and <= '9'))
				||  (path[baseLength - 1] is not (>= '0' and <= '9')))
					continue; // Not a "gameresources_###" file; don't delete it

				if (!path[baseLength .. ^0].EqualsOrdinalIgnoreCase(".patch") // Not "Path.GetExtension"
				&&  !path[baseLength .. ^0].EqualsOrdinalIgnoreCase(".pindex"))
					continue; // Not a resource container; don't delete it

				if (File.Exists($"{path}.verify")) // A vanilla file
				{
					// Store the "highest found vanilla patch container" number for later
					Data.PatchNumber = Math.Max(int.Parse(path[(baseLength - 3) .. baseLength]), Data.PatchNumber);
				}
				else // Delete custom files
					File.Delete(path);
			}

			// Delete video mod files
			try
				{Directory.Delete($"./base/video/{(Config.Final.SnapMap ? "snap_" : "")}mods/", recursive: true);}
			catch (DirectoryNotFoundException)
				{} // The directory doesn't exist, which is fine
		}
		catch (Exception e) when (e is DirectoryNotFoundException or IOException
		or PathTooLongException or SecurityException or UnauthorizedAccessException)
		{
			Console.WriteLine();
			Prompts.WriteError("ERROR: Failed to uninstall mods!");
			Prompts.WriteWarning("Make sure that none of the game files are currently open. Try rebooting your computer and running DOOMModLoader again");
			Prompts.WriteVerbose($"({e.GetType().Name} in UninstallModsAndSetPatchNumber)");
			Prompts.ExitPrompt();
			return;
		}
	}

	// Returns true if any mods/patches will be installed, false otherwise
	static bool AnyModsOrPatchexExist()
	{
		try
		{
			foreach (string x in Directory.EnumerateFileSystemEntries(Config.Cli.In))
				return true; // Mods found
		}
		catch (Exception e) when (e is DirectoryNotFoundException or IOException
		or PathTooLongException or SecurityException or UnauthorizedAccessException)
		{
			Console.WriteLine();
			Console.WriteLine($"Installing {(Config.Final.SnapMap ? "SnapM" : "m")}ods...");
			Console.WriteLine();
			Prompts.WriteError("ERROR: Failed to install mods!");
			Prompts.WriteWarning($"Could not access the \"{Path.GetFileName(Path.TrimEndingDirectorySeparator(Config.Cli.In))}\" directory. Try rebooting your computer and running DOOMModLoader again");
			Prompts.ExitPrompt();
			return false;
		}

		return Config.Final.UncapCutscenes; // No mods found. Return whether any patch is active
	}

	// Must be called before loading any mods
	static void BeforeAllMods(ResourceArchive container)
	{
		HandleStrings.LoadVanillaStrings(container);
	}

	// Flushes data after each individual mod
	static void AfterEachMod()
	{
		HandleStrings.FlushCustomStrings();
		HandleSemicolon.Choice = null; // Ask separately for each mod
		if (Data.ModName == "[Loose files]")
			Data.ModFileNames.Add("[Loose files]"); // Don't save the "Mods" directory's name
		else
			Data.ModFileNames.Add(Data.CurrentMod);
	}

	// Finalises data after all mods have been installed
	static void AfterAllMods(ResourceArchive container, FileStream destinationData)
	{
		HandleModDecl.SavePackageCfg(container, destinationData);
		HandleStrings.SaveStrings(destinationData);
		HandleMiscellaneous.UncapVanillaResources(container, destinationData);
		HandleFileIds.ApplyFileIds(container);
	}

	// Loads mod files from a loose directory
	static void LoadLoose(ResourceArchive container, FileStream destinationData, bool looseRoot)
	{
		if (looseRoot) // The "Mods" directory as a whole is a mod
		{
			Data.ModName = "[Loose files]";
			int rootLength = Config.Cli.In.Length;

			Console.WriteLine($"    {Data.ModName}...");

			foreach (string x in Directory.EnumerateFiles(Config.Cli.In, "*", SearchOption.AllDirectories))
			{
				string path = x;
				string relativePath = path[rootLength .. ^0];

				if (!MemoryExtensions.ContainsAny(relativePath, '/', '\\')
				&& Path.GetExtension(relativePath).EqualsOrdinalIgnoreCase(".zip"))
					continue; // Don't load a zip archive directly in "Mods" as a raw file

				if (relativePath.ContainsOrdinal(';'))
				{
					path = HandleSemicolon.RemoveSuffix(path, relativePath);
					relativePath = path[rootLength .. ^0];
				}
				using FileStream stream = File.OpenRead(path);
				HandleResource.LoadThing(stream, container, destinationData, relativePath, stream.Length);
			}

			AfterEachMod();
			return;
		}

		// Otherwise, every subdirectory within "Mods" is an individual mod
		Span<string> list = Directory.GetDirectories(Config.Cli.In);
		list.Sort(StringComparer.OrdinalIgnoreCase);
		list.Reverse(); // Load mods from highest-priority to lowest-priority
		foreach (string mod in list)
		{
			Data.ModName = Path.GetFileName(mod);
			int rootLength = (mod.Length + 1); // "+1" because "mod" doesn't end with a directory separator

			Console.WriteLine($"    {Data.ModName}...");

			foreach (string x in Directory.EnumerateFiles(mod, "*", SearchOption.AllDirectories))
			{
				string path = x;
				string relativePath = path[rootLength .. ^0];
				if (relativePath.ContainsOrdinal(';'))
				{
					path = HandleSemicolon.RemoveSuffix(path, relativePath);
					relativePath = path[rootLength .. ^0];
				}
				using FileStream stream = File.OpenRead(path);
				HandleResource.LoadThing(stream, container, destinationData, relativePath, stream.Length);
			}

			AfterEachMod();
		}
	}

	// Loads mod files from a zip archive
	static void LoadZip(string path, ResourceArchive container, FileStream destinationData)
	{
		Data.ModName = Path.GetFileNameWithoutExtension(path);

		Console.WriteLine($"    {Data.ModName}...");

		try
		{
			using ZipArchive zip = ZipFile.OpenRead(path);
			foreach (ZipArchiveEntry zipEntry in zip.Entries)
			{
				if (zipEntry.FullName.EndsWith('/') || zipEntry.FullName.EndsWith('\\'))
					continue; // Skip directory entries
				using Stream stream = zipEntry.Open();
				HandleResource.LoadThing(stream, container, destinationData, zipEntry.FullName, zipEntry.Length);
			}
		}
		catch (Exception e) when (e is InvalidDataException or NotSupportedException)
		{
			Console.WriteLine();
			Prompts.WriteError("ERROR: Failed to install mods!");
			Prompts.WriteWarning($"Failed to read \"{Path.GetFileName(path)}\". Make sure that it's a valid, Deflate-compressed zip archive");
			Prompts.ExitPrompt();
			return;
		}
		catch (Exception e) when (e is DirectoryNotFoundException or FileNotFoundException or IOException or UnauthorizedAccessException)
		{
			Console.WriteLine();
			Prompts.WriteError("ERROR: Failed to install mods!");
			Prompts.WriteWarning("Make sure that none of the game files are currently open. Try rebooting your computer and running DOOMModLoader again");
			Prompts.WriteVerbose($"({e.GetType().Name} in LoadZip)");
			Prompts.ExitPrompt();
			return;
		}

		AfterEachMod();
	}

	// Loads and installs mods. Returns true if any mods/patches were installed, false otherwise
	public static bool InstallMods()
	{
		if (!AnyModsOrPatchexExist())
			return false;

		Console.WriteLine();
		Console.WriteLine($"Installing {(Config.Final.SnapMap ? "SnapM" : "m")}ods...");

		// Install mods into the first available "gameresources_###" container
		// 0 is "gameresources.[resources/index]", 1 is "gameresources.[patch/pindex]",
		// 2+ is "gameresources_###.[patch/pindex]" with leading zeroes
		string inContainer = $"{(Config.Final.SnapMap ? "snap_" : "")}gameresources";
		if (Data.PatchNumber == 0)
			inContainer = $"./base/{inContainer}.index";
		else if (Data.PatchNumber == 1)
			inContainer = $"./base/{inContainer}.pindex";
		else if (Data.PatchNumber < 255) // "<", not "<="
			inContainer = $"./base/{inContainer}_{Data.PatchNumber:D3}.pindex";
		else
		{
			Console.WriteLine();
			Prompts.WriteError("ERROR: Failed to install mods!");
			Prompts.WriteWarning($"\"{inContainer}_{Data.PatchNumber:D3}.pindex\" must be less than 255");
			Prompts.ExitPrompt();
			return false;
		}
		Data.PatchNumber += 1;
		string outContainer = $"./base/{(Config.Final.SnapMap ? "snap_" : "")}gameresources";
		if (Data.PatchNumber == 1)
			outContainer += ".pindex";
		else
			outContainer += $"_{Data.PatchNumber:D3}.pindex";

		bool looseRoot = false; // Whether the "Mods" directory as a whole or each of its subdirectories is a mod
		if (Directory.Exists(Path.Join(Config.Cli.In, "generated"))
		||  Directory.Exists(Path.Join(Config.Cli.In, "maps"))
		||  Directory.Exists(Path.Join(Config.Cli.In, "video")) // For DOOMModLoader's custom video support
		||  Directory.Exists(Path.Join(Config.Cli.In, "cooked"))
		||  Directory.Exists(Path.Join(Config.Cli.In, "decls"))
		||  Directory.Exists(Path.Join(Config.Cli.In, "env"))
		||  Directory.Exists(Path.Join(Config.Cli.In, "fga"))
		||  Directory.Exists(Path.Join(Config.Cli.In, "fonts"))
		||  Directory.Exists(Path.Join(Config.Cli.In, "md6"))
		||  Directory.Exists(Path.Join(Config.Cli.In, "snapmap_offline"))
		||  Directory.Exists(Path.Join(Config.Cli.In, "submission"))
		||  Directory.Exists(Path.Join(Config.Cli.In, "textures")))
			looseRoot = true; // If any of DOOM (2016)'s, SnapMap's, or DOOM VFR's directories exist, "Mods" is a mod

		Utility.Assert(
			!File.Exists(outContainer) && !File.Exists(Path.ChangeExtension(outContainer, ".patch")),
			$"InstallMods: \"{Path.GetFileName(outContainer)}\" exists"
		);

		try
		{
			using ResourceArchive container = new(inContainer);
			using FileStream destinationData = File.Create(Path.ChangeExtension(outContainer, ".patch"));

			destinationData.Write([0x05, (byte)'S', (byte)'E', (byte)'R']); // Reverse "RESources v5" magic

			// Find mod zips
			List<string> modZips = [];
			foreach (string path in Directory.EnumerateFiles(Config.Cli.In))
			{
				if (Path.GetExtension(path).EqualsOrdinalIgnoreCase(".zip"))
					modZips.Add(path);
				else // If a non-zip file is found in the root "Mods" directory, then "Mods" itself is a mod
					looseRoot = true;
			}

			// Load mods from highest-priority to lowest-priority
			BeforeAllMods(container);

			Data.LoadingZip = false;
			LoadLoose(container, destinationData, looseRoot);

			Data.LoadingZip = true;
			modZips.Sort(StringComparer.OrdinalIgnoreCase);
			modZips.Reverse();
			foreach (string path in modZips)
				LoadZip(path, container, destinationData);

			AfterAllMods(container, destinationData);
			container.Save(outContainer);

			HandleWarnings.ShowAll(); // Display mod warnings

			return true; // Mods found and installed
		}
		catch (Exception e) when (e is DirectoryNotFoundException or IOException or SecurityException or UnauthorizedAccessException)
		{
			Console.WriteLine();
			Prompts.WriteError("ERROR: Failed to install mods!");
			Prompts.WriteWarning("Make sure that none of the game files are currently open. Try rebooting your computer and running DOOMModLoader again");
			Prompts.WriteVerbose($"({e.GetType().Name} in InstallMods)");
			Prompts.ExitPrompt();
			return false;
		}
	}
}
