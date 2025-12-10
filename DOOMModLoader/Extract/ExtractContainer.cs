using DOOMModLoader.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Enumeration;
using System.Text;

// Extracts all game resources

namespace DOOMModLoader.Extract;
static class ExtractContainer
{
	// Sets up the default file name filters, or prepares custom filters for use
	static void SetUpFilters(bool extractUseful)
	{
		// If custom filters are not set, use default file name filters
		if (extractUseful && Config.Cli.Filters.Count == 0 && Config.Cli.Types.Count == 0)
		{
			Config.Cli.Filters = [
				"generated/decls/*",
				"generated/binaryfile/strings/*",
				"*.entities",
			];
		}
		else // If custom filters are set, process them
		{
			for (int i = 0; i < Config.Cli.Filters.Count; i++)
			{
				// Add asterisks around wildcardless filters, to make them work as partial matches
				// Convert backslashes to forward slashes
				// Also make the filters lowercase, so that we don't need to perform repeated case-insensitive matches
				// (All vanilla resources in DOOM (2016), SnapMap, and DOOM VFR are lowercase)
				if (Config.Cli.Filters[i].ContainsOrdinal('*'))
					Config.Cli.Filters[i] = Config.Cli.Filters[i].ToLowerInvariant().Replace('\\', '/');
				else
					Config.Cli.Filters[i] = $"*{Config.Cli.Filters[i].ToLowerInvariant().Replace('\\', '/')}*";
			}
		}
	}

	// Returns true if the resource passes the name and type filters
	static bool FilterResource(ResourceArchiveEntry entry)
	{
		// Type filter
		if (Config.Cli.Types.Count != 0 && !Config.Cli.Types.ContainsOrdinalIgnoreCase(entry.Type))
			return false;

		// File name filter
		foreach (string filter in Config.Cli.Filters)
		{
			if (FileSystemName.MatchesSimpleExpression(filter, entry.FullName, ignoreCase: false))
				return true;
		}
		if (Config.Cli.Filters.Count == 0)
			return true;

		return false;
	}

	// Shows where files were extracted to, and asks to open the output directory in a file manager
	static void AskToOpenOutput()
	{
		Console.Write(
			""
			+  "\nThe extracted files can be found here:"
			+ $"\n    {Utility.HideUserName(Config.Cli.Out)}"
			+  "\n"
			+  "\nOpen this directory now?"
			+  "\n"
			+  "\n(Press [Y] to open the above path)"
			+  "\n(Press [N] to exit)"
		);
		bool? choice = Prompts.GetYesOrNo();

		if (choice is null)
		{
			Prompts.WriteWarning("Warning: Failed to detect keystroke");
			return;
		}
		else if (choice == false)
		{
			Environment.Exit(0); // Exit immediately if the user pressed N
			return;
		}

		ProcessStartInfo info;
		if (OperatingSystem.IsWindows())
		{
			info = new ProcessStartInfo()
			{
				FileName = "explorer.exe",
				ArgumentList = { // "ArgumentList" automatically escapes special characters for us, unlike "Arguments"
					Config.Cli.Out,
				},
				UseShellExecute = true,
			};
			// The path shouldn't start with "/" on Windows, but disallow accidental arguments for "explorer.exe" anyway
			if (Config.Cli.Out.StartsWith('/'))
			{
				Prompts.WriteWarning("Warning: Failed to open the output directory!");
				return;
			}
		}
		else
		{
			info = new ProcessStartInfo()
			{
				FileName = Config.Cli.Out, // On Linux, "UseShellExecute" automatically uses "xdg-open" for us
				UseShellExecute = true,
			};
			Utility.Assert( // The path SHOULD start with "/" on Linux, as we always convert it to an absolute path
				Config.Cli.Out.StartsWith('/') && Config.Cli.Out.EndsWith('/'),
				$"Extract: \"{Utility.HideUserName(Config.Cli.Out)}\" isn't an absolute path"
			);
		}

		try
		{
			Process.Start(info);
			Console.WriteLine("Opened the output directory!");
		}
		catch (Win32Exception) // "Win32Exception" is multi-platform, used when the file doesn't exist
			{Prompts.WriteWarning("Warning: Failed to open the output directory!");}
	}

