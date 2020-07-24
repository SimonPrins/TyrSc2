using System.Collections.Generic;
using Tyr.Builds;

namespace Tyr.BuildSelection
{
    public class RotateSelector : BuildSelector
    {
        public bool CountWins = true;
        public Build Select(List<Build> builds, string[] lines)
        {
            Dictionary<string, int> defeats = new Dictionary<string, int>();
            Dictionary<string, int> games = new Dictionary<string, int>();
            foreach (string line in lines)
            {
                if (line.StartsWith("result "))
                {
                    string[] words = line.Split(' ');
                    if (words[1] != Bot.Bot.EnemyRace.ToString())
                        continue;
                    if (words[3] == "Defeat")
                    {
                        if (!defeats.ContainsKey(words[2]))
                            defeats.Add(words[2], 0);
                        defeats[words[2]]++;

                        if (!games.ContainsKey(words[2]))
                            games.Add(words[2], 1);
                        else if (games[words[2]] < defeats[words[2]])
                            games[words[2]] = defeats[words[2]];
                    }
                }
                else if (line.StartsWith("started"))
                {
                    string[] words = line.Split(' ');
                    if (words[1] != Bot.Bot.EnemyRace.ToString())
                        continue;

                    if (!games.ContainsKey(words[2]))
                        games.Add(words[2], 0);
                    games[words[2]]++;
                }
            }

            Build preffered = null;
            int losses = int.MaxValue;
            foreach (Build option in builds)
            {
                if (!defeats.ContainsKey(option.Name()))
                    defeats.Add(option.Name(), 0);
                if (!games.ContainsKey(option.Name()))
                    games.Add(option.Name(), 0);
                
                int newLosses;
                if (CountWins)
                    newLosses = defeats[option.Name()] - (games[option.Name()] - defeats[option.Name()]) / 4;
                else
                    newLosses = defeats[option.Name()];

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
