using System;
using System.IO;

/// <summary>
/// Disposable MBitWriter that returns the Writer to the MBitWriterPool when disposed
/// </summary>
public sealed class MPooledBitWriter : MLAPI.Serialization.BitWriter, IDisposable {
    private bool isDisposed = false;

    internal MPooledBitWriter (Stream stream) : base(stream) {
    }

    /// <summary>
    /// Gets a MPooledBitWriter from the static MBitWriterPool
    /// </summary>
    /// <returns>MPooledBitWriter</returns>
    public static MPooledBitWriter Get (Stream stream) {
        MPooledBitWriter writer = MBitWriterPool.GetWriter(stream);
        writer.isDisposed = false;
        return writer;
    }

    /// <summary>
    /// Returns the MPooledBitWriter into the static MBitWriterPool
    /// </summary>
    public void Dispose () {
        if(!isDisposed) {
            isDisposed = true;
            MBitWriterPool.PutBackInPool(this);
        }
    }
}
