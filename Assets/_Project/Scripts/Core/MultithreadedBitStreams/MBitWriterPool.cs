using System.Collections.Generic;
using System.IO;
using MLAPI.Logging;
using UnityEngine;

/// <summary>
/// Static class containing MPooledBitWriters
/// </summary>
public static class MBitWriterPool {
    private static byte createdWriters = 0;
    private static readonly Queue<MPooledBitWriter> writers = new Queue<MPooledBitWriter>();

    private static object getWriterLock = new object();
    private static object returnWriterLock = new object();

    /// <summary>
    /// Retrieves a MPooledBitWriter
    /// </summary>
    /// <param name="stream">The stream the writer should write to</param>
    /// <returns>A MPooledBitWriter</returns>
    public static MPooledBitWriter GetWriter (Stream stream) {
        lock(getWriterLock) {
            if(writers.Count == 0) {
                if(createdWriters == 254) {
                    Debug.LogWarning("255 writers have been created. Did you forget to dispose?");
                } else if(createdWriters < 255)
                    createdWriters++;

                return new MPooledBitWriter(stream);
            }

            MPooledBitWriter writer = writers.Dequeue();
            writer.SetStream(stream);

            return writer;
        }
    }

    /// <summary>
    /// Puts a MPooledBitWriter back into the pool
    /// </summary>
    /// <param name="writer">The writer to put in the pool</param>
    public static void PutBackInPool (MPooledBitWriter writer) {
        lock(returnWriterLock) {
            if(writers.Count < 64)
                writers.Enqueue(writer);
            else
                Debug.Log("MBitWriterPool already has 64 queued. Throwing to GC. Did you forget to dispose?");
        }
    }
}
