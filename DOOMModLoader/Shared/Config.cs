using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security;
using System.Text;

// Loads and saves configurable settings, plus command-line overrides for them

namespace DOOMModLoader.Shared;
static class Config
{
	public static class Cli // Command-line arguments
	{
		// Overrides for file settings
		public static bool? CheckForUpdates; // Unlike the file variant, this is only true/false
		public static bool? LaunchGame;
		public static bool? PatchGame;
		public static bool? ShowConflicts;
		public static bool? ShowZipWarnings;
		public static bool? SnapMap;
		public static bool? UncapCutscenes;
		public static bool? Verbose;

		// Command-line-exclusive options
		// "-decrypt", "-encrypt", "-extract", and "-help" are directly in the Main method
		public static bool DryRun = false; // Extract: Print would-be extracted files
		public static List<string> Filters = []; // Extract: File path/name filters
		public static bool Force = false; // Decrypt/Encrypt/Extract: Allow a non-empty destination
		public static string In = ""; // Mod directory, container to extract, or file to decrypt/encrypt
		public static byte[]? Iv = null; // Encrypt: AES IV
		public static string Out = ""; // Decrypt/Encrypt/Extract: Destination file/directory
		public static byte[]? Salt = null; // Encrypt: Salt
		public static List<string> Types = []; // Extract: Type filters
	}

	public static bool ShouldSave = false; // If false, skip saving the settings file

	public static class File // Settings that are saved to a file, and their default values
	{
		public static int? CheckForUpdates;
		public static DateOnly? LastUpdateCheck;
		public static bool? LaunchGame;
		public static bool PatchGame       = true;
		public static bool ShowConflicts   = true;
		public static bool ShowZipWarnings = false;
		public static bool SnapMap         = false;
		public static bool UncapCutscenes  = false;
		public static bool Verbose         = false;

		// Loads the settings file
		public static void Load()
		{
			ShouldSave = true;

			if (!System.IO.File.Exists("./DOOMModLoaderSettings.txt"))
			{
				// If the old settings file name exists, rename it to the new file name
				try
					{System.IO.File.Move("./DOOMModLoader Settings.txt", "./DOOMModLoaderSettings.txt");}
				catch (Exception e) when (e is DirectoryNotFoundException or FileNotFoundException
				or IOException or PathTooLongException or UnauthorizedAccessException)
					{return;} // If the old settings file name doesn't exist either, there's nothing to load
			}

			Dictionary<string, byte[]> parsed;
			try
			{
				using FileStream stream = System.IO.File.OpenRead("./DOOMModLoaderSettings.txt");
				parsed = DeclParser.Parse(stream, (int)stream.Length);
			}
			catch (FileNotFoundException)
				{return;} // The settings file doesn't exist yet, which is fine
			catch (Exception e) when (e is DirectoryNotFoundException or IOException or PathTooLongException or UnauthorizedAccessException)
			{
				Console.WriteLine();
				Prompts.WriteWarning("Warning: Failed to load \"DOOMModLoaderSettings.txt\"");
				return;
			}

			ShouldSave = false;

			byte[]? loadData; // For the LoadHelper* methods
			if (!LoadHelperI("checkForUpdates", ref File.CheckForUpdates, allowNull: true))
				ShouldSave = true;
			if (File.CheckForUpdates >= 1)
				LoadHelperD( "lastUpdateCheck", ref File.LastUpdateCheck);
			if (!LoadHelperN("launchGame",      ref File.LaunchGame, allowNull: true))
				ShouldSave = true;
			if (!LoadHelperB("patchGame",       ref File.PatchGame))
				ShouldSave = true;
			if (!LoadHelperB("showConflicts",   ref File.ShowConflicts))
				ShouldSave = true;
			if (!LoadHelperB("showZipWarnings", ref File.ShowZipWarnings))
				ShouldSave = true;
			if (!LoadHelperB("snapMap",         ref File.SnapMap))
			{
				if (System.IO.File.Exists("./base/snap_gameresources.resources")) // Don't require the SnapMap line for DOOM VFR
					ShouldSave = true;
			}
			if (!LoadHelperB("uncapCutscenes",  ref File.UncapCutscenes))
				ShouldSave = true;
			if (!LoadHelperB("verbose",         ref File.Verbose))
				ShouldSave = true;

			if (ShouldSave && parsed.ContainsKey(".error"))
			{
				// A variable wasn't found ("ShouldSave"). That's normal when updating DOOMModLoader
				// But there was also a syntax error, in which case the error is probably why it wasn't found,
				// so only show a warning when there's an error (and a missing variable)
				Console.WriteLine();
				Prompts.WriteWarning("Warning: Failed to load \"DOOMModLoaderSettings.txt\"");
			}

			// String-to-value shortcuts. Sets a value and returns true if the value was found, false otherwise
			bool LoadHelperB(string name, ref bool result, bool allowNull = false)
			{
				if (!parsed.TryGetValue(name, out loadData))
					return false;
				else if (MemoryExtensions.SequenceEqual(loadData, "true"u8))
				{
					result = true;
					return true;
				}
				else if (MemoryExtensions.SequenceEqual(loadData, "false"u8))
				{
					result = false;
					return true;
				}
				return (allowNull && MemoryExtensions.SequenceEqual(loadData, "NULL"u8));
			}
			bool LoadHelperN(string name, ref bool? result, bool allowNull = false)
			{
				if (!parsed.TryGetValue(name, out loadData))
					return false;
				else if (MemoryExtensions.SequenceEqual(loadData, "true"u8))
				{
					result = true;
					return true;
				}
				else if (MemoryExtensions.SequenceEqual(loadData, "false"u8))
				{
					result = false;
					return true;
				}
				return (allowNull && MemoryExtensions.SequenceEqual(loadData, "NULL"u8));
			}
			bool LoadHelperD(string name, ref DateOnly? result, bool allowNull = false)
			{
				if (!parsed.TryGetValue(name, out loadData))
					return false;
				string text = Encoding.UTF8.GetString(loadData);
#pragma warning disable IDE0018 // "Variable declaration can be inlined"
				DateOnly temp;
#pragma warning restore IDE0018
				if (DateOnly.TryParseExact(text, "\\\"yyyy-MM-dd\\\"", CultureInfo.InvariantCulture, DateTimeStyles.None, out temp))
				{
					result = temp;
					return true;
				}
				return (allowNull && text == "NULL");
			}
			bool LoadHelperI(string name, ref int? result, bool allowNull = false)
			{
				if (!parsed.TryGetValue(name, out loadData))
					return false;
#pragma warning disable IDE0018 // "Variable declaration can be inlined"
				int temp;
#pragma warning restore IDE0018
				if (int.TryParse(loadData, out temp))
				{
					if (temp is >= -1 and <= 30) // Only allow up to a month between update checks
					{
						result = temp;
						return true;
					}
					else
						return false;
				}
				return (allowNull && MemoryExtensions.SequenceEqual(loadData, "NULL"u8));
			}
		}

