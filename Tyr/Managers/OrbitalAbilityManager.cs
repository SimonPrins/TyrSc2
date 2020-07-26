using SC2APIProtocol;
using System.Collections.Generic;
using SC2Sharp.Agents;

namespace SC2Sharp.Managers
{
    public class OrbitalAbilityManager : Manager
    {
        public List<ScanCommand> ScanCommands = new List<ScanCommand>();

        public int SaveEnergy = 0;

        public void OnFrame(Bot bot)
        {
            if (bot.GameInfo.PlayerInfo[(int)bot.PlayerId - 1].RaceActual != Race.Terran)
                return;
            foreach (Agent agent in bot.UnitManager.Agents.Values)
                if (agent.Unit.UnitType == UnitTypes.ORBITAL_COMMAND)
                    FindTarget(agent);
        }

        public void FindTarget(Agent orbital)
        {
            if (orbital.Unit.Energy < 50)
                return;

            ScanCommand scanCommand = null;
            foreach (ScanCommand potentialScan in ScanCommands)
                if (potentialScan.FromFrame <= Bot.Main.Frame)
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

            if (Bot.Main.Frame % 4 != 0)
                return;

            float distance = 1000000;
            Unit target = null;
            foreach (Unit mineral in Bot.Main.Observation.Observation.RawData.Units)
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
