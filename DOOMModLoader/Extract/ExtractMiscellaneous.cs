using DOOMModLoader.Shared;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Security;

// Miscellaneous resource extraction methods

namespace DOOMModLoader.Extract;
static class ExtractMiscellaneous
{
	// Makes sure that no invalid arguments are used, and sets a default output path
	public static void ProcessArguments()
	{
		// Validate arguments
		string text = "";
		if (Config.Cli.CheckForUpdates == true)
			text += "-checkforupdates, ";
		if (Config.Cli.Iv is not null)
			text += "-iv, ";
		if (Config.Cli.LaunchGame == true)
			text += "-launchgame, ";
		if (Config.Cli.PatchGame == true)
			text += "-patchgame, ";
		if (Config.Cli.Salt is not null)
			text += "-salt, ";
		if (Config.Cli.ShowConflicts == true)
			text += "-showconflicts, ";
		if (Config.Cli.ShowZipWarnings == true)
			text += "-showzipwarnings, ";
		if (Config.Cli.UncapCutscenes == true)
			text += "-uncapcutscenes, ";
		if (!string.IsNullOrEmpty(text))
		{
			Console.WriteLine();
			Prompts.WriteError("ERROR: The following cannot be used with \"-extract\":");
			Prompts.WriteWarning($"    {text[0 .. ^2]}"); // Trim the last comma and space
			Prompts.ExitPrompt();
			return;
		}
		if (Config.Cli.Force && string.IsNullOrEmpty(Config.Cli.Out))
		{
			Console.WriteLine();
			Prompts.WriteError("ERROR: Failed to extract resources!");
			Prompts.WriteWarning("\"-force\" requires \"-out\"!");
			Prompts.ExitPrompt();
			return;
		}
		if (Config.Cli.SnapMap == true && !string.IsNullOrEmpty(Config.Cli.In))
		{
			// This doesn't differentiate between "-snap" and "-snapmap"
			Console.WriteLine();
			Prompts.WriteError("ERROR: Failed to extract resources!");
			Prompts.WriteWarning("\"-snapmap\" cannot be used with \"-in\"!");
			Prompts.ExitPrompt();
			return;
		}

		// If the user just pastes the full executable path into a random terminal to extract resources,
		// the working directory won't be DOOM (2016)'s installation. Default to the program directory in this case
		// But first, convert any relative "-in" and "-out" paths to absolute using the current working directory
		if (!string.IsNullOrEmpty(Config.Cli.In))
		{
			try
				{Config.Cli.In = Path.GetFullPath(Config.Cli.In);}
			catch (Exception e) when (e is ArgumentException or PathTooLongException or SecurityException)
			{
				Console.WriteLine();
				Prompts.WriteError("ERROR: Failed to extract resources!");
				Prompts.WriteWarning($"\"{Utility.HideUserName(Config.Cli.In)}\" doesn't exist");
				Prompts.ExitPrompt();
				return;
			}
		}
		if (!string.IsNullOrEmpty(Config.Cli.Out))
		{
			try
				{Config.Cli.Out = Path.GetFullPath($"{Config.Cli.Out}/");} // Make it end with a directory separator
			catch (Exception e) when (e is ArgumentException or PathTooLongException or SecurityException)
			{
				Console.WriteLine();
				Prompts.WriteError("ERROR: Failed to extract resources!");
				Prompts.WriteWarning($"Could not access \"{Utility.HideUserName(Config.Cli.Out)}\"");
				Prompts.ExitPrompt();
				return;
			}
		}

		if (!File.Exists("./base/gameresources.index")) // Change the working directory. ".index", not ".pindex"
		{
			string? processDir = Path.GetDirectoryName(Environment.ProcessPath);
			if (!string.IsNullOrEmpty(processDir))
			{
				try
					{Directory.SetCurrentDirectory(processDir);}
				catch (Exception e) when (e is ArgumentNullException or DirectoryNotFoundException
				or FileNotFoundException or IOException or SecurityException)
					{} // Failed to set the working directory, which is fine as we'll just abort later
			}
		}

		// Finalise arguments
		if (string.IsNullOrEmpty(Config.Cli.In)) // If "-in" isn't specified, default to the latest vanilla container
		{
			string fileName = $"{(Config.Final.SnapMap ? "snap_" : "")}gameresources";
			int baseLength = ($"./base/{fileName}_???".Length);
			int highest = -1;

			try
			{
				foreach (string path in Directory.EnumerateFiles("./base/", $"{fileName}_???.pindex.verify"))
				{
					if ((path[baseLength - 3] is not (>= '0' and <= '9'))
					||  (path[baseLength - 2] is not (>= '0' and <= '9'))
					||  (path[baseLength - 1] is not (>= '0' and <= '9')))
						continue; // Not a "gameresources_###" file; don't use it

					highest = Math.Max(int.Parse(path[(baseLength - 3) .. baseLength]), highest);
				}
			}
			catch (Exception e) when (e is DirectoryNotFoundException or IOException
			or PathTooLongException or SecurityException or UnauthorizedAccessException)
			{
				Console.WriteLine();
				Prompts.WriteError("ERROR: Failed to extract resources!");
				Prompts.WriteWarning("Could not access the \"base\" directory");
				Prompts.ExitPrompt();
				return;
			}

			try
			{
				if (highest != -1)
					Config.Cli.In = Path.GetFullPath($"./base/{fileName}_{highest:D3}.pindex");
				else if ((File.Exists($"./base/{fileName}.pindex") && File.Exists($"./base/{fileName}.pindex.verify"))
				|| !(File.Exists($"./base/{fileName}.index") && File.Exists($"./base/{fileName}.index.verify")))
					Config.Cli.In = Path.GetFullPath($"./base/{fileName}.pindex");
				else // DOOM VFR doesn't have a .pindex container. Default to ".index" if ONLY the .index file exists
					Config.Cli.In = Path.GetFullPath($"./base/{fileName}.index");
			}
			catch (Exception e) when (e is ArgumentException or PathTooLongException or SecurityException)
			{
				Console.WriteLine();
				Prompts.WriteError("ERROR: Failed to extract resources!");
				Prompts.WriteWarning($"\"{Utility.HideUserName(Config.Cli.In)}\" doesn't exist");
				Prompts.ExitPrompt();
				return;
			}
		}

		if (string.IsNullOrEmpty(Config.Cli.Out)) // If "-out" isn't specified, default to the container's file name
		{
			try
				{Config.Cli.Out = Path.GetFullPath($"./{Path.GetFileNameWithoutExtension(Config.Cli.In)}/");}
			catch (Exception e) when (e is ArgumentException or PathTooLongException or SecurityException)
			{
				Console.WriteLine();
				Prompts.WriteError("ERROR: Failed to extract resources!");
				Prompts.WriteWarning($"Could not access \"{Utility.HideUserName(Config.Cli.Out)}\"");
				Prompts.ExitPrompt();
				return;
			}
		}
	}

