using DOOMModLoader.Shared;
using System;
using System.IO;
using System.Security;

// Asks questions prior to extracting resources

namespace DOOMModLoader.Extract;
static class ExtractQuestionnaire
{
	public class Answers
	{
		public required bool ExtractUseful;
		public required bool NtfsCompression;
		public required bool ConvertCrLf;

		public long ContainerLength; // Not a question, but we'll still store it here
	}



	// If the user specified a .index file, but a .pindex file exists, asks to correct it
	public static void CheckForPindex()
	{
		if (Path.GetExtension(Config.Cli.In).EqualsOrdinalIgnoreCase(".pindex"))
			return;

		string patch = Path.ChangeExtension(Config.Cli.In, ".pindex");
		if (!File.Exists(patch) || !File.Exists($"{patch}.verify"))
			return;

		Console.Write(
			""
			+ $"\nWarning: \"{Path.GetFileName(Config.Cli.In)}\" contains outdated resources"
			+ $"\nDo you want to extract \"{Path.GetFileName(patch)}\" instead?"
			+  "\n"
			+ $"\n(Press [Y] to use \"{Path.GetFileName(patch)}\" - Recommended)"
			+ $"\n(Press [N] to use \"{Path.GetFileName(Config.Cli.In)}\")"
		);
		bool? choice = Prompts.GetYesOrNo();

		if (choice is null)
			Prompts.WriteWarning("Warning: Failed to detect keystroke"); // Don't correct it
		else if (choice == true)
			Config.Cli.In = patch;

		Console.WriteLine();
	}

	// Returns a hardcoded expected size on disk for extracted resources
	// The returned string starts with "?" when the expected size is unknown
	public static string ExpectedSize(Answers answers)
	{
		/* Extracted sizes as of 2025, from a Windows+NTFS machine with a cluster size of 4096 bytes
		"Convert CRLF newlines to LF" reduces file size, but not enough to matter. These sizes are without CRLF-to-LF
		"File size" is the length of each file. "Compressed" and "uncompressed" are the size on disk

		Useful resources:    |   File size    |   Compressed   |  Uncompressed  |
		DOOM (2016)  .index: |    363,786,157 |    177,029,120 |    394,133,504 |
		DOOM (2016) .pindex: |    363,786,229 |    177,029,120 |    394,137,600 |
		SnapMap      .index: |    117,824,311 |     74,186,752 |    141,266,944 |
		SnapMap     .pindex: |    117,824,383 |     74,186,752 |    141,266,944 |
		Demo         .index: |     89,622,461 |     61,079,552 |    101,298,176 |
		DOOM VFR     .index: |    123,084,899 |     60,211,200 |    134,717,440 |

		All resources:       |   File size    |   Compressed   |  Uncompressed  |
		DOOM (2016)  .index: | 17,083,898,656 | 12,046,168,064 | 17,213,353,984 |
		DOOM (2016) .pindex: | 17,083,944,977 | 12,046,057,472 | 17,213,415,424 |
		SnapMap      .index: |  5,018,903,195 |  3,206,987,776 |  5,120,528,384 |
		SnapMap     .pindex: |  5,018,946,582 |  3,206,672,384 |  5,120,598,016 |
		Demo         .index: |  1,822,616,903 |  1,188,798,464 |  1,868,046,336 |
		DOOM VFR     .index: |  3,497,355,924 |  2,339,643,392 |  3,555,573,760 | */

		if (Config.Cli.DryRun) // If we don't extract anything, it won't fill anything
			return "0 B";

		if (Config.Cli.Filters.Count != 0 || Config.Cli.Types.Count != 0)
			return "??? (Unknown due to custom filters)"; // Must start with "?"

		switch (answers.ContainerLength) // A crude, fast way to tell which container is being extracted
		{
			case 0xE9CA03: // DOOM (2016) "gameresources.index"
			case 0xE9CA68: // DOOM (2016) "gameresources.pindex"
				if (answers.ExtractUseful)
					return (answers.NtfsCompression ? "~0.2 GiB" : "~0.4 GiB");
				else
					return (answers.NtfsCompression ? "~11.3 GiB" : "~16.1 GiB");
			case 0xE296D4: // DOOM (2016) "snap_gameresources.index"
			case 0xE29739: // DOOM (2016) "snap_gameresources.pindex"
				if (answers.ExtractUseful)
					return (answers.NtfsCompression ? "~0.1 GiB" : "~0.2 GiB");
				else
					return (answers.NtfsCompression ? "~3.0 GiB" : "~4.8 GiB");
			case 0x6A0D62: // DOOM (2016) Demo "gameresources.index"
				if (answers.ExtractUseful)
					return (answers.NtfsCompression ? "~0.1 GiB" : "~0.2 GiB");
				else
					return (answers.NtfsCompression ? "~1.2 GiB" : "~1.8 GiB");
			case 0x6E0FFA: // DOOM VFR "gameresources.index"
				if (answers.ExtractUseful)
					return (answers.NtfsCompression ? "~0.1 GiB" : "~0.2 GiB");
				else
					return (answers.NtfsCompression ? "~2.2 GiB" : "~3.4 GiB");
			default:
				return "??? (Unrecognised game version)"; // Must start with "?"
		}
	}

