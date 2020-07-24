using SC2APIProtocol;
using System.Collections.Generic;
using Tyr.Builds;
using Tyr.Builds.Protoss;
using Tyr.StrategyAnalysis;

namespace Tyr.buildSelection
{
    class TycklesBuildsProvider : BuildsProvider
    {
        public List<Build> GetBuilds(Bot tyr, string[] lines)
        {
            List<Build> options = new List<Build>();


            if (Bot.Main.EnemyRace == Race.Protoss)
            {
                if (Stalker.Get().DetectedPreviously
                    && Zealot.Get().DetectedPreviously
                    && !Immortal.Get().DetectedPreviously)
                {
                    options.Add(new DefensiveSentries() { DelayAttacking = true });
                    //options.Add(new MassSentries() { SkipNatural = true });
                    //options.Add(new GreedySentries());
                    return options;
                }
                if (!Stalker.Get().DetectedPreviously
                    && Zealot.Get().DetectedPreviously
                    && !Immortal.Get().DetectedPreviously)
                {
                    //options.Add(new DefensiveSentries() { DelayAttacking = true });
                    options.Add(new MassSentries() { SkipNatural = true });
                    return options;
                }
            }
            if (Bot.Main.EnemyRace == Race.Zerg)
            {
                if ((Zergling.Get().DetectedPreviously || Roach.Get().DetectedPreviously)
                    && !Hydralisk.Get().DetectedPreviously)
                {
                    options.Add(new DefensiveSentries());
                    return options;
                }
            }
            if (Bot.Main.EnemyRace == Race.Terran && Battlecruiser.Get().DetectedPreviously && !SiegeTank.Get().DetectedPreviously)
            {
                options.Add(new MassSentries() { AntiBC = true });
                return options;
            }
            options.Add(new MassSentries());
            options.Add(new GreedySentries());
            options.Add(new DefensiveSentries() { DelayAttacking = true });

            return options;
        }

    }
}
