using System.Collections.Generic;
using SC2Sharp.Builds;

namespace SC2Sharp.buildSelection
{
    public interface BuildsProvider
    {
        List<Build> GetBuilds(Bot bot, string[] lines);
    }
}
