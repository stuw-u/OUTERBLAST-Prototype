using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

unsafe public struct BlitableArray<T> : IDisposable where T : struct {
    [NativeDisableUnsafePtrRestriction]
    private void* m_Buffer;
    private int m_Length;
    private Allocator m_AllocatorLabel;

    public BlitableArray (int size, Allocator allocator) {
        m_AllocatorLabel = allocator;
        m_Length = size;
        var elementSize = UnsafeUtility.SizeOf<T>();
        m_Buffer = UnsafeUtility.Malloc(size * elementSize, UnsafeUtility.AlignOf<T>(), allocator);
    }

    unsafe public void Dispose () {
        UnsafeUtility.Free(m_Buffer, m_AllocatorLabel);
    }

    unsafe public T this[int index] {
        get {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if(m_AllocatorLabel == Allocator.Invalid)
                throw new ArgumentException("AutoGrowArray was not initialized.");

            if(index >= Length)
                throw new IndexOutOfRangeException();
#endif

            return UnsafeUtility.ReadArrayElement<T>(m_Buffer, index);
        }

        set {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if(m_AllocatorLabel == Allocator.Invalid)
                throw new ArgumentException("AutoGrowArray was not initialized.");

            if(index >= Length)
                throw new IndexOutOfRangeException();
#endif
            UnsafeUtility.WriteArrayElement(m_Buffer, index, value);
        }
    }

    unsafe public void CopyFrom (BlitableArray<T> from) {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        if(from.m_Length != m_Length)
            throw new ArgumentException("Both array must have the same size.");
#endif

        UnsafeUtility.MemCpy(from.m_Buffer, this.m_Buffer, m_Length);
    }

    public T[] ToArray () {
        var res = new T[Length];

        for(var i = 0; i < Length; i++)
            res[i] = this[i];

        return res;
    }

    public static implicit operator T[] (BlitableArray<T> array) {
        return array.ToArray();
    }

    public int Length { get { return m_Length; } }
}