		// Writes the settings file
		public static void Save()
		{
			if (!ShouldSave)
				return;
			ShouldSave = false; // Set it to false even if the file fails to save

			string text = "{"
			+ $"\n\tcheckForUpdates = {ValueI(File.CheckForUpdates)};"
			+  " //Check for updates before installing mods (1-30: Days between checks / 0: Always / -1: Never)";
			if (File.CheckForUpdates >= 1 && (File.LastUpdateCheck is not null))
			{
				text += $"\n\tlastUpdateCheck = {ValueD(File.LastUpdateCheck)};"
				+  " //The date of the last update check";
			}
			text += $"\n\tlaunchGame = {ValueB(File.LaunchGame)};"
			+  " //Launch the game after installing mods"
			+ $"\n\tpatchGame = {ValueB(File.PatchGame)};"
			+  " //Patch the game to not require developer mode, if possible"
			+ $"\n\tshowConflicts = {ValueB(File.ShowConflicts)};"
			+  " //Show mod file conflicts"
			+ $"\n\tshowZipWarnings = {ValueB(File.ShowZipWarnings)};"
			+  " //Show mod development warnings for zips, not just loose files";
			if (System.IO.File.Exists("./base/snap_gameresources.resources")) // Don't write the SnapMap line for DOOM VFR
			{
				text += $"\n\tsnapMap = {ValueB(File.SnapMap)};"
				+  " //Install mods for SnapMap instead of Campaign/Multiplayer";
			}
			text += $"\n\tuncapCutscenes = {ValueB(File.UncapCutscenes)};"
			+  " //Experimental: Let cutscenes run at more than 60 FPS"
			+ $"\n\tverbose = {ValueB(File.Verbose)};"
			+  " //Display more information while installing mods"
			+  "\n}";

			try
				{System.IO.File.WriteAllText("./DOOMModLoaderSettings.txt", text);}
			catch (Exception e) when (e is DirectoryNotFoundException or IOException or NotSupportedException
			or PathTooLongException or SecurityException or UnauthorizedAccessException)
				{Prompts.WriteWarning("Warning: Failed to save \"DOOMModLoaderSettings.txt\"");}

			// Value-to-string shortcuts
			static string ValueB(bool? x) => (x is null) ? "NULL" : (x == true ? "true" : "false");
			static string ValueD(DateOnly? x) => (x is null) ? "NULL" : $"\"{x.Value.Year}-{x.Value.Month:D2}-{x.Value.Day:D2}\"";
			static string ValueI(int? x) => (x is null) ? "NULL" : x.Value.ToString();
		}
	}

	public static class Final // Command-line arguments, falling back to file settings if not overridden
	{
		public static int? CheckForUpdates
		{
			get
			{
				if (Cli.CheckForUpdates is null)
					return File.CheckForUpdates;
				else if (Cli.CheckForUpdates == true)
					return 0; // Always
				else
					return -1; // Never
			}
		}
		public static bool? LaunchGame     => (Cli.LaunchGame      ?? File.LaunchGame);
		public static bool PatchGame       => (Cli.PatchGame       ?? File.PatchGame);
		public static bool ShowConflicts   => (Cli.ShowConflicts   ?? File.ShowConflicts);
		public static bool ShowZipWarnings => (Cli.ShowZipWarnings ?? File.ShowZipWarnings);
		public static bool SnapMap         => (Cli.SnapMap         ?? File.SnapMap);
		public static bool UncapCutscenes  => (Cli.UncapCutscenes  ?? File.UncapCutscenes);
		public static bool Verbose         => (Cli.Verbose         ?? File.Verbose);
	}
}
