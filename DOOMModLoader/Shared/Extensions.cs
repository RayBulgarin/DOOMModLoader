using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;

// Miscellaneous programming shortcuts

namespace DOOMModLoader.Shared;
static class Extensions
{
	// Different string methods default to different cultural/ordinal comparisons
	// Therefore, it can be good practice to explicitly state the desired comparison type
	// A case-insensitive "abc.Equals" is preferable over "abc == xyz.ToLowerInvariant", too
	extension(string str)
	{
		public bool ContainsOrdinal(char @value)
			=> str.Contains(@value, StringComparison.Ordinal);
		public bool ContainsOrdinal(string @value)
			=> str.Contains(@value, StringComparison.Ordinal);
		public bool EndsWithOrdinal(string @value)
			=> str.EndsWith(@value, StringComparison.Ordinal);
		public bool EqualsOrdinalIgnoreCase(string @value)
			=> str.Equals(@value, StringComparison.OrdinalIgnoreCase);
		public int IndexOfOrdinal(char @value)
			=> str.IndexOf(@value, StringComparison.Ordinal);
		public string ReplaceOrdinal(string oldValue, string newValue)
			=> str.Replace(oldValue, newValue, StringComparison.Ordinal);
		public bool StartsWithOrdinal(string @value)
			=> str.StartsWith(@value, StringComparison.Ordinal);
		public bool StartsWithOrdinalIgnoreCase(string @value)
			=> str.StartsWith(@value, StringComparison.OrdinalIgnoreCase);
	}

	// Similarly for string lists
	public static bool ContainsOrdinalIgnoreCase(this List<string> list, string item)
		=> list.Exists(x => x.EqualsOrdinalIgnoreCase(item));

	// BinaryReader only exposes little-endian reading methods,
	// but resource containers have both big- and little-endian values
	extension(BinaryReader reader)
	{
		public  int ReadBEInt32() => BinaryPrimitives.ReadInt32BigEndian(reader.ReadBytes(4));
		public long ReadBEInt64() => BinaryPrimitives.ReadInt64BigEndian(reader.ReadBytes(8));
		public  int ReadLEInt32() => reader.ReadInt32();
		public long ReadLEInt64() => reader.ReadInt64();
		public string ReadIdStr()
		{
			int length = reader.ReadLEInt32(); // Little-endian string length
			return Encoding.UTF8.GetString(reader.ReadBytes(length));
		}
	}

	// HttpClient has convenience wrappers for "SendAsync", but not for "Send", so make our own convenience wrapper
	public static HttpResponseMessage Get(this HttpClient client, string? requestUri)
		=> client.Send(new HttpRequestMessage(HttpMethod.Get, requestUri));
}
