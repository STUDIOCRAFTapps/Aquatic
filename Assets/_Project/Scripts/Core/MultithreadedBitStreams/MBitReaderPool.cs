using System.Collections.Generic;
using System.IO;
using MLAPI.Logging;
using UnityEngine;

/// <summary>
/// Static class containing MPooledBitReaders
/// </summary>
public static class MBitReaderPool {
    private static byte createdReaders = 0;
    private static readonly Queue<MPooledBitReader> readers = new Queue<MPooledBitReader>();

    private static object getReaderLock = new object();
    private static object returnReaderLock = new object();

    /// <summary>
    /// Retrieves a MPooledBitReader
    /// </summary>
    /// <param name="stream">The stream the reader should read from</param>
    /// <returns>A MPooledBitReader</returns>
    public static MPooledBitReader GetReader (Stream stream) {
        lock(getReaderLock) {
            if(readers.Count == 0) {
                if(createdReaders == 254) {
                    Debug.LogWarning("255 readers have been created. Did you forget to dispose?");
                } else if(createdReaders < 255)
                    createdReaders++;

                return new MPooledBitReader(stream);
            }

            MPooledBitReader reader = readers.Dequeue();
            reader.SetStream(stream);

            return reader;
        }
    }

    /// <summary>
    /// Puts a MPooledBitReader back into the pool
    /// </summary>
    /// <param name="reader">The reader to put in the pool</param>
    public static void PutBackInPool (MPooledBitReader reader) {
        lock(returnReaderLock) {
            if(readers.Count < 64)
                readers.Enqueue(reader);
            else 
                Debug.LogWarning("MBitReaderPool already has 64 queued. Throwing to GC. Did you forget to dispose?");
        }
    }
}