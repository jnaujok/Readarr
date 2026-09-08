using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.MediaFiles.Commands
{
    public class MergeAudiobookCommand : Command
    {
        public int EditionId { get; set; }

        public override bool SendUpdatesToClient => true;
        public override bool RequiresDiskAccess => true;

        public MergeAudiobookCommand()
        {
        }

        public MergeAudiobookCommand(int editionId)
        {
            EditionId = editionId;
        }
    }
}