	// Displays the current extraction settings
	static void ShowSettings(Answers answers)
	{
		Console.Write( // Not "WriteLine", because we want to "WriteSuccess" or "WriteError" on the same line
			"Container to extract:"
			+ $"\n    {Utility.HideUserName(Config.Cli.In)}"
			+  "\nOutput directory:"
			+ $"\n    {Utility.HideUserName(Config.Cli.Out)}"
			+  "\nResources to extract: "
		);
		if (Config.Cli.Filters.Count != 0 || Config.Cli.Types.Count != 0)
			Prompts.WriteWarning("Custom filter");
		else if (answers.ExtractUseful)
			Prompts.WriteSuccess("Useful only (Recommended)");
		else
			Prompts.WriteError("All");

		if (OperatingSystem.IsWindows())
		{
			Console.Write("Use NTFS compression: ");
			if (answers.NtfsCompression)
				Prompts.WriteSuccess("Yes (Recommended)");
			else
				Prompts.WriteError("No");
		}

		Console.Write("Convert CRLF (\\r\\n) newlines to LF (\\n): ");
		if (answers.ConvertCrLf)
			Prompts.WriteSuccess("Yes (Recommended)");
		else
			Prompts.WriteError("No");

		Console.WriteLine($"Expected size on disk: {ExpectedSize(answers)}");

		if (Config.Cli.DryRun)
			Prompts.WriteWarning("Dry-run without extracting resources: Yes");
	}

