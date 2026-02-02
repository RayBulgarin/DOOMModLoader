using DOOMModLoader.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

// Parses "generated/binaryfile/strings/*.bfile" files and loads custom strings

namespace DOOMModLoader.LoadMods;
static class HandleStrings
{
	class LangResource
	{
#pragma warning disable IDE0044 // "Make field readonly" - "required" and "readonly" don't work together
		public required ResourceArchiveEntry Resource;
		public required OrderedDictionary<byte[], byte[]> Strings; // Use UTF-8 byte arrays for string names and texts
		public required OrderedDictionary<byte[], byte[]> StagedStrings; // Non-current mods' strings
		public required OrderedDictionary<byte[], byte[]> ModStrings; // The current mod's strings
		public required byte[] Salt; // The original salt and AES IV used to encrypt the vanilla strings
		public required byte[] AesIv; // (We don't have to reuse the original salt/AES IV, but still)
#pragma warning restore IDE0044
	}

	// DOOM (2016), SnapMap, and DOOM VFR have 11 string resources
	static readonly Dictionary<string, LangResource> langMap = new(11);
	static LangResource? englishLang = null; // We reference the English strings later
	static bool shouldFlush = false; // Set to true if the mod being processed contains strings



	// Moves "i" to the ending quote of a string's name or "max lengths"
	// "i" should be positioned after the opening quote before calling this method
	// Returns true on success, false otherwise
	static bool FindStringNameEnd(ReadOnlySpan<byte> bytes, ref int i, bool isName)
	{
		int old_i = i;
		i += MemoryExtensions.IndexOf(bytes[i .. ^0], (byte)'\"'); // Don't check for backslash escape sequences
		if (i == old_i-1) // Syntax error - The string is unterminated
		{
			i = old_i;
			return false;
		}

		// Unlike DOOM (2016), we consider it a syntax error if the name contains a control character,
		// except for the two vanilla string names that contain a newline and a tab (We support LF, CR, and CRLF)
		if (!MemoryExtensions.ContainsAnyInRange(bytes[old_i .. i], (byte)'\0', (byte)'\x1F')
		|| (isName &&
		(
			MemoryExtensions.SequenceEqual(   bytes[old_i .. i], "\n\t"u8)
			|| MemoryExtensions.SequenceEqual(bytes[old_i .. i], "\r\t"u8)
			|| MemoryExtensions.SequenceEqual(bytes[old_i .. i], "\r\n\t"u8)
			|| MemoryExtensions.SequenceEqual(bytes[old_i .. i], "#str_snapmap\n\t"u8)
			|| MemoryExtensions.SequenceEqual(bytes[old_i .. i], "#str_snapmap\r\t"u8)
			|| MemoryExtensions.SequenceEqual(bytes[old_i .. i], "#str_snapmap\r\n\t"u8)
		)))
			return true;
		else
		{
			i = old_i;
			return false;
		}
	}

	// Moves "i" to the ending quote of a string's text, supporting escape sequences
	// "i" should be positioned after the opening quote before calling this method
	// Returns true on success, false otherwise
	static bool FindStringTextEnd(ReadOnlySpan<byte> bytes, ref int i)
	{
		int old_i = i;
		while (true)
		{
			int index = MemoryExtensions.IndexOfAny(bytes[i .. ^0], (byte)'"', (byte)'\\');
			if (index == -1) // Syntax error - The string is unterminated
			{
				i = old_i;
				return false;
			}
			i += index;

			if (bytes[i] == '"') // Ending quote
				break;
			else // Backslash. Include the next byte for escaped quotes (\")
			{
				i++;
				if (i >= bytes.Length || ((char)bytes[i] is not ('n' or 't' or '\"' or '\\')))
				{
					i = old_i;
					return false; // Syntax error - Unknown escape sequence
				}
			}

			i++;
		}

		// Unlike DOOM (2016), we consider it a syntax error if the text contains a literal newline/control character
		if (!MemoryExtensions.ContainsAnyInRange(bytes[old_i .. i], (byte)'\0', (byte)'\x1F')
		|| MemoryExtensions.SequenceEqual(bytes[old_i .. i], "1.)\tHealth"u8)) // Except for this DOOM VFR string
			return true;
		else
		{
			i = old_i;
			return false;
		}
	}

