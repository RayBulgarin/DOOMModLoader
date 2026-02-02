using DOOMModLoader.Extract;
using DOOMModLoader.LoadMods;
using DOOMModLoader.Shared;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

// The Main method/entry point, help message, command-line parsing, program flow, and assembly attributes

[assembly: AssemblyFileVersion(DOOMModLoader.Program.VersionString)]
[assembly: AssemblyTitle("DOOM (2016)/DOOM VFR mod loader")]

namespace DOOMModLoader;
static class Program
{
	internal const string VersionString = "0.5"; // This should match the GitHub release tag, without the "v" prefix



	// Displays the help message
	static void PrintUsage(string[] args)
	{
		string executable = "./DOOMModLoader";
		if (OperatingSystem.IsWindows())
			executable = "DOOMModLoader.exe";

		bool modeCrypt = false;
		bool modeExtract = false;
		foreach (string x in args) // Look for "-decrypt", "-encrypt", and "-extract"
		{
			// Support "-", "--", and "/" prefixes, and ignore case
			string arg = x.ReplaceOrdinal("--", "-").Replace('/', '-').ToLowerInvariant();
			switch (arg)
			{
				case "-decrypt":
				case "-encrypt":
					modeCrypt = true;
					break;
				case "-extract":
					modeExtract = true;
					break;
			}
		}

		Console.WriteLine();
		Console.WriteLine($"Usage: Load mods: {executable} [options]");
		if (!modeCrypt && !modeExtract)
		{
			Console.WriteLine(
				"\"Options\" can be any of the following:"
				+ "\n    -[no]checkforupdates - [Don't] Check for updates before installing mods"
				+ "\n    -[no]launchgame      - [Don't] Launch the game after installing mods"
				+ "\n    -moddir <path>       - Load mods from a given directory instead of \"Mods\""
				+ "\n    -[no]patchgame       - [Don't] Patch the game to not require developer mode, if possible"
				+ "\n    -[no]showconflicts   - [Don't] Show mod file conflicts"
				+ "\n    -[no]showzipwarnings - [Don't] Show mod development warnings for zips, not just loose files"
				+ "\n    -[no]snapmap         - [Don't] Install mods for SnapMap instead of Campaign/Multiplayer"
				+ "\n    -[no]uncapcutscenes  - [Don't] Let cutscenes run at more than 60 FPS"
				+ "\n    -[no]verbose         - [Don't] Display more information while installing mods"
				+ "\nAll options besides \"-moddir\" can also be set in \"DOOMModLoaderSettings.txt\""
			);
		}
		else
			Console.WriteLine($"    Show options: {executable} -help");

		Console.WriteLine();
		Console.WriteLine($"Extract game resources: {executable} -extract [options]");
		if (modeExtract && !modeCrypt)
		{
			Console.WriteLine(
				"You'll be prompted for confirmation before any files are extracted"
				+ "\n\"Options\" can be any of the following:"
				+ "\n    -dry-run          - Print would-be extracted files without actually extracting them"
				+ "\n    -filter <filters> - Filter by file names, e.g. -filter \"maps/*.entities\" \"generated/decls/entitydef/*\""
				+ "\n    -force            - Extract resources even if the output directory already exists. Requires \"-out\""
				+ "\n    -in <file>        - Extract resources from a given file instead of \"base/gameresources.pindex\""
				+ "\n    -out <path>       - Extract resources to the given directory instead of \"gameresources\""
				+ "\n    -snapmap          - Extract resources for SnapMap instead of Campaign/Multiplayer. Incompatible with \"-in\""
				+ "\n    -type <types>     - Filter by types, e.g. -type perkGroups weapon"
				+ "\n    -verbose          - Display more information while extracting resources"
			);
		}
		else
		{
			Console.WriteLine("    You'll be prompted for confirmation before any files are extracted");
			Console.WriteLine($"    Show options: {executable} -help -extract");
		}

		Console.WriteLine();
		Console.WriteLine($"Decrypt/Encrypt binary file: {executable} -decrypt|-encrypt <file> <key> [options]");
		if (modeCrypt && !modeExtract)
		{
			Console.WriteLine(
				$"Example: {executable} -decrypt \"./english.bfile\" \"strings/english.lang\""
				+ "\n\"Options\" can be any of the following:"
				+ "\n    -force              - Overwrite an existing file. Optional if \"-out\" isn't set"
				+ "\n    -iv <hexadecimal>   - When encrypting, set 16 specific bytes for the AES IV. Defaults to zero for a deterministic output"
				+ "\n    -out <file>         - Save the decrypted/encrypted file to the given path instead of \"<file>.dec\" or \"<file>.bfile\""
				+ "\n    -salt <hexadecimal> - When encrypting, set 12 specific bytes for the salt. Defaults to zero for a deterministic output"
				+ "\n    -verbose            - Display more information about the decrypted/encrypted file"
				+ "\nDOOMModLoader extracts and loads decrypted files, so you don't need to do this manually for DOOM (2016)"
			);
		}
		else
		{
			Console.WriteLine($"    Example: {executable} -decrypt \"./english.bfile\" \"strings/english.lang\"");
			Console.WriteLine($"    Show options: {executable} -help -decrypt|-encrypt");
		}

		Console.WriteLine();
		Console.WriteLine($"Display this text: {executable} -help");
	}

