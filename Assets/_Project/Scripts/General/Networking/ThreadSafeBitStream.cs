using MLAPI.Serialization.Pooled;
using System.IO;

public static class ThreadSafeBitStream {

    private static object getStreamLock = new object();
    private static object returnStreamLock = new object();

    private static object getReaderLock = new object();
    private static object returnReaderLock = new object();

    private static object getWriterLock = new object();
    private static object returnWriterLock = new object();

    public static PooledBitStream GetStreamSafe () {
        PooledBitStream s;
        lock(getStreamLock) {
            s = PooledBitStream.Get();
        }
        return s;
    }

    public static void DisposeStreamSafe (this PooledBitStream stream) {
        lock(returnStreamLock) {
            stream.Dispose();
        }
    }

    public static PooledBitReader GetReaderSafe (Stream stream) {
        PooledBitReader r;
        lock(getReaderLock) {
            r = PooledBitReader.Get(stream);
        }
        return r;
    }

    public static void DisposeReaderSafe (this PooledBitReader reader) {
        lock(returnReaderLock) {
            reader.Dispose();
        }
    }

    public static PooledBitWriter GetWriterSafe (Stream stream) {
        PooledBitWriter w;
        lock(getWriterLock) {
            w = PooledBitWriter.Get(stream);
        }
        return w;
    }

    public static void DisposeWriterSafe (this PooledBitWriter reader) {
        lock(returnWriterLock) {
            reader.Dispose();
        }
    }
}