	// Parses a decrypted string file
	// If "vanilla" is true, populates the vanilla strings list. Otherwise, compares custom strings to the vanilla list
	static void ParseAllStrings(ReadOnlySpan<byte> bytes, LangResource lang, bool vanilla)
	{
		// Skip the UTF-8 byte order mark if present. Unlike DOOM (2016), assume UTF-8 even if it's missing
		int i = 0;
		if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
			i = 3;

		// Unlike DOOM (2016), require a "{" before anything else besides comments
		// DOOM (2016) only understands single-line comments in string files, but we support multi-line comments too
		if (!DeclParser.FindTokenStart(bytes, ref i) || bytes[i] != '{')
			goto Failure;
		i++;

		// Go through all strings
		while (true)
		{
			// Find a string name, allowing comments in between
			if (!DeclParser.FindTokenStart(bytes, ref i) || bytes[i] != '"')
				break;

			i++; // Don't include the surrounding quotes
			int nameStart = i;
			if (!FindStringNameEnd(bytes, ref i, true))
				goto Failure;
			int nameEnd = i;
			i++;

			if (nameStart == nameEnd)
				goto Failure; // Unlike DOOM (2016), we consider empty string names a syntax error

			// Find the string's text. DOOM (2016) doesn't understand comments beforehand, so we forbid them
			for (; i < bytes.Length; i++)
			{
				if ((char)bytes[i] is '\t' or ' ') // Skip white space. DOOM (2016) allows newlines here, but we don't
					continue;
				break;
			}
			if (i >= bytes.Length || bytes[i] != '"')
				goto Failure; // Unlike DOOM (2016), we consider anything else a syntax error

			i++;
			int textStart = i;
			if (!FindStringTextEnd(bytes, ref i))
				goto Failure;
			int textEnd = i;
			i++;

			// Find the next line or the string's optional "max lengths". Unlike DOOM (2016), fully support comments
			bool allowComment = true;
			int allowQuote = 2;
			for (; i < bytes.Length; i++)
			{
				if ((char)bytes[i] is '\t' or ' ') // Skip white space, excluding newlines. DOOM (2016) allows CR (\r) here
					continue;
				else if (bytes[i] == '/' && allowComment) // Unlike DOOM (2016), properly skip comments after a string's text
				{
					i++;
					if (!DeclParser.SkipComment(bytes, ref i))
						goto Failure;
					if ((char)bytes[i] is '\n' or '\r')
						break; // Single-line comments are terminated by the newline that we were looking for

					allowQuote = 0; // Don't allow "max lengths" after multi-line comments
					continue; // Loop to look for a newline after multi-line comments
				}
				else if ((bytes[i] == '"' && allowQuote > 0) // An optional "max length" for the string
				|| vfrEdgeCase(bytes, ref i, ref allowQuote, bytes[nameStart .. nameEnd], bytes[textStart .. textEnd]))
				{
					i++;
					if (!FindStringNameEnd(bytes, ref i, false))
						goto Failure;
					textEnd = i; // As a "hack", just include the "max length" as part of the string's text

					allowQuote--; // Allow either zero, one, or two "max lengths", but no more than that
					allowComment = (allowQuote == 0); // DOOM (2016) doesn't understand comments between "max lengths"
					continue;
				}
				break; // Found a newline, closing curly bracket, or something unexpected
			}

			// Ensure that we're at a newline (or "}"). Unlike DOOM (2016), we don't allow multiple strings on one line
			if (i >= bytes.Length || (char)bytes[i] is not ('\n' or '\r' or '}')) // Unlike DOOM (2016), use CR (\r) here
				goto Failure; // Unlike DOOM (2016), we consider anything else a syntax error

			// Store the string
			if (i >= bytes.Length) // Syntax error (or a missing "}" at the end, which we also consider a syntax error)
				goto Failure;

			byte[] name = bytes[nameStart .. nameEnd].ToArray();
			byte[] text = bytes[textStart .. textEnd].ToArray();

			if (vanilla)
				lang.Strings[name] = text;
			else // Compare it to the vanilla strings; don't override another mod's custom strings with vanilla strings
			{
#pragma warning disable IDE0018 // "Variable declaration can be inlined"
				byte[]? vanillaText;
#pragma warning restore IDE0018
				if (lang.Strings.TryGetValue(name, out vanillaText) && MemoryExtensions.SequenceEqual(text, vanillaText))
					HandleWarnings.AddVanillaStrings();
				else if (!lang.ModStrings.TryAdd(name, text))
				{
					int line = (MemoryExtensions.Count(bytes[0 .. i], (byte)'\n') + 1);
					string nameStr = Encoding.UTF8.GetString(name);

					Console.WriteLine();
					Prompts.WriteError("ERROR: Failed to install mods!");
					Prompts.WriteWarning($"\"{lang.Resource.FullName}\": Duplicate string name \"{nameStr}\" at line #{line}");
					Prompts.ExitPrompt();
					return;
				}
			}
		}

		// If we're at a "}" now, success!
		if (i >= bytes.Length || bytes[i] != '}')
		{
			// DOOM VFR is missing the "}", so don't fail at an unexpected end of file if this is DOOM VFR
			if (i < bytes.Length || BuildInfo.CurrentBuild!.Game != BuildInfo.GameKind.DOOM_VFR)
				goto Failure; // Unlike DOOM (2016), we consider anything other than "}" a syntax error
		}
		i++;
		DeclParser.FindTokenStart(bytes, ref i);
		if (i < bytes.Length)
			goto Failure; // Unlike DOOM (2016), we consider any further tokens a syntax error

		return;

Failure:
		Console.WriteLine();
		Prompts.WriteError("ERROR: Failed to install mods!");
		{
			i = int.Min(i, bytes.Length);
			int line = (MemoryExtensions.Count(bytes[0 .. i], (byte)'\n') + 1);
			if (vanilla)
				Prompts.WriteWarning($"Failed to parse vanilla strings (line #{line} of \"{lang.Resource.FullName}\")");
			else
				Prompts.WriteWarning($"Failed to parse line #{line} of \"{lang.Resource.FullName}\"");
		}
		Prompts.ExitPrompt();
		return;

		// Two of DOOM VFR's strings are malformed; support them
		static bool vfrEdgeCase(ReadOnlySpan<byte> bytes, ref int i, ref int allowQuote, ReadOnlySpan<byte> name, ReadOnlySpan<byte> text)
		{
			if (BuildInfo.CurrentBuild!.Game != BuildInfo.GameKind.DOOM_VFR)
				return false;
			else if (bytes[i] == '2' && allowQuote == 2
			&& MemoryExtensions.SequenceEqual(name, "#swf_guis_end_of_line"u8))
				return true;
			else if (bytes[i] == 'A' && allowQuote == 2
			&& MemoryExtensions.SequenceEqual(name, "#swf_guis_purchasereqlegal"u8)
			&& MemoryExtensions.SequenceEqual(text, "By pressing "u8) // Ends with a space
			&& (i + "Accept\""u8.Length) <= bytes.Length
			&& MemoryExtensions.StartsWith(bytes[i .. ^0], "Accept\""u8))
			{
				i += "Accept\""u8.Length - 1; // Move to the second unescaped quote in "Accept"
				allowQuote += 1; // Account for the unescaped quotes
				return true;
			}
			return false;
		}
	}

