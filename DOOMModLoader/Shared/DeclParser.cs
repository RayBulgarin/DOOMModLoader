using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

// Parses a standard decl byte stream into a flattened byte array dictionary
// Doesn't match DOOM (2016)'s behaviour exactly, but should handle all vanilla standard decls

namespace DOOMModLoader.Shared;
static class DeclParser
{
	enum NumberMode // DOOM (2016) supports multiple number formats
	{
		Binary,      // Prefixed with "0b"
		Octal,       // Prefixed with "0"
		Decimal,     // No prefix; integer or float
		Hexadecimal, // Prefixed with "0x"
	}



	// Moves "i" to the end of a single- or multi-line comment
	// "i" should be positioned after the first "/" before calling this method
	// Returns true if the comment was skipped, or false if there's a syntax error or the span ends
	public static bool SkipComment(ReadOnlySpan<byte> bytes, ref int i)
	{
		if (i >= bytes.Length)
			return false;

		int index;
		switch (bytes[i])
		{
			case (byte)'/': // Single-line comment
				// Backslashes don't escape newlines in comments in DOOM (2016)
				// DOOM (2016) doesn't actually stop at CR (\r), but we do
				index = MemoryExtensions.IndexOfAny(bytes[i .. ^0], (byte)'\n', (byte)'\r');
				if (index == -1)
					return false;
				i += index; // This puts "i" at the newline byte, which lets the caller's "i++" loop move past it
				return true;
			case (byte)'*': // Multi-line comment
				int old_i = i;
				// DOOM (2016) parses "/*/" as both the beginning and the end of the same multi-line comment,
				// so only increment "i" once before the loop
				// (It does NOT parse "*//" as both the end of a multi-comment and the start of a single-line comment)
				for (i++; ; i += 2) // "2" because if a "/" wasn't the end, the next byte can't be the end either
				{
					index = MemoryExtensions.IndexOf(bytes[i .. ^0], (byte)'/');
					if (index == -1)
					{
						i = old_i; // For string file errors to show the unterminated comment's starting line number
						return false;
					}
					i += index; // This puts "i" at the "/", which lets the caller's "i++" loop move past it
					if (bytes[i-1] == '*')
						return true;
				}
			default: // Syntax error
				return false;
		}
	}

	// Moves "i" to the first non-white space/-comment byte
	// Returns true if successful, or false if there's a syntax error or the span ends
	public static bool FindTokenStart(ReadOnlySpan<byte> bytes, ref int i)
	{
		for (; i < bytes.Length; i++)
		{
			if ((char)bytes[i] is '\n' or '\r' or '\t' or ' ') // Skip white space
				continue;
			else if (bytes[i] == '/') // Skip comments
			{
				i++;
				if (!SkipComment(bytes, ref i))
					return false; // Syntax error
			}
			else // Any non-white space/-comment byte
				return true;
		}
		return false; // The span ended
	}

