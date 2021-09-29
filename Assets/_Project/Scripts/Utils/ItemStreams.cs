using System;
using System.Collections.Generic;
using MLAPI.Serialization;

public sealed class PooledFrameStream : NetworkBuffer, IDisposable {
    private bool isDisposed = false;

    internal PooledFrameStream () {
    }
    
    public static PooledFrameStream Get () {
        PooledFrameStream stream = FrameStreamPool.GetStream();
        stream.isDisposed = false;
        return stream;
    }
    
    public new void Dispose () {
        if(!isDisposed) {
            isDisposed = true;
            FrameStreamPool.PutBackInPool(this);
        }
    }
}

public static class FrameStreamPool {
    private static readonly Queue<PooledFrameStream> streams = new Queue<PooledFrameStream>();
    
    public static PooledFrameStream GetStream () {
        if(streams.Count == 0) {
            return new PooledFrameStream();
        }

        PooledFrameStream stream = streams.Dequeue();

        stream.SetLength(0);
        stream.Position = 0;

        return stream;
    }
    
    public static void PutBackInPool (PooledFrameStream stream) {
        streams.Enqueue(stream);
    }
}
