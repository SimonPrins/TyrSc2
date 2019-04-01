using System;
using System.Collections.Generic;
using Tyr.Builds;
using Tyr.Util;

namespace Tyr.BuildSelection
{
    public class ProbabilitySelector : BuildSelector
    {
        private double TakePartLoss = 0.2;
        private double TakePartWin = 0.1;

        public Build Select(List<Build> builds, string[] lines)
        {
            if (builds.Count == 1)
                return builds[0];

            Dictionary<string, double> probabilities = new Dictionary<string, double>();
            foreach (Build option in builds)
                probabilities.Add(option.Name(), 1.0 / builds.Count);


            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (line.StartsWith("result "))
                {
                    string[] words = line.Split(' ');
                    if (words[1] != Tyr.Bot.EnemyRace.ToString())
                        continue;
                    if (!probabilities.ContainsKey(words[2]))
                        continue;
                    if (words[3] != "Defeat")
                        continue;

                    double takeAway = TakePartLoss * probabilities[words[2]];
                    probabilities[words[2]] -= takeAway;
                    foreach (Build option in builds)
                        if (option.Name() != words[2])
                            probabilities[option.Name()] += takeAway / (builds.Count - 1);
                }
                else if (line.StartsWith("started"))
                {
                    string[] words = line.Split(' ');
                    if (words[1] != Tyr.Bot.EnemyRace.ToString())
                        continue;
                    if (!probabilities.ContainsKey(words[2]))
                        continue;

                    if (i + 1 < lines.Length && lines[i + 1].StartsWith("result ") && lines[i + 1].EndsWith("Defeat"))
                        continue;

                    double addTo = 0;
                    foreach (Build option in builds)
                        if (option.Name() != words[2])
                        {
                            double takeAway = probabilities[option.Name()] * TakePartWin;
                            probabilities[option.Name()] -= takeAway;
                            addTo += takeAway;
                        }
                    probabilities[words[2]] += addTo;
                }
            }

            DebugUtil.WriteLine("Probabilties for selecting each build.");
            foreach (Build option in builds)
                DebugUtil.WriteLine(option.Name() + ": " + probabilities[option.Name()]);

            double rand = new Random().NextDouble();
            DebugUtil.WriteLine("Selected random number: " + rand);
            foreach (Build option in builds)
            {
                if (rand < probabilities[option.Name()])
                {
                    DebugUtil.WriteLine("Chosen build: " + option.Name());
                    return option;
                }
                rand -= probabilities[option.Name()];
            }
            return builds[builds.Count - 1];
        }
    }
}
