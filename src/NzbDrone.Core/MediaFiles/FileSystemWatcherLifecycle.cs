using System;
using System.Collections.Concurrent;

namespace NzbDrone.Core.MediaFiles
{
    public static class FileSystemWatcherLifecycle
    {
        public static bool TryEnable<TValue>(
            ConcurrentDictionary<string, TValue> watchers,
            string path,
            TValue watcher,
            Func<bool> isStopped,
            Action<TValue> dispose)
        {
            if (isStopped())
            {
                dispose(watcher);
                return false;
            }

            if (!watchers.TryAdd(path, watcher))
            {
                dispose(watcher);
                return false;
            }

            if (isStopped())
            {
                watchers.TryRemove(path, out var removed);
                dispose(watcher);
                return false;
            }

            return true;
        }
    }
}
