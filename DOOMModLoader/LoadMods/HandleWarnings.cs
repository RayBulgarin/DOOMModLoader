using DOOMModLoader.Shared;
using System;
using System.Collections.Generic;
using System.IO;

// Stores and displays mod-loading warnings

namespace DOOMModLoader.LoadMods;
static class HandleWarnings
{
	static readonly SortedSet<string> containsSemicolon = new(StringComparer.OrdinalIgnoreCase);
	static readonly SortedDictionary<string, SortedSet<string>> fileIdsMissingResources = [];
	static readonly SortedSet<string> modConflicts      = new(StringComparer.Ordinal); // Don't ignore case
	static readonly SortedDictionary<string, SortedSet<string>> types = [];
	static readonly SortedDictionary<string, SortedSet<string>> uppercaseNames = [];
	static readonly SortedSet<string> vanillaStrings    = new(StringComparer.OrdinalIgnoreCase);



	// A file name has a semicolon plus resource type suffix
	public static void AddContainsSemicolon()
	{
		if (Config.Final.ShowZipWarnings || !HandleMods.Data.LoadingZip)
			containsSemicolon.Add(HandleMods.Data.CurrentMod);
	}

	// "fileids.txt" specifies an unfound resource
	public static void AddFileIdsMissingResources(string mod, string name)
	{
		if (Config.Final.ShowZipWarnings || !Path.GetExtension(name).EqualsOrdinalIgnoreCase(".zip"))
		{
			fileIdsMissingResources.TryAdd(mod, new SortedSet<string>(StringComparer.Ordinal)); // Don't worry about case
			fileIdsMissingResources[mod].Add(name);
		}
	}

	// Multiple mods contain the same file
	public static void AddModConflicts(string name)
	{
		if (Config.Final.ShowConflicts)
			modConflicts.Add(name);
	}

	// A file name's type/short name couldn't be determined
	public static void AddType(string name)
	{
		if (Config.Final.ShowZipWarnings || !HandleMods.Data.LoadingZip)
		{
			types.TryAdd(HandleMods.Data.CurrentMod, new SortedSet<string>(StringComparer.Ordinal)); // Don't worry about case
			types[HandleMods.Data.CurrentMod].Add(name);
		}
	}

	// A file name isn't lowercase
	public static void AddUppercaseName(string name)
	{
		if (Config.Final.ShowZipWarnings || !HandleMods.Data.LoadingZip)
		{
			uppercaseNames.TryAdd(HandleMods.Data.CurrentMod, new SortedSet<string>(StringComparer.Ordinal)); // Don't ignore case
			uppercaseNames[HandleMods.Data.CurrentMod].Add(name);
		}
	}

	// A string file contains vanilla strings
	public static void AddVanillaStrings()
	{
		if (Config.Final.ShowZipWarnings || !HandleMods.Data.LoadingZip)
			vanillaStrings.Add(HandleMods.Data.CurrentMod);
	}

	// Shows all stored warnings, mostly in order of increasing importance
	public static void ShowAll()
	{
		if (fileIdsMissingResources.Count != 0)
		{
			Console.WriteLine();
			Prompts.WriteWarning(); // Switch to yellow text
			Console.WriteLine("Warning: The following mods contain \"fileids.txt\" lines that specify nonexistent files:");
			ShowMap(fileIdsMissingResources);
			Console.ResetColor();
		}

		if (modConflicts.Count != 0)
		{
			Console.WriteLine();
			Prompts.WriteWarning(); // Switch to yellow text
			Console.WriteLine("Warning: The following conflicting files were found in multiple mods:");
			foreach (string path in modConflicts)
				Console.WriteLine($"    {path}");
			Console.ResetColor();
		}

		if (containsSemicolon.Count != 0)
		{
			Console.WriteLine();
			Prompts.WriteWarning(); // Switch to yellow text
			Console.WriteLine("Warning: The following mods contain files with a type suffix (\";\"), which is superfluous:");
			foreach (string mod in containsSemicolon)
				Console.WriteLine($"    {mod}");
			Console.ResetColor();
		}

		if (uppercaseNames.Count != 0)
		{
			Console.WriteLine();
			Prompts.WriteWarning(); // Switch to yellow text
			Console.WriteLine("Warning: The following mods contain files with uppercase file names:");
			ShowMap(uppercaseNames);
			Console.ResetColor();
		}

		if (types.Count != 0)
		{
			Console.WriteLine();
			Prompts.WriteWarning(); // Switch to yellow text
			Console.WriteLine("Warning: Failed to determine the types for files in the following mods (did you place the file incorrectly?):");
			ShowMap(types);
			Console.ResetColor();
		}

		if (vanillaStrings.Count != 0)
		{
			Console.WriteLine();
			Prompts.WriteWarning(); // Switch to yellow text
			Console.WriteLine("Warning: The following mods contain unchanged vanilla strings, which is superfluous:");
			foreach (string mod in vanillaStrings)
				Console.WriteLine($"    {mod}");
			Console.ResetColor();
		}

		if (Config.Final.UncapCutscenes)
		{
			Console.WriteLine();
			Prompts.WriteWarning("Warning: \"uncapCutscenes\" is enabled. This is experimental, and will cause problems");
		}

		static void ShowMap(SortedDictionary<string, SortedSet<string>> map)
		{
			const int maxPerMod = 6; // For brevity, only show up to 6 files per mod (or 5 plus an ellipsis if more)

			foreach (KeyValuePair<string, SortedSet<string>> mod in map)
			{
				Console.WriteLine($"    {mod.Key}:");
				int i = 0;
				foreach (string path in mod.Value)
				{
					Console.WriteLine($"        {path}");
					i++;
					if (i >= (maxPerMod - 1) && mod.Value.Count > maxPerMod)
					{
						Console.WriteLine("        ...");
						break;
					}
				}
			}
		}
	}
}
