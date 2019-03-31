using System.Collections.Generic;
using Tyr.Builds;

namespace Tyr.BuildSelection
{
    public class RotateSelector : BuildSelector
    {
        public Build Select(List<Build> builds, Dictionary<string, int> defeats, Dictionary<string, int> games)
        {
            Build preffered = null;
            int losses = int.MaxValue;
            foreach (Build option in builds)
            {
                if (!defeats.ContainsKey(option.Name()))
                    defeats.Add(option.Name(), 0);
                if (!games.ContainsKey(option.Name()))
                    games.Add(option.Name(), 0);
                
                int newLosses = defeats[option.Name()] - (games[option.Name()] - defeats[option.Name()]) / 4;

                if (newLosses < losses)
                {
                    losses = newLosses;
                    preffered = option;
                }
            }
            return preffered;
        }
    }
}
