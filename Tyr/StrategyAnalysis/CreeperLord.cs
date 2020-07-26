using SC2APIProtocol;
using System.Collections.Generic;
using SC2Sharp.Agents;

namespace SC2Sharp.StrategyAnalysis
{
    public class CreeperLord : Strategy
    {
        private static CreeperLord Singleton = new CreeperLord();

        private Dictionary<ulong, int> LastExpansionHoverFrame = new Dictionary<ulong, int>(); 

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            HashSet<ulong> creeperLords = new HashSet<ulong>();
            foreach (Unit enemy in Bot.Main.Enemies())
            {
                if (enemy.UnitType != UnitTypes.OVERLORD)
                    continue;
                foreach (Agent agent in Bot.Main.Units())
                {
                    if (!UnitTypes.ResourceCenters.Contains(agent.Unit.UnitType))
                        continue;
                    if (agent.DistanceSq(enemy) <= 2 * 2)
                    {
                        creeperLords.Add(enemy.Tag);
                        if (!LastExpansionHoverFrame.ContainsKey(enemy.Tag))
                            LastExpansionHoverFrame.Add(enemy.Tag, Bot.Main.Frame);
                        else if (Bot.Main.Frame - LastExpansionHoverFrame[enemy.Tag] >= 22.4 * 10)
                            return true;
                        break;
                    }
                }
            }
            List<ulong> removeTags = new List<ulong>();
            foreach (ulong tag in LastExpansionHoverFrame.Keys)
                if (!creeperLords.Contains(tag))
                    removeTags.Add(tag);
            foreach (ulong removeTag in removeTags)
                LastExpansionHoverFrame.Remove(removeTag);
            return false;
        }

        public override string Name()
        {
            return "CreeperLord";
        }
    }
}
