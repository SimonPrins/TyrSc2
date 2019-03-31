using SC2APIProtocol;
using System.Collections.Generic;
using Tyr.Agents;

namespace Tyr.Micro
{
    public class YamatoController : CustomController
    {
        private Dictionary<ulong, int> YamatoFrames = new Dictionary<ulong, int>();
        public override bool DetermineAction(Agent agent, Point2D target)
        {
            if (agent.Unit.UnitType != UnitTypes.BATTLECRUISER)
                return false;

            if (!Tyr.Bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(UpgradeType.YamatoCannon))
                return false;

            if (!YamatoFrames.ContainsKey(agent.Unit.Tag))
                YamatoFrames.Add(agent.Unit.Tag, -10000);
            if (Tyr.Bot.Frame - YamatoFrames[agent.Unit.Tag] < 44)
                return true;
            if (Tyr.Bot.Frame - YamatoFrames[agent.Unit.Tag] < 22.4 * 72)
                return false;

            foreach (Unit enemy in Tyr.Bot.Enemies())
            {
                if (enemy.UnitType != UnitTypes.BATTLECRUISER
                    && enemy.UnitType != UnitTypes.MISSILE_TURRET
                    && enemy.UnitType != UnitTypes.CYCLONE)
                    continue;

                if (agent.DistanceSq(enemy) <= 10 * 10)
                {
                    agent.Order(401, enemy.Tag);
                    YamatoFrames[agent.Unit.Tag] = Tyr.Bot.Frame;
                    return true;
                }
            }
            
            return false;
        }
    }
}
