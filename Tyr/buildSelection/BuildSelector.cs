using System.Collections.Generic;
using Tyr.Builds;

namespace Tyr.BuildSelection
{
    public interface BuildSelector
    {
        Build Select(List<Build> builds, string[] lines);
    }
}
