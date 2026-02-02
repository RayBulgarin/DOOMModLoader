using DOOMModLoader.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

// Parses "fileids.txt" files and sets resource IDs

namespace DOOMModLoader.LoadMods;
static class HandleFileIds
{
	static readonly Dictionary<string, (int Id, string Mod)> newIds = [];



	// Parses (a chunk of) a mod's "fileids.txt" file
	// Returns how far we've successfully read into the span, for buffer refill reasons
	static int ParseIds(ReadOnlySpan<byte> bytes)
	{
		int i = 0;

		while (true)
		{
			int newline = MemoryExtensions.IndexOfAny(bytes[i .. ^0], (byte)'\n', (byte)'\r');
			if (newline == -1) // No more full lines
				return i;

			int equal = MemoryExtensions.LastIndexOf(bytes[i .. (i+newline)], (byte)'=');
			if (equal == -1) // This line doesn't set an ID
			{
				i += (newline + 1);
				continue;
			}

			// This line appears to set a resource's ID
			string name = Encoding.UTF8.GetString(bytes[i .. (i+equal)]).Trim().Replace('\\', '/').ToLowerInvariant();
#pragma warning disable IDE0018 // "Variable declaration can be inlined"
			long id;
#pragma warning restore IDE0018
			if (string.IsNullOrEmpty(name) || !long.TryParse(bytes[(i+equal+1) .. (i+newline)], out id))
			{
				Console.WriteLine();
				Prompts.WriteError("ERROR: Failed to install mods!");
				Prompts.WriteWarning("\"fileids.txt\": Failed to parse line:");
				Prompts.WriteWarning($"    {Encoding.UTF8.GetString(bytes[i .. (i+newline)])}");
				Prompts.ExitPrompt();
				return -1;
			}
			else if (id < int.MinValue || id > 0xFFFFFFFF) // Support down to -2147483648 for legacy reasons
			{
				Console.WriteLine();
				Prompts.WriteError("ERROR: Failed to install mods!");
				Prompts.WriteWarning($"\"fileids.txt\": \"{name}\" must be set to an ID between 0 and 4294967295");
				Prompts.ExitPrompt();
				return -1;
			}
			else if (!newIds.TryAdd(name, ((int)id, HandleMods.Data.CurrentMod)) && newIds[name].Id != (int)id)
				HandleWarnings.AddModConflicts("fileids.txt (ID conflict)"); // This resource's ID was already set

			i += (newline + 1);
			continue;
		}
	}

	// Loads a mod's "fileids.txt" file, storing the IDs for later
	public static void LoadFileIds(Stream source, int length)
	{
		const int bufferSize = 81920; // The default buffer size from "Stream.CopyTo"

		try
		{
			int readTotal = 0;
			if (length <= bufferSize-1)
			{
				Span<byte> bytes = new byte[length+1];
				source.ReadExactly(bytes[0 .. ^1]);
				bytes[^1] = (byte)'\n';
				readTotal = ParseIds(bytes);
			}
			else
			{
				Span<byte> bytes = new byte[bufferSize];
				source.ReadExactly(bytes);
				while (true)
				{
					int read = ParseIds(bytes);
					if (read == 0) // There are "bufferSize" bytes without a newline? Just abort
						goto Failure;

					readTotal += read;
					bytes[read .. ^0].CopyTo(bytes);

					if ((length - readTotal) > bufferSize-1)
						source.ReadExactly(bytes[^read .. ^0]);
					else // The rest of the file can fit in one more buffer read
					{
						bytes = bytes[0 .. (length+1 - readTotal)];
						source.ReadExactly(bytes[(bufferSize - read) .. ^1]);
						bytes[^1] = (byte)'\n';
						readTotal += ParseIds(bytes);
						break;
					}
				}
			}
			Utility.Assert(readTotal == length+1, $"LoadFileIds: readTotal ({readTotal}) != length+1 ({length+1})");

			Prompts.WriteVerbose("          Parsed fileids.txt");
		}
		catch (Exception e) when (e is InvalidDataException or IOException)
			{goto Failure;}
		return;

Failure:
		Console.WriteLine();
		Prompts.WriteError("ERROR: Failed to install mods!");
		Prompts.WriteWarning("Failed to parse \"fileids.txt\"");
		Prompts.ExitPrompt();
		return;
	}

	// Sets all resource IDs that have been specified, and stores warnings about unfound resources
	public static void ApplyFileIds(ResourceArchive container)
	{
		foreach (KeyValuePair<string, (int Id, string Mod)> newId in newIds)
		{
			List<ResourceArchiveEntry> entries = container.Entries.FindAll(x => x.FullName == newId.Key);
			if (entries.Count == 0 && !HandleMods.Data.ModResources.Contains(newId.Key))
				HandleWarnings.AddFileIdsMissingResources(newId.Value.Mod, newId.Key);
			else
			{
				foreach (ResourceArchiveEntry entry in entries)
					entry.Id = newId.Value.Id;
			}
		}
		// Todo: Support resources even if their full names were emptied due to being replaced with an empty file
		// Currently, the "!HandleMods.Data.ModResources.Contains" check is to just avoid showing a warning for it
	}
}