	// Attempts to validate the input and output paths' formats and file existence
	public static void ValidatePaths()
	{
		if (!File.Exists(Config.Cli.In)) // Make sure that the input container exists
		{
			Console.WriteLine();
			Prompts.WriteError("ERROR: Failed to extract resources!");
			Prompts.WriteWarning($"\"{Utility.HideUserName(Config.Cli.In)}\" doesn't exist");
			Prompts.ExitPrompt();
			return;
		}

		try // Make sure that the output directory is non-existent or empty
		{
			foreach (string x in Directory.EnumerateFileSystemEntries(Config.Cli.Out))
			{
				if (Config.Cli.Force)
					break; // Even if we just break, we should call "EnumerateFileSystemEntries" to check for exceptions

				Console.WriteLine();
				Prompts.WriteError("ERROR: Failed to extract resources!");
				Prompts.WriteWarning($"\"{Utility.HideUserName(Config.Cli.Out)}\" already exists");
				Prompts.WriteWarning("If you want to overwrite existing files, use \"-force\"");
				Prompts.ExitPrompt();
				return;
			}
		}
		catch (DirectoryNotFoundException)
			{} // If the directory is just missing, that's fine. If it's invalid, we'll find out later
		catch (IOException)
		{
			Console.WriteLine();
			Prompts.WriteError("ERROR: Failed to extract resources!");
			Prompts.WriteWarning($"\"{Utility.HideUserName(Path.TrimEndingDirectorySeparator(Config.Cli.Out))}\" is a file, not a directory");
			Prompts.ExitPrompt();
			return;
		}
		catch (Exception e) when (e is PathTooLongException or SecurityException or UnauthorizedAccessException)
		{
			Console.WriteLine();
			Prompts.WriteError("ERROR: Failed to extract resources!");
			Prompts.WriteWarning($"Could not access \"{Utility.HideUserName(Config.Cli.Out)}\"");
			Prompts.ExitPrompt();
			return;
		}
	}

