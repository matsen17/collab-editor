namespace CollabEditor.Utilities;

public sealed class AsyncLock : IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private bool _disposed;
    
    public async Task<IDisposable> LockAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        await _semaphore.WaitAsync();
        return new LockReleaser(_semaphore);
    }

    public IDisposable Lock()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _semaphore.Wait();
        return new LockReleaser(_semaphore);
    }
    
    public async Task<bool> DoIfNotLocked(Func<Task> action)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        
        if (!await _semaphore.WaitAsync(0))
        {
            return false;
        }

        try
        {
            await action();
            return true;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        
        _disposed = true;
        _semaphore.Dispose();
        GC.SuppressFinalize(this);
    }
    
    private sealed class LockReleaser(SemaphoreSlim semaphore) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            
            _disposed = true;
            semaphore.Release();
        }
    }
}