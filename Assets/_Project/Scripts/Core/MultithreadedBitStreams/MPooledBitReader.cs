using System;
using System.IO;

/// <summary>
/// Disposable MBitReader that returns the Reader to the MBitReaderPool when disposed
/// </summary>
public sealed class MPooledBitReader : MLAPI.Serialization.BitReader, IDisposable {
    private bool isDisposed = false;

    internal MPooledBitReader (Stream stream) : base(stream) {
    }

    /// <summary>
    /// Gets a MPooledBitReader from the static MBitReaderPool
    /// </summary>
    /// <returns>MPooledBitReader</returns>
    public static MPooledBitReader Get (Stream stream) {
        MPooledBitReader reader = MBitReaderPool.GetReader(stream);
        reader.isDisposed = false;
        return reader;
    }

    /// <summary>
    /// Returns the MPooledBitReader into the static MBitReaderPool
    /// </summary>
    public void Dispose () {
        if(!isDisposed) {
            isDisposed = true;
            MBitReaderPool.PutBackInPool(this);
        }
    }
}