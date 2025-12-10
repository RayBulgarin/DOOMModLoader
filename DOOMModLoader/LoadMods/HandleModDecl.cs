using DOOMModLoader.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

// Parses "mod.decl" files

namespace DOOMModLoader.LoadMods;
static class HandleModDecl
{
	static readonly List<byte[]> autoExecLines = [];



	// Takes a piece of console text, unescapes it, and stores it if it doesn't use blacklisted commands/variables
	static void ParseAutoExec(byte[] bytes)
	{
		if (bytes.Length < 2 || bytes[0] != '"' || bytes[1] == '"') // Require a non-empty string
		{
			Console.WriteLine();
			Prompts.WriteError("ERROR: Failed to install mods!");
			Prompts.WriteWarning("\"mod.decl\": \"autoExec\" must be a string");
			Prompts.ExitPrompt();
			return;
		}

		if (MemoryExtensions.Contains(bytes, (byte)'\\'))
		{
			// Unescape tabs, quotes, and backslashes, but not newlines
			int unescaped = 0;
			for (int i = 1; i < (bytes.Length - 2); i++)
			{
				if (bytes[i] != '\\')
				{
					bytes[i-unescaped] = bytes[i];
					continue;
				}

				// Unescape an escape sequence
				i++;
				unescaped++;
				switch (bytes[i])
				{
					case (byte)'t':
						bytes[i-unescaped] = (byte)'\t';
						break;
					case (byte)'"':
						bytes[i-unescaped] = (byte)'"';
						break;
					case (byte)'\\':
						bytes[i-unescaped] = (byte)'\\';
						break;
					default: // Unknown escape sequence
						Console.WriteLine();
						Prompts.WriteError("ERROR: Failed to install mods!");
						Prompts.WriteWarning($"\"mod.decl\" > \"autoExec\": \"\\{(char)bytes[i]}\" is forbidden");
						Prompts.ExitPrompt();
						return;
				}
			}
			bytes[^(unescaped + 2)] = bytes[^2];
			bytes = bytes[1 .. ^(unescaped + 1)]; // Set to the new length, and trim the wrapping quotes
		}
		else
			bytes = bytes[1 .. ^1]; // If there are no escape sequences, just trim the wrapping quotes

		string blacklisted = AutoExecBlacklist.ValidateAutoExec(bytes);
		if (string.IsNullOrEmpty(blacklisted))
			autoExecLines.Add(bytes);
		else
		{
			Console.WriteLine();
			Prompts.WriteError("ERROR: Failed to install mods!");
			Prompts.WriteWarning($"\"mod.decl\" > \"autoExec\": \"{blacklisted}\" is forbidden");
			Prompts.ExitPrompt();
			return;
		}
	}

	// Loads a mod's "mod.decl" file
	public static void LoadModDecl(Stream source, int length)
	{
		Dictionary<string, byte[]> parsed = DeclParser.Parse(source, length);

		if (parsed.ContainsKey(".error"))
		{
			Console.WriteLine();
			Prompts.WriteError("ERROR: Failed to install mods!");
			Prompts.WriteWarning($"Failed to parse \"mod.decl\"");
			Prompts.ExitPrompt();
			return;
		}

#pragma warning disable IDE0018 // "Variable declaration can be inlined"
		byte[]? temp;
#pragma warning restore IDE0018
		if (!parsed.TryGetValue("modLoaderVersion", out temp) || temp[0] != '"')
		{
			Console.WriteLine();
			Prompts.WriteError("ERROR: Failed to install mods!");
			Prompts.WriteWarning("\"mod.decl\": \"modLoaderVersion\" must be \"0.4\" or greater, as a string");
			Prompts.ExitPrompt();
			return;
		}
		string version = Encoding.UTF8.GetString(temp[1 .. ^1]); // Trim the wrapping quotes
		if (Utility.CompareVersion(version, "0.4") < 0)
		{
			Console.WriteLine();
			Prompts.WriteError("ERROR: Failed to install mods!");
			Prompts.WriteWarning("\"mod.decl\": \"modLoaderVersion\" must be \"0.4\" or greater, as a string");
			Prompts.ExitPrompt();
			return;
		}

		if (Utility.CompareVersion(version, Program.VersionString) > 0)
		{
			Console.WriteLine();
			Prompts.WriteError("ERROR: Failed to install mods!");
			Prompts.WriteWarning($"\"{HandleMods.Data.CurrentMod}\" requires v{parsed["modLoaderVersion"]} of DOOMModLoader, but you only have v{Program.VersionString}");
			UpdateCheck.AskToOpenDownloadPage(true);
			Environment.Exit(1); // The user chose not to open the download page
			return;
		}

		if (parsed.TryGetValue("autoExec", out temp))
			ParseAutoExec(temp);

		foreach (string x in parsed.Keys) // Check for unrecognised variables in the file
		{
			if (x is not ("autoExec" or "modLoaderVersion"))
			{
				Console.WriteLine();
				Prompts.WriteError("ERROR: Failed to install mods!");
				Prompts.WriteWarning($"\"mod.decl\": Unknown variable name \"{x}\"");
				Prompts.ExitPrompt();
				return;
			}
		}

		Prompts.WriteVerbose("          Parsed mod.decl");
	}