	// Extracts game resources to loose files, and generates a "fileids.txt" file
	public static void Extract(ResourceArchive container, ExtractQuestionnaire.Answers answers)
	{
		ExtractMiscellaneous.CreateOutputDirectory(answers);

		Console.WriteLine();
		Console.WriteLine($"{(Config.Cli.Simulate ? "Simula" : "Extrac")}ting resources...");

		SetUpFilters(answers.ExtractUseful);

		// Determine how many/which resources are to be extracted
		int emptyCount = 0;
		List<string> fileIds = new(88636); // DOOM (2016) has 88636 non-empty resources by default
		List<ResourceArchiveEntry> matchedResources = new(88636); // (SnapMap has 82127, DOOM VFR has 40651)
		foreach (ResourceArchiveEntry entry in container.Entries)
		{
			if (entry.Length == 0)
				emptyCount++;
			else
			{
				fileIds.Add($"{entry.FullName}={entry.Id}\n");
				if (FilterResource(entry))
					matchedResources.Add(entry);
			}
		}

		if (matchedResources.Count == 0) // Stop early if no resources were matched
		{
			bool plural = ((Config.Cli.Filters.Count + Config.Cli.Types.Count) != 1);
			Console.WriteLine();
			Prompts.WriteWarning($"Warning: No resources matched the custom filter{(plural ? "s" : "")}");
			Prompts.ExitPrompt(exitCode: 0);
			return;
		}

		// Start extracting files
		const int perLoop = 16;
		int maxDigits = (((int)Math.Log10(matchedResources.Count)) + 1);
		bool isDemo = (answers.ContainerLength == 0x6A0D62); // DOOM (2016)'s demo's "gameresources.index" length

		if (Config.Cli.Verbose == true) // Not "Config.Final"
			Prompts.WriteVerbose(); // Switch to cyan text if verbosity is enabled

		for (int outerI = 0; outerI < matchedResources.Count; outerI += perLoop) // Increment by "perLoop", not one
		{
			// Console throughput may be a bottleneck here, so batch and write multiple lines at once
			// We have to do this before extracting files, in case an exception would abort before writing the buffer
			string text = "";
			for (int innerI = outerI; innerI < (outerI + perLoop) && innerI < matchedResources.Count; innerI++)
			{
				ResourceArchiveEntry entry = matchedResources[innerI];
				text += $"{(innerI + 1).ToString().PadLeft(maxDigits)}/{matchedResources.Count}: {entry.FullName}\n";
				if (Config.Cli.Verbose == true) // Not "Config.Final"
				{
					text += $"    ID: {entry.Id}"
					+ $"\n    Type: \"{entry.Type}\""
					+ $"\n    Name: \"{entry.ShortName}\""
					+ $"\n    Size: {entry.Length}"
					+ $"\n    Compressed: {((entry.Length == entry.CompressedLength) ? "No" : $"Yes ({100 - ((entry.CompressedLength * 100) / entry.Length)}%)")}"
					+ $"\n    Patch index: {entry.Patch}"
					+ "\n\n"; // One line break after the patch index, one blank line between verbose entries
				}

				if (innerI == 0 || innerI == perLoop-1) // Only show a single line for the first loop,
					break; // and reset to a multiple of "perLoop" afterwards
			}
			Console.Write(text); // Not "WriteLine", as it already has "\n" at the end

			if (!Config.Cli.Simulate)
			{
				for (int innerI = outerI; innerI < (outerI + perLoop) && innerI < matchedResources.Count; innerI++)
				{
					// Todo: Multi-thread this?
					if (!ExtractResource.WriteFile(matchedResources[innerI], answers.ConvertCrLf, isDemo))
					{
						Console.WriteLine();
						Prompts.WriteError("ERROR: Failed to extract resources!");
						Prompts.ExitPrompt();
						return;
					}

					if (innerI == 0 || innerI == perLoop-1) // Only extract a single resource for the first loop,
					{ // and reset to a multiple of "perLoop" afterwards
						if (outerI == 0)
							outerI = (1 - perLoop); // It will become 1 after the outer loop increments it
						else
							outerI = 0; // It will become "perLoop" after the outer loop increments it
						break;
					}
				}
			}
		}

		if (!Config.Cli.Simulate) // Create "fileids.txt"
		{
			fileIds.Sort(StringComparer.Ordinal);
			fileIds[^1] = fileIds[^1].TrimEnd('\n'); // Remove the last newline
			try
			{
				// "File.WriteAllLines" has \r\n newlines on Windows, but we only want \n, so we'll use a custom writer
				using FileStream stream = File.Create(Path.Join(Config.Cli.Out, "fileids.txt"));
				foreach (string x in fileIds)
					stream.Write(Encoding.UTF8.GetBytes(x));
			}
			catch (Exception e) when (e is DirectoryNotFoundException
			or IOException or PathTooLongException or UnauthorizedAccessException)
			{
				// "fileids.txt" is not necessary, so this is a warning, not an error
				Prompts.WriteWarning("Warning: Failed to generate \"fileids.txt\"");
			}
		}

		Console.WriteLine();
		Prompts.WriteSuccess($"Successfully {(Config.Cli.Simulate ? "simula" : "extrac")}ted {matchedResources.Count} resources!");
		if (Config.Cli.Verbose == true) // Not "Config.Final"
			Prompts.WriteVerbose($"Skipped {emptyCount} empty resources");
		int count = (fileIds.Count - matchedResources.Count);
		Console.WriteLine($"Skipped {count} filtered resource{((count == 1) ? "" : "s")}");

		if (!Config.Cli.Simulate)
			AskToOpenOutput();
	}
}
