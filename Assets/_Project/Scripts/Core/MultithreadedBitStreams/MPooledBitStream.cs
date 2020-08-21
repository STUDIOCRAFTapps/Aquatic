using System;

/// <summary>
/// Disposable MBitStream that returns the Stream to the MBitStreamPool when disposed
/// </summary>
public sealed class MPooledBitStream : MLAPI.Serialization.BitStream, IDisposable {
    private bool isDisposed = false;

    internal MPooledBitStream () {
    }

    /// <summary>
    /// Gets a MPooledBitStream from the static MBitStreamPool
    /// </summary>
    /// <returns>MPooledBitStream</returns>
    public static MPooledBitStream Get () {
        MPooledBitStream stream = MBitStreamPool.GetStream();
        stream.isDisposed = false;
        return stream;
    }

    /// <summary>
    /// Returns the MPooledBitStream into the static MBitStreamPool
    /// </summary>
    public new void Dispose () {
        if(!isDisposed) {
            isDisposed = true;
            MBitStreamPool.PutBackInPool(this);
        }
    }
}
