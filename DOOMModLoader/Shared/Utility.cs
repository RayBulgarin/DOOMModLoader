using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Security;

// Miscellaneous utility methods

namespace DOOMModLoader.Shared;
static class Utility
{
	// Terminates if the condition is false, even in release builds
	// "condition" should traditionally still be a read-only operation, though
	[StackTraceHidden]
	public static void Assert([DoesNotReturnIf(false)] bool condition, string message)
	{
		if (!condition)
		{
			Console.WriteLine();
			Prompts.WriteError("ERROR: Problem exists between (programmer's) chair and keyboard!");
			Prompts.WriteWarning(message);
			Prompts.WriteWarning(new StackTrace().ToString().TrimEnd());
			Prompts.ExitPrompt();
			return;
		}
	}

	// Converts an integer to a big-endian byte array
	public static byte[] GetBEBytes(int number)
	{
		if (BitConverter.IsLittleEndian)
			number = BinaryPrimitives.ReverseEndianness(number);
		return BitConverter.GetBytes(number);
	}
	public static byte[] GetBEBytes(long number)
	{
		if (BitConverter.IsLittleEndian)
			number = BinaryPrimitives.ReverseEndianness(number);
		return BitConverter.GetBytes(number);
	}

	// Converts an integer to a little-endian byte array
	public static byte[] GetLEBytes(int number)
	{
		if (!BitConverter.IsLittleEndian)
			number = BinaryPrimitives.ReverseEndianness(number);
		return BitConverter.GetBytes(number);
	}
	public static byte[] GetLEBytes(long number)
	{
		if (!BitConverter.IsLittleEndian)
			number = BinaryPrimitives.ReverseEndianness(number);
		return BitConverter.GetBytes(number);
	}

	// Hides the user's "$HOME" directory/user name from a path, if present
	public static string HideUserName(string text)
	{
		if (!OperatingSystem.IsWindows())
		{
			try // On Linux, if a path is within "$HOME", replace that with "~"
			{
				string? home = Environment.GetEnvironmentVariable("HOME");
				if (home is not null && (text.StartsWithOrdinal($"{home}/") || text == home))
					return $"~{text[home.Length .. ^0]}";
			}
			catch (SecurityException)
				{}
		}

		string userName = (Path.DirectorySeparatorChar + Environment.UserName + Path.DirectorySeparatorChar);
		string replacement = $"{Path.DirectorySeparatorChar}[username]{Path.DirectorySeparatorChar}";
		if (OperatingSystem.IsWindows())
			replacement = $"{Path.DirectorySeparatorChar}[Username]{Path.DirectorySeparatorChar}";
		return text.ReplaceOrdinal(userName, replacement);
	}

	// Compares two version strings. Returns >0 if "ver1" is higher, <0 if it's lower, and 0 if they're equal
	// Expects strings like "1.0", "5", "10.5.105", "0.5a"
	public static int CompareVersion(string verA, string verB)
	{
		if (verA == verB) // Neither is higher if they're the same
			return 0;
		if (string.IsNullOrEmpty(verA) || string.IsNullOrEmpty(verB)) // If one version is empty, the other is higher
			return (string.IsNullOrEmpty(verA) ? (string.IsNullOrEmpty(verB) ? 0 : -1) : 1);

		int iA = 0;
		int iB = 0;

		while (true)
		{
			// Skip leading zeroes - hence why there's a separate "i" variable for each version
			while (iA < verA.Length && verA[iA] == '0')
				iA++;
			while (iB < verB.Length && verB[iB] == '0')
				iB++;
			// Get the number length
			int digits = 0;
			while (iA < verA.Length && (verA[iA] is >= '0' and <= '9'))
			{
				iA++;
				digits++;
			}
			int temp = 0;
			while (iB < verB.Length && (verB[iB] is >= '0' and <= '9'))
			{
				iB++;
				temp++;
			}
			// Compare the numbers
			if (digits != temp)
				return ((digits > temp) ? 1 : -1);
			temp = string.CompareOrdinal(verA, iA - digits, verB, iB - digits, digits);
			if (temp != 0)
				return Math.Sign(temp);

			// Get the non-number length
			digits = 0;
			while (iA < verA.Length && (verA[iA] is not ((>= '0' and <= '9') or '.')))
			{
				iA++;
				digits++;
			}
			temp = 0;
			while (iB < verB.Length && (verB[iB] is not ((>= '0' and <= '9') or '.')))
			{
				iB++;
				temp++;
			}
			// Compare the non-numbers - "b" is more than "a", but less than "aa"
			if (digits != temp)
				return ((digits > temp) ? 1 : -1);
			temp = string.CompareOrdinal(verA, iA - digits, verB, iB - digits, digits);
			if (temp != 0)
				return Math.Sign(temp);

			// Handle the end of one or both version strings, and reaching a period
			if (iA >= verA.Length || iB >= verB.Length) // If at least one is at the end, the other is either higher or equal
				return NonZeroes(verA, iA, verB, iB); // (E.g. "1.0" is equal to "1.0.0")
			if ((verA[iA] == '.') != (verB[iB] == '.')) // If only one is at a period, the other is higher
				return NonZeroes(verA, iA, verB, iB); // (E.g. "1a.0" is less than "1a0.0")
			while (verA[iA] == '.' && verB[iB] == '.') // If they're both at a period, skip past it
			{
				iA++;
				iB++;
				if (iA >= verA.Length || iB >= verB.Length)
					return NonZeroes(verA, iA, verB, iB);
			}
		}

		static int NonZeroes(string verA, int iA, string verB, int iB)
		{
			// If one version ends but the other doesn't, then the other is higher,
			// unless the other has a period at its "i" and only more periods and zeroes after that
			// (E.g. "1.2a" is equal to "1.2a.0", but less than "1.2a.1", "1.2a.0b", and "1.2a0")
			if (iA < verA.Length && verA[iA] != '.' && iA > 0 && verA[iA-1] != '.')
				return 1;
			if (iB < verB.Length && verB[iB] != '.' && iB > 0 && verB[iB-1] != '.')
				return -1;
			for (; iA < verA.Length; iA++)
			{
				if (verA[iA] is not ('.' or '0'))
					return 1;
			}
			for (; iB < verB.Length; iB++)
			{
				if (verB[iB] is not ('.' or '0'))
					return -1;
			}
			return 0; // They both ended, or the other only has zeroes and periods afterwards
		}
	}

	// Checks for a given byte sequence in a stream
	public static bool StreamCheckSequence(Stream stream, ReadOnlySpan<byte> sequence) => StreamCheckSequence(stream, null, sequence);
	public static bool StreamCheckSequence(Stream stream, long? position, ReadOnlySpan<byte> sequence)
	{
		if (position < 0
		|| (stream.CanSeek && ((position ?? stream.Position) + sequence.Length) > stream.Length))
			return false; // The sequence does not fit in the stream

		if (position is not null)
		{
			try
				{stream.Position = (long)position;}
			catch (Exception e) when (e is IOException or NotSupportedException)
				{goto Failure;}
		}
		Span<byte> bytes = new byte[sequence.Length];
		try
			{stream.ReadExactly(bytes);}
		catch (Exception e) when (e is InvalidDataException or IOException)
			{goto Failure;}
		return MemoryExtensions.SequenceEqual(bytes, sequence);

Failure:
		Console.WriteLine();
		Prompts.WriteError("ERROR: Failed to check stream bytes!");
		Prompts.ExitPrompt();
		return false;
	}
}