	// ASCII-case-insensitive equals check
	static bool StringNameEquals(byte[]? x, byte[]? y)
	{
		ReadOnlySpan<byte> first = x;
		ReadOnlySpan<byte> second = y;

		if (first.Length != second.Length)
			return false;

		if (MemoryExtensions.ContainsAnyInRange(first, (byte)'A', (byte)'Z'))
		{
			Span<byte> span = new byte[first.Length];
			Ascii.ToLower(first, span, out _);
			first = span;
		}
		if (MemoryExtensions.ContainsAnyInRange(second, (byte)'A', (byte)'Z'))
		{
			Span<byte> span = new byte[second.Length];
			Ascii.ToLower(second, span, out _);
			second = span;
		}
		return MemoryExtensions.SequenceEqual(first, second);
	}

	// ASCII-case-insensitive hash code
	static int StringNameHash(byte[] obj)
	{
		// Lowercase a-z's bitmask is 0b011-_----, uppercase A-Z's bitmask is 0b010-_----
		// The two high bits never change for a-z, and we MUST ignore the third-highest bit, so just keep the five low bits
		long result = 0;
		int shift = 0;
		foreach (byte x in obj) // Most string names are less than 60 bytes long, so iterating over all bytes is fine
		{
			result += ((long)x & 0b0001_1111) << shift;
			shift = (shift + 5) & 0x1F; // "(shift + 5) % 32"
		}
		return (int)(result + (result >> 32)); // Add the "overflow" as if the shifted and added bits had rotated
	}

