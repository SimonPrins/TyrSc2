using SC2APIProtocol;
using SC2Sharp.Agents;
using SC2Sharp.MapAnalysis;
using SC2Sharp.Util;

namespace SC2Sharp.Tasks
{
    public class SupplyDepotTask : Task
    {
        public static SupplyDepotTask Task = new SupplyDepotTask();
        public WallInCreator RaiseWall;

        public SupplyDepotTask() : base(1)
        { }

        public static void Enable()
        {
            Enable(Task);
        }

        public override bool DoWant(Agent agent)
        {
            return agent.Unit.UnitType == UnitTypes.SUPPLY_DEPOT
                || agent.Unit.UnitType == UnitTypes.SUPPLY_DEPOT_LOWERED;
        }

        public override bool IsNeeded()
        {
            return true;
        }

        public override void OnFrame(Bot bot)
        {
            foreach (Agent agent in units)
            {
                bool closeEnemy = false;
                if (RaiseWall != null)
                    foreach (WallBuilding building in RaiseWall.Wall)
                        if (SC2Util.DistanceSq(building.Pos, agent.Unit.Pos) < 2)
                        {
                            closeEnemy = true;
                            break;
                        }

                if (!closeEnemy)
                {
                    foreach (Unit enemy in bot.Enemies())
                        if (agent.DistanceSq(enemy) <= 10 * 10
                            && !enemy.IsFlying
                            && enemy.UnitType != UnitTypes.REAPER
                            && enemy.UnitType != UnitTypes.ADEPT_PHASE_SHIFT
                            && enemy.UnitType != UnitTypes.KD8_CHARGE
                            && !UnitTypes.ChangelingTypes.Contains(enemy.UnitType)
                            && !UnitTypes.WorkerTypes.Contains(enemy.UnitType))
                        {
                            closeEnemy = true;
                            break;
                        }
                }
                if (agent.Unit.UnitType == UnitTypes.SUPPLY_DEPOT
                    && !closeEnemy)
                    agent.Order(556);
                else if (agent.Unit.UnitType != UnitTypes.SUPPLY_DEPOT
                    && closeEnemy)
                    agent.Order(558);
            }
        }
    }
}
