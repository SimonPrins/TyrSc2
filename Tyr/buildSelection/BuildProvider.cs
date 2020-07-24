using System.Collections.Generic;
using Tyr.Builds;

namespace Tyr.buildSelection
{
    public interface BuildsProvider
    {
        List<Build> GetBuilds(Bot tyr, string[] lines);
    }
}
