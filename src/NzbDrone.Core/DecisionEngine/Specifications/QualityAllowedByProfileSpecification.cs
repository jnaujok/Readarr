using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.DecisionEngine.Specifications
{
    public class QualityAllowedByProfileSpecification : IDecisionEngineSpecification
    {
        private readonly Logger _logger;

        public QualityAllowedByProfileSpecification(Logger logger)
        {
            _logger = logger;
        }

        public SpecificationPriority Priority => SpecificationPriority.Default;
        public RejectionType Type => RejectionType.Permanent;

        public virtual Decision IsSatisfiedBy(RemoteBook subject, SearchCriteriaBase searchCriteria)
        {
            _logger.Debug("Checking if report meets quality requirements. {0}", subject.ParsedBookInfo.Quality);

            var profile = subject.Author.QualityProfile.Value;
            var qualities = subject.ParsedBookInfo.Qualities;
            if (qualities == null || !qualities.Any())
            {
                qualities = new List<Quality> { subject.ParsedBookInfo.Quality.Quality };
            }

            var allowed = qualities.Where(q => IsAllowed(profile, q)).ToList();

            if (!allowed.Any())
            {
                _logger.Debug("Quality {0} rejected by Author's quality profile", subject.ParsedBookInfo.Quality);
                return Decision.Reject("{0} is not wanted in profile", subject.ParsedBookInfo.Quality.Quality);
            }

            if (!allowed.Contains(subject.ParsedBookInfo.Quality.Quality))
            {
                var preferred = allowed[0];
                var bestIndex = profile.GetIndex(preferred).Index;
                for (var i = 1; i < allowed.Count; i++)
                {
                    var index = profile.GetIndex(allowed[i]).Index;
                    if (index > bestIndex)
                    {
                        preferred = allowed[i];
                        bestIndex = index;
                    }
                }

                subject.ParsedBookInfo.Quality = new QualityModel(preferred, subject.ParsedBookInfo.Quality.Revision);
            }

            return Decision.Accept();
        }

        private static bool IsAllowed(QualityProfile profile, Quality quality)
        {
            foreach (var item in profile.Items)
            {
                if (item.Quality != null && item.Quality.Id == quality.Id)
                {
                    return item.Allowed;
                }

                if (item.Items != null && item.Items.Any(i => i.Quality != null && i.Quality.Id == quality.Id))
                {
                    return item.Allowed;
                }
            }

            return false;
        }
    }
}