	// The entry point. Parses command-line arguments and runs the applicable program mode
	static int Main(string[] args)
	{
		Console.WriteLine($"    DOOMModLoader v{VersionString} by Zwip-Zwap Zapony and PowerBall253, originally by infogram");
		Console.WriteLine("      https://github.com/ZwipZwapZapony/DOOMModLoader");

		// In case of unhandled exceptions, wait for a keystroke before terminating
		AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(UnhandledExceptionHandler);

		// Parse command-line arguments
		bool argDecrypt = false;
		bool argEncrypt = false;
		bool argExtract = false;
		string argKey = ""; // Decryption/Encryption key
		SearchValues<char> hexChars = SearchValues.Create("0123456789ABCDEFabcdef"); // For "-iv"/"-salt"
		for (int i = 0; i < args.Length; i++)
		{
			// Support "-", "--", and "/" prefixes, and ignore case
			string arg = args[i].ReplaceOrdinal("--", "-").Replace('/', '-').ToLowerInvariant();
			switch (arg)
			{
				case "-checkforupdates":
				case "-nocheckforupdates":
					Config.Cli.CheckForUpdates = (arg[1] != 'n');
					break;
				case "-decrypt":
				case "-encrypt":
					if ((i + 2) >= args.Length || string.IsNullOrEmpty(args[i+1]) || string.IsNullOrEmpty(args[i+2])
					|| args[i+1].StartsWith('-')) // Warn about "-" for the file (but not key), but allow "/"
					{
						if ((i + 1) < args.Length && args[i+1].EqualsOrdinalIgnoreCase("help"))
						{
							PrintUsage(args);
							Prompts.ExitPrompt(exitCode: 0);
							return 0;
						}
						Console.WriteLine();
						Prompts.WriteError($"ERROR: \"{arg}\" requires a path and key!");
						if ((i + 1) < args.Length && Path.Exists(args[i+1]))
							Prompts.WriteWarning($"If \"{args[i+1]}\" is a relative path, try \"./{args[i+1]}\" instead");
						Prompts.ExitPrompt();
						return 1;
					}
					if (arg[1] == 'd')
						argDecrypt = true;
					else
						argEncrypt = true;
					Config.Cli.In = args[++i];
					argKey = args[++i]; // We will allow "-" at the start of the decryption/encryption key
					break;
				case "-dry-run":
				case "-simulate":
					Config.Cli.DryRun = true;
					break;
				case "-extract":
					argExtract = true;
					break;
				case "-filter":
				case "-type":
					List<string> list = ((arg == "-filter") ? Config.Cli.Filters : Config.Cli.Types);
					while (i + 1 < args.Length) // Look for upcoming filters
					{
						if (args[i+1][0] == '-') // Stop when we reach another "-" (but not "/") option
							break;
						else
							list.Add(args[++i].ToLowerInvariant()); // Case-insensitive
					}
					if (list.Count == 0)
					{
						Console.WriteLine();
						Prompts.WriteError($"ERROR: \"{arg}\" requires a set of {((arg == "-filter") ? "filter" : "type")}s!");
						if (i + 1 != args.Length) // If "-filter" isn't the final option, then the filter was skipped due to "-"
							Prompts.WriteWarning($"{((arg == "-filter") ? "Filter" : "Type")}s cannot start with \"-\"");
						Prompts.ExitPrompt();
						return 1;
					}
					break;
				case "-force":
					Config.Cli.Force = true;
					break;
				case "-?":
				case "-h":
				case "-help":
					PrintUsage(args);
					Prompts.ExitPrompt(exitCode: 0);
					return 0;
				case "-in":
				case "-moddir":
				case "-out":
					if ((i + 1) >= args.Length || string.IsNullOrEmpty(args[i+1]))
					{
						Console.WriteLine();
						Prompts.WriteError($"ERROR: \"{arg}\" requires a path!");
						Prompts.ExitPrompt();
						return 1;
					}
					else if (args[i+1].StartsWith('-')) // Warn about "-", but allow "/" for paths
					{
						Console.WriteLine();
						Prompts.WriteError($"ERROR: \"{arg}\" requires a path!");
						if (Path.Exists(args[i+1]) || arg == "-out")
							Prompts.WriteWarning($"If \"{args[i+1]}\" is a relative path, try \"./{args[i+1]}\" instead");
						Prompts.ExitPrompt();
						return 1;
					}
					if (arg == "-out")
						Config.Cli.Out = args[++i];
					else // Both "-in" and "-moddir"
						Config.Cli.In = args[++i];
					break;
				case "-iv":
					if ((i + 1) >= args.Length || args[i+1].Length != 2*0x10
					|| MemoryExtensions.ContainsAnyExcept(args[i+1], hexChars))
					{
						Console.WriteLine();
						Prompts.WriteError("ERROR: \"-iv\" requires 16 hexadecimal bytes without spaces!");
						Prompts.WriteWarning("Example: -iv 79B80D3FC448CA091ACD361C66F013A7");
						Prompts.ExitPrompt();
						return 1;
					}
					Config.Cli.Iv = Convert.FromHexString(args[++i]);
					break;
				case "-launchgame":
				case "-nolaunchgame":
					Config.Cli.LaunchGame = (arg[1] != 'n');
					break;
				// "-moddir" is handled alongside "-in"
				// "-out" is handled alongside "-in"
				case "-patchgame":
				case "-nopatchgame":
					Config.Cli.PatchGame = (arg[1] != 'n');
					break;
				case "-salt":
					if ((i + 1) >= args.Length || args[i+1].Length != 2*0xC
					|| MemoryExtensions.ContainsAnyExcept(args[i+1], hexChars))
					{
						Console.WriteLine();
						Prompts.WriteError("ERROR: \"-salt\" requires 12 hexadecimal bytes without spaces!");
						Prompts.WriteWarning("Example: -salt 5A5E829EEE78F86EE610BB09");
						Prompts.ExitPrompt();
						return 1;
					}
					Config.Cli.Salt = Convert.FromHexString(args[++i]);
					break;
				case "-showconflicts":
				case "-noshowconflicts":
					Config.Cli.ShowConflicts = (arg[1] != 'n');
					break;
				case "-showzipwarnings":
				case "-noshowzipwarnings":
					Config.Cli.ShowZipWarnings = (arg[1] != 'n');
					break;
				case "-snap":
				case "-snapmap":
				case "-nosnap":
				case "-nosnapmap":
					Config.Cli.SnapMap = (arg[1] != 'n');
					break;
				// "-type" is handled alongside "-filter"
				case "-uncapcutscenes":
				case "-nouncapcutscenes":
					Config.Cli.UncapCutscenes = (arg[1] != 'n');
					break;
				case "-verbose":
				case "-noverbose":
					Config.Cli.Verbose = (arg[1] != 'n');
					break;
				default:
					Console.WriteLine();
					Prompts.WriteError($"ERROR: Unknown option \"{arg}\"!");
					Prompts.ExitPrompt();
					return 1;
			}
		}

		// Run the relevant program mode
		if ((argDecrypt && argEncrypt) || (argDecrypt && argExtract) || (argEncrypt && argExtract))
		{
			Console.WriteLine();
			Prompts.WriteError("ERROR: You can only use one of \"-decrypt\", \"-encrypt\", and \"-extract\" at a time!");
			Prompts.ExitPrompt();
			return 1;
		}
		else if (argDecrypt || argEncrypt)
			MainCrypt(argEncrypt, argKey);
		else if (argExtract)
			MainExtract();
		else
			MainLoadMods();

		return 0;
	}

