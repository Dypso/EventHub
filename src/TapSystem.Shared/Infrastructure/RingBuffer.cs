using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace TapSystem.Shared.Infrastructure;

public sealed class RingBuffer<T> where T : struct
{
    private readonly T[] _buffer;
    private readonly int _mask;
    private long _writePosition;
    private long _readPosition;

    public RingBuffer(int size)
    {
        if (!IsPowerOfTwo(size))
            throw new ArgumentException("Size must be a power of 2", nameof(size));

        _buffer = new T[size];
        _mask = size - 1;
        _writePosition = 0;
        _readPosition = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryWrite(in T item)
    {
        var currentWrite = _writePosition;
        var nextWrite = currentWrite + 1;
        
        if (nextWrite - _readPosition > _buffer.Length)
            return false;

        _buffer[currentWrite & _mask] = item;
        _writePosition = nextWrite;
        
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryRead(out T item)
    {
        var currentRead = _readPosition;
        
        if (currentRead >= _writePosition)
        {
            item = default;
            return false;
        }

        item = _buffer[currentRead & _mask];
        _readPosition = currentRead + 1;
        
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsPowerOfTwo(int x) => (x & (x - 1)) == 0;
}