	// Loads and parses all vanilla strings for all languages. Must be called before "LoadCustomStrings"
	public static void LoadVanillaStrings(ResourceArchive container)
	{
		Prompts.WriteVerbose("    [Parsing vanilla strings]...");

		// Use an ASCII-case-insensitive comparer for the string dictionary keys
		EqualityComparer<byte[]> comparer = EqualityComparer<byte[]>.Create(StringNameEquals, StringNameHash);

		foreach (ResourceArchiveEntry entry in container.Entries)
		{
			if (entry.Type != "binaryFile" || !entry.FullName.StartsWithOrdinal("generated/binaryfile/strings/"))
				continue;

			IdCrypt.Decrypted? dec;
			using (Stream stream = entry.Open())
			{
				Span<byte> bytes = new byte[entry.Length]; // Should be less than 5 megabytes
				try
					{stream.ReadExactly(bytes);}
				catch (Exception e) when (e is InvalidDataException or IOException)
					{goto Failure;}
				dec = IdCrypt.Decrypt(bytes, entry.ShortName);
			}

			LangResource lang = new()
			{
				// DOOM VFR has 31794 strings by default (DOOM (2016) and SnapMap have 30166)
				// Mods can add more strings, though, so set the initial capacity a bit higher
				Resource      = entry,
				Strings       = new OrderedDictionary<byte[], byte[]>(32768, comparer),
				StagedStrings = new OrderedDictionary<byte[], byte[]>(   32, comparer),
				ModStrings    = new OrderedDictionary<byte[], byte[]>(   32, comparer),
				Salt          = dec?.Salt  ?? null!, // If "dec" is null, this won't be used, so "null" is fine
				AesIv         = dec?.AesIv ?? null!,
			};

			if (langMap.TryAdd(entry.ShortName, lang) && dec is not null)
			{
				ParseAllStrings(dec.Data, lang, true); // Todo: Do this on a separate thread per language?
				continue;
			}
			else
				goto Failure; // Duplicate string resource, or failed to decrypt

Failure:
			Console.WriteLine();
			Prompts.WriteError("ERROR: Failed to install mods!");
			Prompts.WriteWarning($"Failed to decrypt vanilla strings (\"{entry.FullName}\")");
			Prompts.ExitPrompt();
			return;
		}

		englishLang = langMap.GetValueOrDefault("strings/english.lang"); // Set "englishLang" for later
		if (englishLang is null)
		{
			Console.WriteLine();
			Prompts.WriteError("ERROR: Failed to install mods!");
			Prompts.WriteWarning("Failed to decrypt vanilla strings (\"generated/binaryfile/strings/english.bfile\")");
			Prompts.ExitPrompt();
			return;
		}
	}

	// Loads and parses custom strings
	public static void LoadCustomStrings(Stream source, ResourceArchiveEntry entry, int length)
	{
#pragma warning disable IDE0018 // "Variable declaration can be inlined"
		LangResource? lang;
#pragma warning restore IDE0018
		if (!langMap.TryGetValue(entry.ShortName, out lang))
			Utility.Assert(false, $"LoadCustomStrings: lang ({entry.ShortName}) == null");

		Span<byte> bytes = new byte[length]; // Should be less than 5 megabytes
		try
			{source.ReadExactly(bytes);}
		catch (Exception e) when (e is InvalidDataException or IOException)
		{
			Console.WriteLine();
			Prompts.WriteError("ERROR: Failed to install mods!");
			Prompts.WriteWarning("Make sure that none of the game files are currently open. Try rebooting your computer and running DOOMModLoader again");
			Prompts.WriteVerbose($"({e.GetType().Name} in LoadCustomStrings)");
			Prompts.ExitPrompt();
			return;
		}
		IdCrypt.Decrypted? dec = IdCrypt.Decrypt(bytes, entry.ShortName);

		ParseAllStrings(dec?.Data ?? bytes, lang, false); // Todo: Do this on a separate thread?
		shouldFlush = true;

		Prompts.WriteVerboseReplaced($"        Modified {entry.FullName}");
	}