	// Asks the user how they'd like to extract resources
	public static Answers AskQuestions()
	{
		Answers answers = new()
		{
			ExtractUseful = true,
			NtfsCompression = OperatingSystem.IsWindows(), // The way that this is handled only applies to Windows
			ConvertCrLf = true,
		};
		try
			{answers.ContainerLength = new FileInfo(Config.Cli.In).Length;}
		catch (Exception e) when (e is NotSupportedException or PathTooLongException or SecurityException or UnauthorizedAccessException)
			{answers.ContainerLength = -1;}

		Console.WriteLine(
			""
			+ "\nResource extraction:"
			+ "\n"
		);
		ShowSettings(answers);
		Console.Write(
			""
			+  "\nUse the recommended extraction settings?"
			+  "\n"
			+ $"\n(Press [Y] to begin {(Config.Cli.DryRun ? "dry-runn" : "extract")}ing resources)"
			+  "\n(Press [N] to customise settings)"
		);
		bool? choice = Prompts.GetYesOrNo();

		if (choice is null)
		{
			Console.WriteLine();
			Prompts.WriteError("ERROR: Failed to detect keystroke!");
			Prompts.ExitPrompt();
			return null;
		}
		else if (choice == true)
			return answers; // The user is satisfied with the recommended settings

		// Otherwise, ask how the user wants to customise the settings
		string sizeYes;
		string sizeNo;

		if (Config.Cli.Filters.Count == 0 && Config.Cli.Types.Count == 0) // Resources to extract?
		{
			sizeYes = ExpectedSize(answers);
			sizeNo = "";
			if (sizeYes.StartsWith('?'))
				sizeYes = ""; // If the expected size is unknown, don't show it
			else
			{
				answers.ExtractUseful = false;
				sizeYes = $" - {sizeYes}";
				sizeNo = $" - {ExpectedSize(answers)}";
			}
			Console.Write(
				""
				+  "\n"
				+  "\nExtract generally useful/commonly-modified resources only?"
				+  "\n    This includes all decls, string files, and map .entities files"
				+  "\n    Skipping other resources reduces the amount of files and size on disk"
				+  "\n    (This can also be customised with the \"-filter\" and \"-type\" command-line arguments)"
				+  "\n"
				+ $"\n(Press [Y] to extract useful resources only - Recommended{sizeYes})"
				+ $"\n(Press [N] to extract all resources{sizeNo})"
			);
			choice = Prompts.GetYesOrNo();

			if (choice is null)
			{
				Console.WriteLine();
				Prompts.WriteError("ERROR: Failed to detect keystroke!");
				Prompts.ExitPrompt();
				return null;
			}
			answers.ExtractUseful = (bool)choice;
		}

		if (OperatingSystem.IsWindows()) // Use NTFS compression?
		{
			sizeYes = ExpectedSize(answers);
			sizeNo = "";
			if (sizeYes.StartsWith('?'))
				sizeYes = ""; // If the expected size is unknown, don't show it
			else
			{
				answers.NtfsCompression = false;
				sizeYes = $" - {sizeYes}";
				sizeNo = $" - {ExpectedSize(answers)}";
			}
			Console.Write(
				""
				+  "\n"
				+  "\nUse file system-level compression on the output directory?"
				+  "\n    This transparently, losslessly reduces the size on disk without affecting how you can use the extracted files"
				+  "\n    This does not create a zip-like archive; all files are still loose, and can be opened directly by any program"
				+  "\n    (This only works for NTFS. Other file systems may also support compression, but DOOMModLoader doesn't know how to handle that for you)"
				+  "\n"
				+ $"\n(Press [Y] to apply NTFS compression - Recommended{sizeYes})"
				+ $"\n(Press [N] to not apply it{sizeNo})"
			);
			choice = Prompts.GetYesOrNo();

			if (choice is null)
			{
				Console.WriteLine();
				Prompts.WriteError("ERROR: Failed to detect keystroke!");
				Prompts.ExitPrompt();
				return null;
			}
			answers.NtfsCompression = (bool)choice;
		}

		sizeYes = ExpectedSize(answers); // Convert CRLF newlines to LF?
		if (sizeYes.StartsWith('?'))
			sizeYes = ""; // If the expected size is unknown, don't show it
		else
			sizeYes = $" - {sizeYes}";
		sizeNo = sizeYes; // CRLF-to-LF doesn't affect the size enough to matter here
		Console.Write(
			""
			+  "\n"
			+  "\nConvert CRLF (\\r\\n) newlines to LF (\\n)?"
			+  "\n    This makes it easier to search for multi-line patterns in decls, as newlines can be matched with \"\\n\" instead of \"\\r\\n\""
			+  "\n    This makes decls and string files consistent with map .entities files"
			+  "\n    (Also removes the byte order mark from the start of string files for consistency)"
			+  "\n"
			+ $"\n(Press [Y] to convert CRLF to LF - Recommended{sizeYes})"
			+ $"\n(Press [N] to leave newlines as they are{sizeNo})"
		);
		choice = Prompts.GetYesOrNo();

		if (choice is null)
		{
			Console.WriteLine();
			Prompts.WriteError("ERROR: Failed to detect keystroke!");
			Prompts.ExitPrompt();
			return null;
		}
		answers.ConvertCrLf = (bool)choice;

		// Final confirmation
		Console.WriteLine();
		Console.WriteLine();
		ShowSettings(answers);
		Console.Write(
			""
			+  "\nUse these extraction settings?"
			+  "\n"
			+ $"\n(Press [Y] to begin {(Config.Cli.DryRun ? "dry-runn" : "extract")}ing resources)"
			+  "\n(Press [N] to abort)"
		);
		choice = Prompts.GetYesOrNo();

		if (choice is null)
		{
			Console.WriteLine();
			Prompts.WriteError("ERROR: Failed to detect keystroke!");
			Prompts.ExitPrompt();
			return null;
		}
		else if (choice == false)
		{
			Environment.Exit(0);
			return null;
		}

		return answers;
	}
}