	// Creates the output directory, and optionally marks it for NTFS compression on Windows
	public static void CreateOutputDirectory(ExtractQuestionnaire.Answers answers)
	{
		try
			{Directory.CreateDirectory(Config.Cli.Out);}
		catch (Exception e) when (e is ArgumentException or DirectoryNotFoundException or IOException
		or NotSupportedException or PathTooLongException or UnauthorizedAccessException)
			{} // Let the "!Directory.Exists" block handle the error
		if (!Directory.Exists(Config.Cli.Out))
		{
			Console.WriteLine();
			Prompts.WriteError("ERROR: Failed to extract resources!");
			Prompts.WriteWarning($"Could not access \"{Utility.HideUserName(Config.Cli.Out)}\"");
			Prompts.ExitPrompt();
			return;
		}

		// Mark the directory for NTFS compression on Windows, if the user wants it
		if (!answers.NtfsCompression || !OperatingSystem.IsWindows())
			return;

		// The path shouldn't start with "/" on Windows, but disallow accidental arguments for "compact.exe" anyway
		if (!Config.Cli.Out.StartsWith('/'))
		{
			ProcessStartInfo info = new()
			{
				// Windows' built-in NTFS compression tool, without P/Invoke and Windows Management Infrastructure
				FileName = Environment.ExpandEnvironmentVariables("%SystemRoot%\\System32\\compact.exe"),
				ArgumentList = {
					"/C", // Compress a file, or mark a directory to compress its future files/subdirectories
					Config.Cli.Out, // The path to compress; a directory in this case
				},
				RedirectStandardError = true, // Hide the output, by redirecting it to a never-used stream
				RedirectStandardOutput = true,
			};
			try
			{
				using Process? proc = Process.Start(info);
				if (proc is not null && !proc.WaitForExit(8000)) // Wait for up to 8 seconds. If it's still alive, something is wrong
					proc.Kill(true);
				// There's no point in looking at the exit code, as it can be 0 even when it failed
			}
			catch (Exception e) when (e is AggregateException or Win32Exception)
				{} // Failed to launch "compact.exe", which is fine, or to kill it, which we can't do anything about
		}

		try
		{
			if (File.GetAttributes(Config.Cli.Out).HasFlag(FileAttributes.Compressed))
				return; // It's successfully marked for compression
		}
		catch (Exception e) when (e is DirectoryNotFoundException or FileNotFoundException
		or IOException or NotSupportedException or PathTooLongException or UnauthorizedAccessException)
			{}

		// It's not marked for compression
		Console.WriteLine();
		Prompts.WriteWarning("Warning: Failed to apply NTFS compression on the output directory");

		answers.NtfsCompression = false;
		string size = ExtractQuestionnaire.ExpectedSize(answers);
		if (size.StartsWith('?'))
			Prompts.WriteWarning("Extract resources anyway?");
		else
			Prompts.WriteWarning($"Extract resources anyway? (Expected size on disk: {size})");
		Console.Write(
			""
			+ "\n(Press [Y] to begin extracting resources)"
			+ "\n(Press [N] to abort)"
		);
		bool? choice = Prompts.GetYesOrNo();

		if (choice is null)
		{
			Console.WriteLine();
			Prompts.WriteError("ERROR: Failed to detect keystroke!");
			Prompts.ExitPrompt();
			return;
		}
		else if (choice == false)
		{
			// We've already created the output directory, so delete it if it's empty
			// We COULD recursively delete empty parent directories too...
			// But if we do that, then a typo here could wipe out a whole file system, which is not worth it
			try
				{Directory.Delete(Config.Cli.Out, recursive: false);}
			catch (Exception e) when (e is DirectoryNotFoundException or IOException or UnauthorizedAccessException)
				{}
			Environment.Exit(0);
			return;
		}
	}
}
