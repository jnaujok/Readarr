using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(041)]
    public class add_wanted_format_kinds : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("QualityProfiles").AddColumn("WantedFormatKinds").AsString().Nullable();
            Alter.Table("Books").AddColumn("WantedFormatKinds").AsString().Nullable();
        }
    }
}
