using System;
using System.Collections.Generic;
using System.IO;
using System.Security;

// A resource container (.index/.pindex file, plus getters for .resources/.patch files)

namespace DOOMModLoader.Shared;
class ResourceArchive : IDisposable
{
	bool _disposed = false;
	readonly string _dataPath; // "[...]/DOOM 2016/base/(snap_)gameresources" - No "_002" or file extension
	Dictionary<int, FileStream> _dataStreams = []; // Open .resources/.patch file streams

	public List<ResourceArchiveEntry> Entries {get; private set;} = []; // Individual resources within the container
	// SnapMap has 91,907 resources by default (DOOM (2016) has 91,542, DOOM VFR has 41,672)



	// Class constructor. "path" should be a .index/.pindex file
	public ResourceArchive(string path)
	{
		FileStream stream;
		BinaryReader reader;
		try
		{
			stream = File.OpenRead(path);
			reader = new BinaryReader(stream);
			_dataPath = Path.GetFullPath(Path.ChangeExtension(path, null)); // Keep the path, but remove the extension
		}
		catch (Exception e) when (e is ArgumentException or DirectoryNotFoundException
		or FileNotFoundException or IOException or NotSupportedException
		or PathTooLongException or SecurityException or UnauthorizedAccessException)
		{
			Console.WriteLine();
			Prompts.WriteError($"ERROR: Failed to open \"{Utility.HideUserName(path)}\"!");
			Prompts.ExitPrompt();
			return;
		}
		// If "path" is a numbered patch file ("gameresources_002.pindex"), remove the number from "_dataPath"
		if (_dataPath.Length > 4
		&& _dataPath[^4] == '_'
		&& (_dataPath[^3] is >= '0' and <= '9')
		&& (_dataPath[^2] is >= '0' and <= '9')
		&& (_dataPath[^1] is >= '0' and <= '9'))
			_dataPath = _dataPath[0 .. ^4];

		try
		{
			if (!Utility.StreamCheckSequence(stream, [0x05, (byte)'S', (byte)'E', (byte)'R'])) // Reverse "RESources v5" magic
			{
				Console.WriteLine();
				Prompts.WriteError($"ERROR: \"{Utility.HideUserName(path)}\" doesn't start with \"[0x05]SER\"!");
				Prompts.ExitPrompt();
				return;
			}
			if (stream.Length < 0x24
			|| reader.ReadBEInt32() != stream.Length - 0x20 // Big-endian - Ensure that the file size is correct
			|| MemoryExtensions.ContainsAnyExcept<byte>(reader.ReadBytes(0x18), 0)) // Expect zeroes from 0x8 through 0x1F
			{
				Console.WriteLine();
				Prompts.WriteError($"ERROR: \"{Utility.HideUserName(path)}\" has an invalid header!");
				if (Path.GetFileName(path).EqualsOrdinalIgnoreCase("master.index"))
				{
					string extension = "pindex";
					if (!File.Exists(Path.Join(Path.GetDirectoryName(path), "gameresources.pindex"))
					&& File.Exists(Path.Join(Path.GetDirectoryName(path), "gameresources.index")))
						extension = "index"; // DOOM VFR doesn't have a patch container

					Prompts.WriteWarning($"(\"master.index\" is not a valid resource container. Use \"gameresources.{extension}\" instead)");
				}
				Prompts.ExitPrompt();
				return;
			}

			int expectedCount = reader.ReadBEInt32(); // Big-endian
			Entries.Capacity = (expectedCount + 256); // Leave room for mods to add new resources. Not a hard cap
			for (int i = 0; i < expectedCount; i++)
				Entries.Add(new ResourceArchiveEntry(archive: this, reader: reader));
			if (stream.Position != stream.Length)
			{
				Console.WriteLine();
				Prompts.WriteError($"ERROR: \"{Utility.HideUserName(path)}\" has an invalid header!");
				Prompts.ExitPrompt();
				return;
			}
		}
		catch (Exception e) when (e is ArgumentOutOfRangeException or IOException)
		{
			Console.WriteLine();
			try
				{Prompts.WriteError($"ERROR: Failed to parse resource near 0x{stream.Position:X} of \"{Utility.HideUserName(path)}\"!");}
			catch (IOException)
				{Prompts.WriteError("ERROR: Failed to parse resource!");}
			Prompts.ExitPrompt();
			return;
		}

		reader.Dispose();
		stream.Dispose();
	}

	// Returns a FileStream for the .resources/.patch file for the given patch number
	// The returned stream should not be disposed
	public FileStream GetDataStream(int patch)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);

#pragma warning disable IDE0018 // "Variable declaration can be inlined"
		FileStream? stream; // Not "using"
#pragma warning restore IDE0018
		if (_dataStreams.TryGetValue(patch, out stream)) // If this patch's data is already open, return it
			return stream;

		// Otherwise, figure out which file to open, and open it
		string filePath;
		if (patch == 0)
			filePath = $"{_dataPath}.resources";        // "[...]/gameresources.resources"
		else if (patch == 1)
			filePath = $"{_dataPath}.patch";            // "[...]/gameresources.patch"
		else
			filePath = $"{_dataPath}_{patch:D3}.patch"; // "[...]/gameresources_002.patch"

		try
		{
			stream = File.OpenRead(filePath); // Not "using"
			if (!Utility.StreamCheckSequence(stream, [0x05, (byte)'S', (byte)'E', (byte)'R'])) // Reverse "RESources v5" magic
			{
				Console.WriteLine();
				Prompts.WriteError($"ERROR: \"{Utility.HideUserName(filePath)}\" has an invalid header!");
				Prompts.ExitPrompt();
				return null;
			}
			_dataStreams.Add(patch, stream);
			return stream;
		}
		catch (Exception e) when (e is IOException or UnauthorizedAccessException)
		{
			Console.WriteLine();
			Prompts.WriteError($"ERROR: Failed to open \"{Utility.HideUserName(filePath)}\"!");
			Prompts.ExitPrompt();
			return null;
		}
	}

	// Writes this container (.index/.pindex file) to the given file path
	// Does not write resource data/contents (.resources/.patch), only their headers (.index/.pindex)
	public void Save(string path)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);

		try
		{
			using FileStream stream = File.Create(path);
			stream.Write([ // Write the container header
				0x05, (byte)'S', (byte)'E', (byte)'R', // Reverse "RESources v5" magic
				0x00, 0x00, 0x00, 0x00, // Big-endian resource headers size - Will be updated later
				0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // Always zero
				.. Utility.GetBEBytes((int)Entries.Count), // Big-endian
			]);
			foreach (ResourceArchiveEntry entry in Entries) // Write the resource headers
				entry.WriteTo(stream);
			stream.Position = 0x4; // Go back to update the resource headers size
			stream.Write(Utility.GetBEBytes((int)stream.Length - 0x20)); // Big-endian
		}
		catch (Exception e) when (e is DirectoryNotFoundException or IOException or NotSupportedException or PathTooLongException or UnauthorizedAccessException)
		{
			Console.WriteLine();
			Prompts.WriteError($"ERROR: Failed to save \"{Utility.HideUserName(path)}\"!");
			Prompts.ExitPrompt();
			return;
		}
	}

	// Cascade dispose all open file streams
	public void Dispose() => Dispose(true);
	protected void Dispose(bool disposing)
	{
		if (_disposed)
			return;

		if (disposing)
		{
			foreach (FileStream stream in _dataStreams.Values)
				stream.Dispose();
			_dataStreams = null!;
			Entries = null!;
		}

		_disposed = true;
	}
}
