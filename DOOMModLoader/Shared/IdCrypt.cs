using System;
using System.Security.Cryptography;
using System.Text;

// Decrypts and encrypts certain binary resources
// Special thanks to emoose for figuring out the encryption: https://github.com/emoose/DOOMExtract/tree/master/idCrypt

namespace DOOMModLoader.Shared;
static class IdCrypt
{
	static readonly Aes aes = Aes.Create(); // Whether loading mods or extracting resources, we use AES-128 multiple times

	internal const int HeaderSize = 0xC + 0x10 + 0x20; // The salt, AES IV, and HMAC hash length combined

	public class Decrypted
	{
		public required byte[] Salt;     // 0xC bytes
		public required byte[] AesIv;    // 0x10 bytes
		public required byte[] Data;     // Any amount of bytes
		public required byte[] HmacHash; // 0x20 bytes
	}



	// Decrypts certain binary files. Returns null on failure
	public static Decrypted? Decrypt(ReadOnlySpan<byte> input, string key)
	{
		if (input.Length < HeaderSize)
			return null;

		Decrypted result = new()
		{
			Salt     = input[ 0x00 ..  0x0C].ToArray(),
			AesIv    = input[ 0x0C ..  0x1C].ToArray(),
			Data     = input[ 0x1C .. ^0x20].ToArray(),
			HmacHash = input[^0x20 .. ^0x00].ToArray(),
		};

		// Get a SHA-256 hash of the salt, "swapTeam\n" plus null byte, and the decryption key
		ReadOnlySpan<byte> shaHash = SHA256.HashData([
			.. result.Salt,
			.. "swapTeam\n\0"u8,
			.. Encoding.UTF8.GetBytes(key),
		]);

		// Get a HMAC hash from the salt, AES IV, and encrypted data
		// This is to check whether the key is correct; otherwise, we'd just successfully get garbage bytes
		ReadOnlySpan<byte> hmacHash = HMACSHA256.HashData(shaHash, input[0 .. ^0x20]);
		if (!MemoryExtensions.SequenceEqual(hmacHash, result.HmacHash))
			return null;

		// Decrypt the data
		aes.Key = shaHash[0 .. 0x10].ToArray(); // Only use the first 0x10 bytes of the SHA hash here; this is AES-128
		try
			{result.Data = aes.DecryptCbc(result.Data, result.AesIv);}
		catch (CryptographicException)
			{return null;}

		return result;
	}

	// Encrypts certain binary files
	public static byte[] Encrypt(Decrypted input, string key)
	{
		Utility.Assert(
			input.Salt.Length == 0xC && input.AesIv.Length == 0x10,
			"IdCrypt.Encrypt: Salt.Length != 0xC or AesIv.Length != 0x10"
		);

		// Get a SHA-256 hash of the salt, "swapTeam\n" plus null byte, and the encryption key
		ReadOnlySpan<byte> shaHash = SHA256.HashData([
			.. input.Salt,
			.. "swapTeam\n\0"u8,
			.. Encoding.UTF8.GetBytes(key),
		]);

		// Encrypt the data
		aes.Key = shaHash[0 .. 0x10].ToArray(); // Only use the first 0x10 bytes of the SHA hash here; this is AES-128
		ReadOnlySpan<byte> encrypted = aes.EncryptCbc(input.Data, input.AesIv);

		// Get a HMAC hash from the salt, AES IV, and encrypted data
		ReadOnlySpan<byte> hmacHash = HMACSHA256.HashData(shaHash, [
			.. input.Salt,
			.. input.AesIv,
			.. encrypted,
		]);

		// Return the salt, AES IV, encrypted data, and HMAC hash as one byte array
		return [
			.. input.Salt,
			.. input.AesIv,
			.. encrypted,
			.. hmacHash,
		];
	}
}
