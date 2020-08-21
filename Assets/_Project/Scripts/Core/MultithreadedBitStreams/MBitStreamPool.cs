using System;
using System.Collections.Generic;
using MLAPI.Logging;
using UnityEngine;

/// <summary>
/// Static class containing MPooledBitStreams
/// </summary>
public static class MBitStreamPool {
    private static byte createdStreams = 0;
    private static readonly Queue<WeakReference> overflowStreams = new Queue<WeakReference>();
    private static readonly Queue<MPooledBitStream> streams = new Queue<MPooledBitStream>();

    private static object getStreamLock = new object();
    private static object returnStreamLock = new object();

    /// <summary>
    /// Retrieves an expandable MPooledBitStream from the pool
    /// </summary>
    /// <returns>An expandable MPooledBitStream</returns>
    public static MPooledBitStream GetStream () {
        lock(getStreamLock) {
            if(streams.Count == 0) {
                if(overflowStreams.Count > 0) {
                    Debug.Log("Retrieving MPooledBitStream from overflow pool. Recent burst?");

                    object weakStream = null;
                    while(overflowStreams.Count > 0 && ((weakStream = overflowStreams.Dequeue().Target) == null));

                    if(weakStream != null) {
                        MPooledBitStream strongStream = (MPooledBitStream)weakStream;

                        strongStream.SetLength(0);
                        strongStream.Position = 0;

                        return strongStream;
                    }
                }

                if(createdStreams == 254) {
                    Debug.LogWarning("255 streams have been created. Did you forget to dispose?");
                } else if(createdStreams < 255)
                    createdStreams++;

                return new MPooledBitStream();
            }

            MPooledBitStream stream = streams.Dequeue();

            stream.SetLength(0);
            stream.Position = 0;

            return stream;
        }
    }

    /// <summary>
    /// Puts a MPooledBitStream back into the pool
    /// </summary>
    /// <param name="stream">The stream to put in the pool</param>
    public static void PutBackInPool (MPooledBitStream stream) {
        lock(returnStreamLock) {
            if(streams.Count > 16) {
                // The user just created lots of streams without returning them in between.
                // Streams are essentially byte array wrappers. This is valuable memory.
                // Thus we put this stream as a weak reference incase of another burst
                // But still leave it to GC
                Debug.Log("Putting MPooledBitStream into overflow pool. Did you forget to dispose?");
                overflowStreams.Enqueue(new WeakReference(stream));
            } else {
                streams.Enqueue(stream);
            }
        }
    }
}