	// Called by default, unless "-decrypt", "-encrypt", or "-extract" are used
	static void MainLoadMods()
	{
		HandleMods.ProcessArguments();
		HandleMods.ValidatePaths();
		Config.File.Load();
		UpdateCheck.AskToCheck();
		UpdateCheck.CheckForUpdates();
		HandleMods.UninstallModsAndSetPatchNumber();
		PatchGame.CheckAndPatchGame();
		bool hasMods = HandleMods.InstallMods();
		LaunchGame.AskToLaunch(hasMods);
		LaunchGame.LaunchAndExit(hasMods);
	}

	// Called by "-extract" to extract game resources
	static void MainExtract()
	{
		ExtractMiscellaneous.ProcessArguments();
		ExtractMiscellaneous.ValidatePaths();
		ExtractQuestionnaire.CheckForPindex();
		// Open the container now, so that the user won't have to answer questions if they'd just get an error later
		using ResourceArchive container = new(Config.Cli.In);
		ExtractQuestionnaire.Answers answers = ExtractQuestionnaire.AskQuestions();
		ExtractContainer.Extract(container, answers);

		Prompts.ExitPrompt(exitCode: 0);
		return;
	}

	// Called by "-decrypt" and "-encrypt" to decrypt/encrypt a file on disk
	static void MainCrypt(bool encrypt, string key)
	{
		DecryptEncryptCli.ProcessArguments(encrypt);
		if (encrypt)
			DecryptEncryptCli.EncryptFile(key);
		else
			DecryptEncryptCli.DecryptFile(key);

		Prompts.ExitPrompt(exitCode: 0);
		return;
	}

	// In case of unhandled exceptions, wait for a keystroke before terminating
	static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
	{
		Console.WriteLine();
		Prompts.WriteError("ERROR: Unhandled exception!");
		Prompts.WriteWarning(e.ExceptionObject.ToString());
		try
			{Prompts.ExitPrompt();}
		catch
			{}
		Environment.Exit(1);
		return;
	}
}
