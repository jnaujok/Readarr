using System.Collections.Generic;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Books
{
    public class MonitoringOptions : IEmbeddedDocument
    {
        public MonitoringOptions()
        {
            BooksToMonitor = new List<string>();
            Monitor = MonitorTypes.Unknown;
        }

        public MonitorTypes Monitor { get; set; }
        public List<string> BooksToMonitor { get; set; }
        public bool Monitored { get; set; }
    }
}
