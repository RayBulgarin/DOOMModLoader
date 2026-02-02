using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

// An individual resource in a .index/.pindex container

namespace DOOMModLoader.Shared;
class ResourceArchiveEntry
{
	// Many resources share the same type, so create a list of types so that each resource can just store an index
	// SnapMap has 285 types by default (DOOM (2016) has 237, DOOM VFR has 167), so set the initial capacity to that
	// (All games combined would have 302, or 303 with DOOM (2016)'s demo)
	static readonly List<string> s_types = new(285) {"file"}; // Use "file" as the default resource type
	static readonly string s_lastType = "file";
	static readonly int s_lastTypeIndex = 0;



	public readonly ResourceArchive? Archive; // The container owning this resource
	public int Id; // Does not have to correspond to the resource order, nor be unique
	int _typeIndex = 0;
	public string Type // E.g. "perkGroups"
	{
		get => s_types[_typeIndex];
		set
		{
			if (value == s_lastType)
				_typeIndex = s_lastTypeIndex;
			else
			{
				_typeIndex = s_types.IndexOf(value);
				if (_typeIndex == -1)
				{
					_typeIndex = s_types.Count;
					s_types.Add(value);
				}
			}
		}
	}
	public string ShortName = ""; // E.g. "perkgroups/weapons/sp/pistol"
	public string FullName = ""; // E.g. "generated/decls/perkgroups/perkgroups/weapons/sp/pistol.decl" - Blank for resources with 0 length
	public long Offset; // Byte offset of the data in the .resources/.patch file
	public int Length; // Data size in bytes
	public int CompressedLength; // If this differs from the uncompressed length, the data is Deflate-compressed
	public int Zeroish; // Usually 0, but 16 for string blangs, "submission/orbis/save_data_icon(_profile).png", and all BSWFs and BSWF bimages
	public byte Patch; // Which .resources/.patch file the data is stored in

	// Class constructor. Optionally parses a BinaryReader to immediately set the resource's values
	public ResourceArchiveEntry(ResourceArchive? archive = null, BinaryReader? reader = null)
	{
		Archive = archive;
		if (reader is not null)
			Parse(reader);
	}

	// Gets a stream of this resource's decompressed data
	public Stream Open()
	{
		if (Archive is null)
		{
			Console.WriteLine();
			Prompts.WriteError("ERROR: Failed to open resource!");
			Prompts.WriteWarning($"\"{FullName}\" isn't associated with a container");
			Prompts.ExitPrompt();
			return null;
		}

		if (Length == 0) // No data to open
			return new MemoryStream(0);

		FileStream data = Archive.GetDataStream(Patch); // Not "using"

		if (Offset < 0 || (Offset + CompressedLength) > data.Length)
		{
			Console.WriteLine();
			Prompts.WriteError("ERROR: Failed to open resource!");
			Prompts.WriteWarning($"\"{FullName}\" ranges from 0x{Offset:X} to 0x{Offset + CompressedLength - 1:X},"
			+ $" but \"{Path.GetFileName(data.Name)}\" is only 0x{data.Length:X} bytes long!");
			Prompts.ExitPrompt();
			return null;
		}

		data.Position = Offset;

		if (CompressedLength == Length) // Return a non-disposable wrapper of the raw data stream for uncompressed data
			return new StreamWrapper(data, leaveOpen: true);
		else // Copy compressed data into a new DeflateStream
		{
			byte[] bytes = new byte[CompressedLength];
			try
				{data.ReadExactly(bytes);}
			catch (Exception e) when (e is InvalidDataException or IOException)
			{
				Console.WriteLine();
				Prompts.WriteError("ERROR: Failed to open resource!");
				Prompts.WriteWarning($"Failed to decompress \"{Path.GetFileName(data.Name)}\" > \"{FullName}\"");
				Prompts.ExitPrompt();
				return null;
			}
			MemoryStream memory = new(bytes);
			return new DeflateStream(memory, CompressionMode.Decompress);
		}
	}

	// Sets this resource's entry header values from a container data reader
	public void Parse(BinaryReader reader)
	{
		long start = -1;
		try
		{
			if (reader.BaseStream.CanSeek)
				start = reader.BaseStream.Position;
			Id               = reader.ReadBEInt32(); // Big-endian
			Type             = reader.ReadIdStr();
			ShortName        = reader.ReadIdStr();
			FullName         = reader.ReadIdStr();
			Offset           = reader.ReadBEInt64(); // Big-endian
			Length           = reader.ReadBEInt32(); // Big-endian
			CompressedLength = reader.ReadBEInt32(); // Big-endian
			Zeroish          = reader.ReadBEInt32(); // Big-endian
			Patch            = (byte)reader.ReadByte();
		}
		catch (Exception e) when (e is ArgumentOutOfRangeException or EndOfStreamException or IOException)
		{
			Console.WriteLine();
			if (start == -1)
				Prompts.WriteError("ERROR: Failed to parse resource!");
			else if (reader.BaseStream is FileStream fs)
				Prompts.WriteError($"ERROR: Failed to parse resource at 0x{start:X} of \"{Utility.HideUserName(fs.Name)}\"!");
			else
				Prompts.WriteError($"ERROR: Failed to parse resource at 0x{start:X}!");
			Prompts.ExitPrompt();
			return;
		}
	}

	// Writes this resource's entry header to a container stream
	public void WriteTo(Stream destination)
	{
		destination.Write([
			.. Utility.GetBEBytes( (int)Id), // Big-endian
			.. Utility.GetLEBytes( (int)Encoding.UTF8.GetByteCount(Type)), // Little-endian type length
			.. Encoding.UTF8.GetBytes(Type), // E.g. "perkGroups"
			.. Utility.GetLEBytes( (int)Encoding.UTF8.GetByteCount(ShortName)), // Little-endian short name length
			.. Encoding.UTF8.GetBytes(ShortName), // E.g. "perkgroups/weapons/sp/pistol"
			.. Utility.GetLEBytes( (int)Encoding.UTF8.GetByteCount(FullName)), // Little-endian full name length
			.. Encoding.UTF8.GetBytes(FullName), // E.g. "generated/decls/perkgroups/perkgroups/weapons/sp/pistol.decl"
			.. Utility.GetBEBytes((long)Offset),           // Big-endian
			.. Utility.GetBEBytes( (int)Length),           // Big-endian
			.. Utility.GetBEBytes( (int)CompressedLength), // Big-endian
			.. Utility.GetBEBytes( (int)Zeroish),          // Big-endian
			Patch, // Single byte
		]);
	}
}
