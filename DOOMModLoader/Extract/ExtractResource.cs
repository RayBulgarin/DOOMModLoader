using DOOMModLoader.Shared;
using System;
using System.IO;
using System.IO.Compression;

// Exports an individual resource to a loose file

namespace DOOMModLoader.Extract;
static class ExtractResource
{
	// Returns a span with CRLF (\r\n) newlines converted to LF (\n), modifying the passed span
	public static Span<byte> HandleCrLf(Span<byte> bytes)
	{
		int newlines = 0;
		for (int i = 0; i < (bytes.Length - 1); i++)
		{
			if (bytes[i] == '\r' && bytes[i+1] == '\n')
				newlines++;
			else
				bytes[i-newlines] = bytes[i];
		}
		bytes[^(newlines + 1)] = bytes[^1];
		return bytes[0 .. ^newlines];
	}

	// Extracts an individual resource to a loose file. Returns true on success and false on failure
	public static bool WriteFile(ResourceArchiveEntry entry, bool convertCrLf, bool isDemo)
	{
		const int bufferSize = 81920; // The default buffer size from "Stream.CopyTo"

		try
		{
			using Stream input = entry.Open();
			string filePath = Path.Join(Config.Cli.Out, entry.FullName);
			Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
			using FileStream output = File.Create(filePath);

			// CRLF-to-LF-converted resources
			if (convertCrLf && (Path.GetExtension(entry.FullName) is ".decl" // ".entities" is already LF-only
			or ".aas_botplayer" or ".aas_monster16" or ".aas_monster24" or ".aas_monster48" or ".aas_monster96"
			or ".aas_monster128" or ".aas_monster256" or ".aas_player" or ".inc" or ".json" or ".md6rig" or ".proc"))
			{
				Span<byte> bytes = new byte[entry.Length]; // Should be less than 5 megabytes
				input.ReadExactly(bytes);
				output.Write(HandleCrLf(bytes));
				return true;
			}

			// Decrypt encrypted resources
			if (entry.Type == "binaryFile")
			{
				Span<byte> bytes = new byte[entry.Length]; // Should be less than 5 megabytes
				input.ReadExactly(bytes);
				IdCrypt.Decrypted? dec = IdCrypt.Decrypt(bytes, entry.ShortName);
				if (dec is null)
				{
					if (isDemo) // Todo: DOOM (2016)'s demo's encryption is unsupported. Extract the raw data for now
						goto Raw;
					return false;
				}
				bytes = dec.Data;

				if (convertCrLf)
				{
					bytes = HandleCrLf(bytes);
					if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
						bytes = bytes[3 .. ^0]; // Skip the UTF-8 byte order mark
				}
				output.Write(bytes);
				return true;
			}

			// Raw/Non-CRLF-to-LF-converted resources
Raw:
			if (input is DeflateStream) // If it's a per-resource stream, "Stream.CopyTo" works for us
				input.CopyTo(output);
			else if (entry.Length <= bufferSize) // Otherwise, we have to handle it ourselves
			{
				Span<byte> bytes = new byte[entry.Length];
				input.ReadExactly(bytes);
				output.Write(bytes);
			}
			else
			{
				byte[] bytes = new byte[bufferSize];
				int readTotal = 0;
				while (readTotal < entry.Length)
				{
					int readNow = input.Read(bytes, 0, Math.Min(entry.Length - readTotal, bufferSize));
					if (readNow == 0)
						return false;
					output.Write(bytes, 0, readNow);
					readTotal += readNow;
				}
			}
			return true;
		}
		catch (Exception e) when (e is DirectoryNotFoundException or InvalidDataException
		or IOException or NotSupportedException or PathTooLongException or UnauthorizedAccessException)
			{return false;}
	}
}
