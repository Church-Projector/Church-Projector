using System.Collections.Concurrent;
using System.IO;

namespace ChurchProjector.Classes;
internal class MemoryStreamPool
{
    public static readonly MemoryStreamPool Shared = new MemoryStreamPool();

    private readonly ConcurrentBag<MemoryStream> _pool = new ConcurrentBag<MemoryStream>();

    public MemoryStream GetStream()
    {
        if (_pool.TryTake(out var ms))
        {
            ms.SetLength(0);
            return ms;
        }

        return new MemoryStream();
    }

    public void ReturnStream(MemoryStream ms)
    {
        _pool.Add(ms);
    }
}