	// Flushes custom strings after each individual mod
	public static void FlushCustomStrings()
	{
		if (!shouldFlush)
			return;
		shouldFlush = false;

		foreach (LangResource lang in langMap.Values)
		{
			foreach (KeyValuePair<byte[], byte[]> modStr in lang.ModStrings) // Flush language-specific strings
				lang.StagedStrings.TryAdd(modStr.Key, modStr.Value); // Don't override already-staged strings

			if (lang != englishLang) // For non-English languages, also fall back to the mod's English strings
			{
				foreach (KeyValuePair<byte[], byte[]> engStr in englishLang!.ModStrings)
					lang.StagedStrings.TryAdd(engStr.Key, engStr.Value);
				lang.ModStrings.Clear();
			}
		}

		englishLang!.ModStrings.Clear(); // English mod strings must be cleared after all other languages are flushed
	}

	// Saves all modified string resources, and adds DOOMModLoader's version number and mod count to the title screen
	public static void SaveStrings(FileStream destination)
	{
		byte[] creditsName = "#str_swf_credits"u8.ToArray();
		if (BuildInfo.CurrentBuild!.Game == BuildInfo.GameKind.DOOM_VFR)
			creditsName = "#str_menu_root_exit_label"u8.ToArray();

		ReadOnlySpan<byte> modInfo;
		if (HandleMods.Data.ModFileNames.Count != 0)
			modInfo = Encoding.UTF8.GetBytes($"        {HandleMods.Data.ModFileNames.Count} MOD{(HandleMods.Data.ModFileNames.Count == 1 ? "" : "S")} (DML v{Program.VersionString})");
		else // When no mods are installed, show how many built-in patches are active instead
		{
			Utility.Assert(Config.Final.UncapCutscenes, "SaveStrings: No mods nor patches installed");
			modInfo = Encoding.UTF8.GetBytes($"        1 PATCH (DML v{Program.VersionString})");
		}

		foreach (LangResource lang in langMap.Values)
		{
			foreach (KeyValuePair<byte[], byte[]> customStr in lang.StagedStrings)
				lang.Strings[customStr.Key] = customStr.Value;

			// Append DOOMModLoader's version and mod count to a string on the main menu
			int index = lang.Strings.IndexOf(creditsName);
			if (index != -1)
			{
				lang.Strings.SetAt(index, [
					.. lang.Strings.GetAt(index).Value,
					.. modInfo,
				]);
			}

			// Determine how many bytes we'll need to fit the string contents
			long newLength = ("XXX{\n}"u8.Length + (lang.Strings.Count * "\t\"\"\t\"\"\n"u8.Length));
			foreach (KeyValuePair<byte[], byte[]> str in lang.Strings)
				newLength += (str.Key.Length + str.Value.Length);
			if (newLength > Array.MaxLength)
			{
				Console.WriteLine();
				Prompts.WriteError("ERROR: Failed to install mods!");
				Prompts.WriteWarning($"Failed to encrypt strings (\"{lang.Resource.FullName}\")");
				Prompts.ExitPrompt();
				return;
			}

			IdCrypt.Decrypted dec = new()
			{
				Salt     = lang.Salt,
				AesIv    = lang.AesIv,
				Data     = new byte[newLength],
				HmacHash = null!, // This isn't used for encryption
			};
			Span<byte> bytes = dec.Data;

			bytes[0] = 0xEF; // Start with a UTF-8 byte order mark, otherwise DOOM (2016) explodes at non-ASCII bytes
			bytes[1] = 0xBB;
			bytes[2] = 0xBF;
			bytes[3] = (byte)'{';
			bytes[4] = (byte)'\n';
			int i = 5;
			foreach (KeyValuePair<byte[], byte[]> str in lang.Strings)
			{
				ReadOnlySpan<byte> line = [
					(byte)'\t',
					(byte)'"',
					.. str.Key,
					(byte)'"',
					(byte)'\t',
					(byte)'"',
					.. str.Value,
					(byte)'"',
					(byte)'\n',
				];
				line.CopyTo(bytes[i .. ^0]);
				i += line.Length;
			}
			bytes[i] = (byte)'}';
			Utility.Assert(i == newLength-1, $"SaveStrings: i ({i}) != newLength-1 ({newLength-1})");

			HandleResource.StartData([lang.Resource], destination);
			destination.Write(IdCrypt.Encrypt(dec, lang.Resource.ShortName));
			HandleResource.FinishData([lang.Resource], destination);
		}

		langMap.Clear();
	}
}
