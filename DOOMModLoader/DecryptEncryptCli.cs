using DOOMModLoader.Shared;
using System;
using System.IO;
using System.Security;

// Decrypts and encrypts certain binary files on disk
// Special thanks to emoose for figuring out the encryption: https://github.com/emoose/DOOMExtract/tree/master/idCrypt

namespace DOOMModLoader;
static class DecryptEncryptCli
{
	// Makes sure that no invalid arguments are used, and sets a default output path
	public static void ProcessArguments(bool encrypt)
	{
		// Validate arguments
		string text = "";
		if (Config.Cli.CheckForUpdates == true)
			text += "-checkforupdates, ";
		if (Config.Cli.Filters.Count != 0)
			text += "-filter, ";
		if (!encrypt && (Config.Cli.Iv is not null))
			text += "-iv, ";
		if (Config.Cli.LaunchGame == true)
			text += "-launchgame, ";
		if (Config.Cli.PatchGame == true)
			text += "-patchgame, ";
		if (!encrypt && (Config.Cli.Salt is not null))
			text += "-salt, ";
		if (Config.Cli.ShowConflicts == true)
			text += "-showconflicts, ";
		if (Config.Cli.ShowZipWarnings == true)
			text += "-showzipwarnings, ";
		if (Config.Cli.Simulate)
			text += "-simulate, ";
		if (Config.Cli.SnapMap == true) // This doesn't differentiate between "-snap" and "-snapmap"
			text += "-snapmap, ";
		if (Config.Cli.Types.Count != 0)
			text += "-type, ";
		if (Config.Cli.UncapCutscenes == true)
			text += "-uncapcutscenes, ";
		if (!string.IsNullOrEmpty(text))
		{
			Console.WriteLine();
			Prompts.WriteError($"ERROR: The following cannot be used with \"-{(encrypt ? "en" : "de")}crypt\":");
			Prompts.WriteWarning($"    {text[0 .. ^2]}"); // Trim the last comma and space
			Prompts.ExitPrompt();
			return;
		}

		// Finalise arguments
		if (string.IsNullOrEmpty(Config.Cli.Out)) // If "-out" isn't specified, add a suffix by default
			Config.Cli.Out = Config.Cli.In + (encrypt ? ".bfile" : ".dec");
		else if (File.Exists(Config.Cli.Out) && !Config.Cli.Force) // Only require "-force" for "-out"
		{
			Console.WriteLine();
			Prompts.WriteError($"ERROR: Failed to encrypt \"{Utility.HideUserName(Config.Cli.In)}\"!");
			Prompts.WriteWarning($"\"{Utility.HideUserName(Config.Cli.Out)}\" already exists");
			Prompts.WriteWarning("If you want to overwrite existing files, use \"-force\"");
			Prompts.ExitPrompt();
			return;
		}

		if (Directory.Exists(Config.Cli.Out))
		{
			Console.WriteLine();
			Prompts.WriteError($"ERROR: Failed to encrypt \"{Utility.HideUserName(Config.Cli.In)}\"!");
			Prompts.WriteWarning($"\"{Utility.HideUserName(Config.Cli.Out)}\" is a directory, not a file");
			Prompts.ExitPrompt();
			return;
		}
	}

