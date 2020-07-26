using System.Collections.Generic;
using SC2Sharp.Builds;

namespace SC2Sharp.BuildSelection
{
    public interface BuildSelector
    {
        Build Select(List<Build> builds, string[] lines);
    }
}
