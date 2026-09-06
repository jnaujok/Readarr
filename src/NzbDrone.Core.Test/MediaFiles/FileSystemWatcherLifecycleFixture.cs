using System.Collections.Concurrent;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.MediaFiles;

namespace NzbDrone.Core.Test.MediaFiles
{
    [TestFixture]
    public class FileSystemWatcherLifecycleFixture
    {
        [Test]
        public void does_not_add_when_stopped()
        {
            var watchers = new ConcurrentDictionary<string, object>();
            var disposed = false;
            var watcher = new object();

            var added = FileSystemWatcherLifecycle.TryEnable(watchers, "/books", watcher, () => true, _ => disposed = true);

            added.Should().BeFalse();
            watchers.Should().BeEmpty();
            disposed.Should().BeTrue();
        }

        [Test]
        public void adds_when_running()
        {
            var watchers = new ConcurrentDictionary<string, object>();
            var watcher = new object();

            var added = FileSystemWatcherLifecycle.TryEnable(watchers, "/books", watcher, () => false, _ => { });

            added.Should().BeTrue();
            watchers["/books"].Should().BeSameAs(watcher);
        }

        [Test]
        public void duplicate_path_disposes_new_watcher()
        {
            var existing = new object();
            var watchers = new ConcurrentDictionary<string, object>();
            watchers.TryAdd("/books", existing);
            var disposed = false;
            var watcher = new object();

            var added = FileSystemWatcherLifecycle.TryEnable(watchers, "/books", watcher, () => false, _ => disposed = true);

            added.Should().BeFalse();
            disposed.Should().BeTrue();
            watchers["/books"].Should().BeSameAs(existing);
        }

        [Test]
        public void stops_after_add_if_disposed_during_start()
        {
            var watchers = new ConcurrentDictionary<string, object>();
            var disposed = false;
            var watcher = new object();
            var calls = 0;

            var added = FileSystemWatcherLifecycle.TryEnable(
                watchers,
                "/books",
                watcher,
                () => ++calls > 1,
                _ => disposed = true);

            added.Should().BeFalse();
            disposed.Should().BeTrue();
            watchers.Should().BeEmpty();
        }
    }
}
