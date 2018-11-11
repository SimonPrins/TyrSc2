using SC2APIProtocol;
using Tyr.Agents;

namespace Tyr.Managers
{
    public class OrbitalAbilityManager : Manager
    {
        public void OnFrame(Tyr tyr)
        {
            if (tyr.GameInfo.PlayerInfo[(int)tyr.PlayerId - 1].RaceActual != Race.Terran)
                return;
            foreach (Agent agent in tyr.UnitManager.Agents.Values)
                if (agent.Unit.UnitType == UnitTypes.ORBITAL_COMMAND)
                    findTarget(agent);
        }

        public void findTarget(Agent orbital)
        {
            if (orbital.Unit.Energy < 50)
                return;

            float distance = 1000000;
            Unit target = null;
            foreach (Unit mineral in Tyr.Bot.Observation.Observation.RawData.Units)
            {
                if (!UnitTypes.MineralFields.Contains(mineral.UnitType))
                    continue;
                float newDist = orbital.DistanceSq(mineral);
                if (newDist < distance)
                {
                    distance = newDist;
                    target = mineral;
                }
            }
            if (target != null)
                orbital.Order(171, target.Tag);
        }
    }
}
