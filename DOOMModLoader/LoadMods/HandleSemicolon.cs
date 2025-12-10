using DOOMModLoader.Shared;
using System;
using System.IO;
using System.Security;

// Removes suffixes from loose file names, e.g. renaming "player.decl;entityDef" to "player.decl"

namespace DOOMModLoader.LoadMods;
static class HandleSemicolon
{
	public static bool? Choice = null; // Store the choice for all files in the same mod



	// Removes the type suffix from a file, and returns the new file path
	public static string RemoveSuffix(string filePath, string relativePath)
	{
		if (Choice is null)
		{
			Console.WriteLine();
			Prompts.WriteWarning($"Note: \"{HandleMods.Data.CurrentMod}\" contains files with a type suffix, which is superfluous");
			Prompts.WriteWarning("Do you want to remove all suffixes?");
			Prompts.WriteWarning($"(\"{Path.GetFileName(relativePath)}\" will become \"{Path.GetFileName(relativePath[0 .. relativePath.IndexOfOrdinal(';')])}\")");
			Console.Write(
				""
				+ "\n(Press [Y] to remove suffixes from file names)"
				+ "\n(Press [N] to keep the suffixes)"
			);
			Choice = Prompts.GetYesOrNo();

			if (Choice is null)
			{
				Prompts.WriteWarning("Warning: Failed to detect keystroke");
				Choice = false;
			}
			else if (Choice == true)
			{
				Console.WriteLine();
				Console.WriteLine("        Removing type suffixes...");
			}
		}

		if (Choice == false)
			return filePath;

		// Instead of "filePath.LastIndexOf", use "relativePath.Length - relativePath.IndexOf", so that we remove all
		// semicolons within the resource path, yet without removing any semicolons before the mod directory
		string newPath = filePath[0 .. ^(relativePath.Length - relativePath.IndexOfOrdinal(';'))];

		try
		{
			File.Move(filePath, newPath);
			return newPath;
		}
		catch (IOException) // "newPath" already exists. If the file contents are identical, just delete one of them
		{
			try
			{
				ReadOnlySpan<byte> oldBytes = File.ReadAllBytes(filePath);
				ReadOnlySpan<byte> newBytes = File.ReadAllBytes(newPath);
				if (MemoryExtensions.SequenceEqual(oldBytes, newBytes))
				{
					File.Delete(filePath);
					return newPath;
				}
				else if (relativePath.StartsWithOrdinal("generated/binaryfile/strings/"))
				{
					// If one string file is encrypted while the other isn't, and they're identical when decrypted,
					// then we can just keep the decrypted file
					string key = $"strings/{Path.GetFileNameWithoutExtension(relativePath)}.lang";
					IdCrypt.Decrypted? oldDec = IdCrypt.Decrypt(oldBytes, key);
					IdCrypt.Decrypted? newDec = IdCrypt.Decrypt(newBytes, key);
					if (MemoryExtensions.SequenceEqual(oldDec?.Data ?? oldBytes, newDec?.Data ?? newBytes))
					{
						// If "newPath" and ONLY "newPath" was encrypted, overwrite it with the decrypted file
						if (oldDec is null && newDec is not null)
							File.Move(filePath, newPath, overwrite: true);
						else // If "newPath" was decrypted or they're both encrypted, delete the semicolon file
							File.Delete(filePath);
						return newPath;
					}
				}

				// If the files with and without a semicolon are different, make the user handle it manually
				Console.WriteLine();
				Prompts.WriteError("ERROR: Failed to install mods!");
				Prompts.WriteWarning($"Could not rename \"{HandleMods.Data.CurrentMod}/{relativePath}\", as \"{Path.GetFileName(newPath)}\" already exists");
				Prompts.ExitPrompt();
				return null;
			}
			catch (Exception e) when (e is DirectoryNotFoundException or FileNotFoundException
			or IOException or SecurityException or UnauthorizedAccessException)
				{goto Failure;}
		}
		catch (Exception e) when (e is DirectoryNotFoundException or FileNotFoundException or UnauthorizedAccessException)
			{goto Failure;}

Failure:
		Console.WriteLine();
		Prompts.WriteError("ERROR: Failed to install mods!");
		Prompts.WriteWarning($"Could not rename \"{HandleMods.Data.CurrentMod}/{relativePath}\". Try rebooting your computer and running DOOMModLoader again");
		Prompts.ExitPrompt();
		return null;
	}
}