	// Returns a variable name, or "}" if the current struct ends, or null upon a syntax error
	// "i" will be positioned after the variable's "=" or the "}"
	static byte[]? GetVariableName(ReadOnlySpan<byte> bytes, ref int i)
	{
		if (!FindTokenStart(bytes, ref i))
			return null; // Syntax error - The span ended

		// DOOM (2016) allows up to two optional no-op ";"s in a row - but somehow, more than that is a syntax error
		// It does not allow two no-op ";"s before a "}", nor a no-op ";" before multiple "}"s
		// However, we will intentionally always forbid no-op ";"s

		if (bytes[i] == '}') // Return "}" when a struct ends
			return [(byte)'}'];

		// The start of a word may be a-z, A-Z, or "_", but not 0-9
		// DOOM (2016) ignores some unrecognised symbols (e.g. "123edit" is the same as "edit"), but we'll abort instead
		if ((char)bytes[i] is not ((>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or '_'))
			return null; // Syntax error

		// The rest of the word may contain a-z, A-Z, 0-9, and "_"
		int start = i;
		for (i++; i < bytes.Length; i++)
		{
			if ((char)bytes[i] is not ((>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9') or '_'))
				break;
		}
		int end = i;

		if (!FindTokenStart(bytes, ref i))
			return null; // Syntax error - The span ended

		if (bytes[i] == '=') // Expect a "=" after the variable name
		{
			i++;
			return bytes[start .. end].ToArray();
		}
		else if (bytes[i] == '[') // Or an array index, e.g. "item[0]"
		{
			// White space and comments are permitted around the "[", the number, and the "]",
			// so we can't just return all bytes from the variable name to the "]"
			i++;
			if (!FindTokenStart(bytes, ref i))
				return null; // Syntax error - The span ended

			int numStart = i;
			if (bytes[i] == '0') // Todo: Support binary, octal, and hexadecimal numbers (and floats and exponents)
				i++; // For now, we only allow just "0"...
			else if ((char)bytes[i] is >= '1' and <= '9') // ...and standard decimal integers
			{
				for (i++; i < bytes.Length; i++)
				{
					if ((char)bytes[i] is not (>= '0' and <= '9'))
						break;
				}
			}
			else
				return null; // Syntax error - A non-number index
			int numEnd = i;

			if (!FindTokenStart(bytes, ref i) || bytes[i] != ']')
				return null; // Syntax error - The array index wasn't followed by "]"
			i++;
			if (!FindTokenStart(bytes, ref i) || bytes[i] != '=')
				return null; // Syntax error - The "]" wasn't followed by "="
			i++;

			return [ // Return the name and array index as one byte array
				.. bytes[start .. end],
				(byte)'[',
				.. bytes[numStart .. numEnd],
				(byte)']',
			];
		}
		else // Anything besides "=" and "[" after the variable name is a syntax error
			return null;
	}

	// Returns a variable value, or "{" if a new struct starts, or null upon a syntax error
	// "i" will be positioned after the variable's ";" or "{"
	static byte[]? GetVariableValue(ReadOnlySpan<byte> bytes, ref int i)
	{
		if (!FindTokenStart(bytes, ref i))
			return null; // Syntax error - The span ended

		int start;
		int end;
		if (bytes[i] == '"') // A quoted string
		{
			start = i;
			i++; // Move past the quote
			for (; i < bytes.Length; i++)
			{
				if (bytes[i] == '"') // Ending quote
					break;
				else if (bytes[i] == '\\') // Include the next byte, for escaped quotes (\")
				{
					// DOOM (2016) doesn't actually do this for decl strings, but we do
					i++;
					if (i >= bytes.Length || bytes[i] == '\n')
						return null; // Syntax error - The string contains a literal LF (\n) newline, or is unterminated
				}
				else if (bytes[i] == '\n')
					return null; // Syntax error - The string contains a LF (\n) newline (but CR (\r) is fine)
			}
			i++; // Move past the quote
			end = i;
		}
		else if ((char)bytes[i] is (>= '0' and <= '9') or '-' or '.') // A number. Cannot start with "+"
		{
			bool allowPeriod = (bytes[i] != '.');
			bool allowExponent = true;
			NumberMode mode = NumberMode.Decimal;

			start = i;
			for (i++; i < bytes.Length; i++)
			{
				if ((mode == NumberMode.Decimal    && ((char)bytes[i] is >= '0' and <= '9'))
				|| (mode == NumberMode.Binary      && ((char)bytes[i] is '0' or '1'))
				|| (mode == NumberMode.Octal       && ((char)bytes[i] is >= '0' and <= '7'))
				|| (mode == NumberMode.Hexadecimal && ((char)bytes[i] is (>= '0' and <= '9') or (>= 'A' and <= 'F') or (>= 'a' and <= 'f'))))
					continue; // Digits don't need special handling
				else if (allowPeriod && bytes[i] == '.')
					allowPeriod = false; // Don't allow multiple periods
				else if (allowExponent && bytes[i] == 'e') // DOOM (2016) only allows a lowercase "e"
				{
					allowExponent = false; // Don't allow multiple exponents
					allowPeriod = false; // Don't allow a period after an exponent

					if ((i == start+1 && (char)bytes[start] is '-' or '.')
					|| (i == start+2 && bytes[start] == '-' && bytes[start+1] == '.'))
						return null; // Syntax error; don't allow a number to start with "-e", ".e", or "-.e"

					// DOOM (2016) does not actually require anything after the "e" (not even numbers),
					// but we should still handle the next character here to specifically allow "+" and "-"
					i++;
					if (i >= bytes.Length || ((char)bytes[i] is not ((>= '0' and <= '9') or '+' or '-')))
						break; // The number ended right after the "e" - which is fine by DOOM (2016)
				}
				else if ((i == start+1 && bytes[start] == '0') // "0b", "0x", and "0" prefixes
				|| (i == start+2 && bytes[start] == '-' && bytes[start+1] == '0'))
				{
					allowPeriod = false;
					allowExponent = false; // DOOM (2016) doesn't support "P" powers for hexadecimal

					if ((char)bytes[i] is 'B' or 'b')
						mode = NumberMode.Binary;
					else if ((char)bytes[i] is 'X' or 'x')
						mode = NumberMode.Hexadecimal;
					else if ((char)bytes[i] is >= '0' and <= '7')
						mode = NumberMode.Octal;
					else
						break;
				}
				else
					break;
			}
			end = i;

			if ((char)bytes[end-1] is 'B' or 'X' or 'b' or 'x' // Don't allow a binary/hexadecimal prefix without a value
			|| (end == start+1 && (char)bytes[start] is '-' or '.') // Don't allow "-" or "." by themselves
			|| (end == start+2 && bytes[start] == '-' && bytes[start+1] == '.')) // Don't allow "-." by itself
				return null;
		}
		else if ((char)bytes[i] is 't' or 'f' or 'N') // "true", "false", or "NULL"
		{
			start = i;
			i += (bytes[i] == 'f' ? "false"u8.Length : "true"u8.Length);
			end = i;

			if (end > bytes.Length
			|| (!MemoryExtensions.SequenceEqual(bytes[start .. end],  "true"u8)
			&&  !MemoryExtensions.SequenceEqual(bytes[start .. end], "false"u8)
			&&  !MemoryExtensions.SequenceEqual(bytes[start .. end],  "NULL"u8)))
				return null; // Syntax error - Not "true", "false", nor "NULL"
		}
		else if (bytes[i] == '{') // Return "{" when a struct starts
			return [(byte)'{'];
		else // Syntax error
			return null;

		if (!FindTokenStart(bytes, ref i) || bytes[i] != ';')
			return null; // Syntax error - The value wasn't followed by ";"

		i++;
		return bytes[start .. end].ToArray();
	}

	// Returns a flattened dictionary from a decl stream
	// If a syntax error is encountered, ".error" is set, and all already-parsed values are returned
	public static Dictionary<string, byte[]> Parse(Stream stream, int length)
	{
		/* Given an input like...
		{
			inherit = "weapon/zion/base/default";
			edit = {
				handsModelMD6 = "zion/objects/weapons/pistols/pistol.md6";
				handsFovScale = 0.6200000048;
				ownerForgetAfterDrop = true;
				givePerksOnReceive = {
					num = 1;
					item[0] = "perk/zion/player/sp/weapons/pistol/secondary_charge_shot";
				}
			}
		}
		...the returned dictionary should contain...
		[
			("inherit", "\"weapon/zion/base/default\""u8),
			("edit.handsModelMD6", "\"zion/objects/weapons/pistols/pistol.md6\""u8),
			("edit.handsFovScale", "0.6200000048"u8),
			("edit.ownerForgetAfterDrop", "true"u8),
			("edit.givePerksOnReceive.num", "1"u8),
			("edit.givePerksOnReceive.item[0]", "\"perk/zion/player/sp/weapons/pistol/secondary_charge_shot\""u8),
		] */

		Dictionary<string, byte[]> result = [];

		// This SHOULD be fine, even for very long custom files; they're usually much less than 20 megabytes
		// Buffering is an option, it's just tedious
		Span<byte> bytes = new byte[length];
		try
			{stream.ReadExactly(bytes);}
		catch (Exception e) when (e is InvalidDataException or IOException)
		{
			Console.WriteLine();
			Prompts.WriteError("ERROR: Failed to install mods!");
			Prompts.WriteWarning("Make sure that none of the game files are currently open. Try rebooting your computer and running DOOMModLoader again");
			Prompts.WriteVerbose($"({e.GetType().Name} in Parse)");
			Prompts.ExitPrompt();
			return result;
		}

		// Like DOOM (2016), skip the UTF-8 byte order mark if present
		int i = 0;
		if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
			i = 3;

		// DOOM (2016) supports preprocessor directives like "#if" and "#define", but we won't bother handling that

		if (!FindTokenStart(bytes, ref i) || bytes[i] != '{') // Require a "{" before anything else besides comments
		{
			// Syntax error - Even an empty decl should contain at least "{}"
			result[".error"] = [(byte)'t', (byte)'r', (byte)'u', (byte)'e'];
			return result;
		}
		i++;

		string tree = "";
		while (true)
		{
			byte[]? name = GetVariableName(bytes, ref i);
			if (name is null) // Syntax error
			{
				result[".error"] = [(byte)'t', (byte)'r', (byte)'u', (byte)'e'];
				return result;
			}
			else if (name.Length == 1 && name[0] == '}') // End of a struct
			{
				if (string.IsNullOrEmpty(tree)) // End of the whole decl
				{
					// Unlike DOOM (2016), we consider any further tokens a syntax error
					i++;
					FindTokenStart(bytes, ref i);
					if (i < bytes.Length)
						result[".error"] = [(byte)'t', (byte)'r', (byte)'u', (byte)'e'];
					return result;
				}
				// Trim the tree, e.g. turn "abc.def.ghi." into "abc.def.", or "abc." into ""
				tree = tree[0 .. (tree.LastIndexOf('.', tree.Length - 2) + 1)];
				continue; // Look for a new variable name
			}

			byte[]? data = GetVariableValue(bytes, ref i);
			if (data is null) // Syntax error
			{
				result[".error"] = [(byte)'t', (byte)'r', (byte)'u', (byte)'e'];
				return result;
			}
			else if (data.Length == 1 && data[0] == '{') // Start of a struct
				tree = $"{tree}{Encoding.UTF8.GetString(name)}.";
			else // A variable value
			{
				// When a variable is set to something besides a struct, whether it's "NULL" or something else,
				// remove any potential previous struct sub-variables
				foreach (string x in result.Keys)
				{
					if (x.StartsWithOrdinal($"{tree}{Encoding.UTF8.GetString(name)}."))
						result.Remove(x);
				}
				// Now, set the variable - even if it's "NULL", because "Config.File.Load" checks for that
				result[$"{tree}{Encoding.UTF8.GetString(name)}"] = data;
			}
		}
	}
}