	// Decrypts the "-in" file on disk
	public static void DecryptFile(string key)
	{
		try
		{
			ReadOnlySpan<byte> bytes = File.ReadAllBytes(Config.Cli.In);
			IdCrypt.Decrypted? dec = IdCrypt.Decrypt(bytes, key);
			if (dec is null)
			{
				Console.WriteLine();
				Prompts.WriteError($"ERROR: Failed to decrypt \"{Utility.HideUserName(Config.Cli.In)}\"!");
				// Encrypted data should be padded to 0x10 alignment plus the header size, so check the file size
				// Note that if the size is correct, there's no way to tell between "not encrypted" and "wrong key"
				if (bytes.Length < IdCrypt.HeaderSize || (bytes.Length - IdCrypt.HeaderSize) % 0x10 != 0)
					Prompts.WriteWarning("The file is not \"binaryFile\"-style encrypted");
				else
					Prompts.WriteWarning($"Incorrect decryption key (\"{key}\")?");
				Prompts.ExitPrompt();
				return;
			}
			File.WriteAllBytes(Config.Cli.Out, dec.Data);

			Console.WriteLine();
			Console.WriteLine($"Decryption key: \"{key}\"");
			if (Config.Cli.Verbose == true) // Not "Config.Final"
			{
				Console.WriteLine();
				Prompts.WriteVerbose(
					"Verbose details:"
					+ $"\nSalt found:        0x{Convert.ToHexString(dec.Salt)}"
					+ $"\nAES IV found:      0x{Convert.ToHexString(dec.AesIv)}"
					+ $"\nHMAC hash matched: 0x{Convert.ToHexString(dec.HmacHash)}"
					+ $"\nSuccessfully decrypted {dec.Data.Length} bytes"
					+  "\n(You don't need the salt/AES IV/HMAC hash to re-encrypt the file. You only have to worry about the decryption key)"
				);
			}
		}
		catch (Exception e) when (e is DirectoryNotFoundException or FileNotFoundException or IOException
		or NotSupportedException or PathTooLongException or SecurityException or UnauthorizedAccessException)
		{
			Console.WriteLine();
			Prompts.WriteError($"ERROR: Failed to decrypt \"{Utility.HideUserName(Config.Cli.In)}\"!");
			if (e is FileNotFoundException)
				Prompts.WriteWarning("The file doesn't exist");
			Prompts.ExitPrompt();
			return;
		}

		Console.WriteLine();
		Prompts.WriteSuccess($"Successfully decrypted \"{Utility.HideUserName(Config.Cli.In)}\"!");
		if (Config.Cli.Out == $"{Config.Cli.In}.dec")
			Console.WriteLine($"Saved the decrypted file to \"{Path.GetFileName(Config.Cli.Out)}\"");
		else
			Console.WriteLine($"Saved the decrypted file to \"{Utility.HideUserName(Config.Cli.Out)}\"");
	}

	// Encrypts the "-in" file on disk
	public static void EncryptFile(string key)
	{
		try
		{
			IdCrypt.Decrypted dec = new()
			{
				Salt     = Config.Cli.Salt ?? new byte[0xC],
				AesIv    = Config.Cli.Iv   ?? new byte[0x10],
				Data     = File.ReadAllBytes(Config.Cli.In),
				HmacHash = null!, // This isn't used for encryption
			};
			ReadOnlySpan<byte> bytes = IdCrypt.Encrypt(dec, key);
			File.WriteAllBytes(Config.Cli.Out, bytes);

			Console.WriteLine();
			Console.WriteLine($"Encryption key: \"{key}\"");
			if (Config.Cli.Verbose == true) // Not "Config.Final"
			{
				Console.WriteLine();
				Prompts.WriteVerbose(
					"Verbose details:"
					+ $"\nSalt used:   0x{Convert.ToHexString(dec.Salt)}"
					+ $"\nAES IV used: 0x{Convert.ToHexString(dec.AesIv)}"
					+ $"\nSuccessfully encrypted {dec.Data.Length} bytes"
					+ $"\nResulting HMAC hash: 0x{Convert.ToHexString(bytes[^0x20 .. ^0])}"
					+  "\n(You only have to worry about the encryption key)"
				);
			}
		}
		catch (Exception e) when (e is DirectoryNotFoundException or FileNotFoundException or IOException
		or NotSupportedException or PathTooLongException or SecurityException or UnauthorizedAccessException)
		{
			Console.WriteLine();
			Prompts.WriteError($"ERROR: Failed to encrypt \"{Utility.HideUserName(Config.Cli.In)}\"!");
			if (e is FileNotFoundException)
				Prompts.WriteWarning("The file doesn't exist");
			Prompts.ExitPrompt();
			return;
		}

		Console.WriteLine();
		Prompts.WriteSuccess($"Successfully encrypted \"{Utility.HideUserName(Config.Cli.In)}\"!");
		if (Config.Cli.Out == $"{Config.Cli.In}.bfile")
			Console.WriteLine($"Saved the encrypted file to \"{Path.GetFileName(Config.Cli.Out)}\"");
		else
			Console.WriteLine($"Saved the encrypted file to \"{Utility.HideUserName(Config.Cli.Out)}\"");
	}
}
