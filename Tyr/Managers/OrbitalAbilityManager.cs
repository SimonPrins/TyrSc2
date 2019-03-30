using SC2APIProtocol;
using System.Collections.Generic;
using Tyr.Agents;

namespace Tyr.Managers
{
    public class OrbitalAbilityManager : Manager
    {
        public List<ScanCommand> ScanCommands = new List<ScanCommand>();

        public int SaveEnergy = 0;

        public void OnFrame(Tyr tyr)
        {
            if (tyr.GameInfo.PlayerInfo[(int)tyr.PlayerId - 1].RaceActual != Race.Terran)
                return;
            foreach (Agent agent in tyr.UnitManager.Agents.Values)
                if (agent.Unit.UnitType == UnitTypes.ORBITAL_COMMAND)
                    FindTarget(agent);
        }

        public void FindTarget(Agent orbital)
        {
            if (orbital.Unit.Energy < 50)
                return;

            ScanCommand scanCommand = null;
            foreach (ScanCommand potentialScan in ScanCommands)
                if (potentialScan.FromFrame <= Tyr.Bot.Frame)
                {
                    scanCommand = potentialScan;
                    break;
                }
            if (scanCommand != null)
            {
                orbital.Order(399, scanCommand.Pos);
                ScanCommands.Remove(scanCommand);
                return;
            }


            if (orbital.Unit.Energy < 50 + SaveEnergy)
                return;

            if (Tyr.Bot.Frame % 4 != 0)
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