	// Saves custom console command/variables, and adds the installed mods to the start of the in-game console log
	public static void SavePackageCfg(ResourceArchive container, FileStream destination)
	{
		ResourceArchiveEntry? entry = container.Entries.FindLast(x => x.FullName == "generated/binaryfile/package.bfile");
		if (entry is null)
			goto Failure;

		IdCrypt.Decrypted? dec;
		using (Stream stream = entry.Open())
		{
			Span<byte> bytes = new byte[entry.Length]; // Should be less than a kilobyte
			try
				{stream.ReadExactly(bytes);}
			catch (Exception e) when (e is InvalidDataException or IOException)
				{goto Failure;}
			dec = IdCrypt.Decrypt(bytes, entry.ShortName);
		}
		if (dec is null)
			goto Failure;

		// Echo DOOMModLoader's version and installed mods near the start of the console log
		// The first "\n" is a line break after the original CFG, the second "\n" is a blank line before the mod info
		string echoText = $"\n\necho\necho \"^2DOOMModLoader v{Program.VersionString} - {HandleMods.Data.ModFileNames.Count} mod{(HandleMods.Data.ModFileNames.Count == 1 ? "" : "s")} ";
		if (Config.Final.UncapCutscenes)
			echoText += "+ ^51 patch ^2";
		echoText += "loaded:\"\n";
		foreach (string name in HandleMods.Data.ModFileNames)
		{
			string temp = name.Replace('\0', '?').Replace('\n', '?').Replace('\r', '?')
			.Replace('"', '\'').Replace('\\', '/').Replace('^', '?');
			echoText += $"echo \"^2- {temp}\"\n";
		}
		if (Config.Final.UncapCutscenes)
			echoText += $"echo \"^5- [Built-in: Uncap Cutscenes]\"\n";
		echoText += "echo"; // No "\n" at the end here

		// Handle mods with "autoExec"
		ReadOnlySpan<byte> exec = [];
		if (autoExecLines.Count != 0)
		{
			exec = [(byte)'\n']; // Insert a blank line before custom commands/variables
			autoExecLines.Reverse(); // Make higher-priority commands/variables come after lower-priority ones
			foreach (byte[] x in autoExecLines) // Append each "autoExec" line one by one
			{
				exec = (byte[])[
					.. exec,
					(byte)'\n',
					.. x,
				];
			}
		}

		dec.Data = [
			.. dec.Data,
			.. Encoding.UTF8.GetBytes(echoText),
			.. exec,
		];

		HandleResource.StartData(entry, destination);
		destination.Write(IdCrypt.Encrypt(dec, entry.ShortName));
		HandleResource.FinishData(entry, destination);
		return;

Failure:
		Console.WriteLine();
		Prompts.WriteError("ERROR: Failed to install mods!");
		Prompts.WriteWarning("Failed to decrypt vanilla package data (\"generated/binaryfile/package.bfile\")");
		Prompts.ExitPrompt();
		return;
	}
}
