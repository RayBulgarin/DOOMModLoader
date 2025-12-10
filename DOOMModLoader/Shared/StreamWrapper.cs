using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

// A basic wrapper for a stream, that can optionally leave the base stream open when disposed

namespace DOOMModLoader.Shared;
class StreamWrapper(Stream baseStream, bool leaveOpen) : Stream
{
	public Stream BaseStream => baseStream;



	// Only dispose the underlying base stream if it shouldn't be left open
	protected override void Dispose(bool disposing)
	{
		if (disposing && !leaveOpen)
			baseStream.Dispose();

		base.Dispose(disposing);
	}

	// Only dispose the underlying base stream if it shouldn't be left open
	public override async ValueTask DisposeAsync()
	{
		if (!leaveOpen)
			await baseStream.DisposeAsync();

		await base.DisposeAsync();
	}

	// Use the underlying base stream's properties
	public override bool CanRead    => baseStream.CanRead;
	public override bool CanSeek    => baseStream.CanSeek;
	public override bool CanTimeout => baseStream.CanTimeout;
	public override bool CanWrite   => baseStream.CanWrite;
	public override long Length     => baseStream.Length;
	public override long Position
	{
		get => baseStream.Position;
		set => baseStream.Position = value;
	}
	public override int ReadTimeout
	{
		get => baseStream.ReadTimeout;
		set => baseStream.ReadTimeout = value;
	}
	public override int WriteTimeout
	{
		get => baseStream.WriteTimeout;
		set => baseStream.WriteTimeout = value;
	}

	// Use the underlying base stream's methods
	public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
		=> baseStream.BeginRead(buffer, offset, count, callback, state);
	public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
		=> baseStream.BeginWrite(buffer, offset, count, callback, state);
	//public override void Close() // Don't use the base stream's "Close" method
		//=> baseStream.Close();
	public override void CopyTo(Stream destination, int bufferSize)
		=> baseStream.CopyTo(destination, bufferSize);
	public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
		=> baseStream.CopyToAsync(destination, bufferSize, cancellationToken);
	[Obsolete]
	protected override WaitHandle CreateWaitHandle()
		=> base.CreateWaitHandle();
	public override int EndRead(IAsyncResult asyncResult)
		=> baseStream.EndRead(asyncResult);
	public override void EndWrite(IAsyncResult asyncResult)
		=> baseStream.EndWrite(asyncResult);
	public override void Flush()
		=> baseStream.Flush();
	public override Task FlushAsync(CancellationToken cancellationToken)
		=> baseStream.FlushAsync(cancellationToken);
	[Obsolete]
	protected override void ObjectInvariant()
		=> base.ObjectInvariant();
	public override int Read(Span<byte> buffer)
		=> baseStream.Read(buffer);
	public override int Read(byte[] buffer, int offset, int count)
		=> baseStream.Read(buffer, offset, count);
	public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		=> baseStream.ReadAsync(buffer, offset, count, cancellationToken);
	public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
		=> baseStream.ReadAsync(buffer, cancellationToken);
	public override int ReadByte()
		=> baseStream.ReadByte();
	public override long Seek(long offset, SeekOrigin origin)
		=> baseStream.Seek(offset, origin);
	public override void SetLength(long @value)
		=> baseStream.SetLength(@value);
	public override void Write(ReadOnlySpan<byte> buffer)
		=> baseStream.Write(buffer);
	public override void Write(byte[] buffer, int offset, int count)
		=> baseStream.Write(buffer, offset, count);
	public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		=> baseStream.WriteAsync(buffer, offset, count, cancellationToken);
	public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
		=> baseStream.WriteAsync(buffer, cancellationToken);
	public override void WriteByte(byte @value)
		=> baseStream.WriteByte(@value);
}
