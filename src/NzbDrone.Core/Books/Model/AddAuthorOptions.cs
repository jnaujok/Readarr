namespace NzbDrone.Core.Books
{
    public class AddAuthorOptions : MonitoringOptions
    {
        public bool SearchForMissingBooks { get; set; }
        public bool AnyEditionOk { get; set; } = true;
    }
}
