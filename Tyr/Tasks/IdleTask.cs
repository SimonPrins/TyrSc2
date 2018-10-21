using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Util;

namespace Tyr.Tasks
{
    public class IdleTask : Task
    {
        public static IdleTask Task = new IdleTask();
        public Point2D Target;
        public Point2D OverrideTarget;
        internal bool FearEnemies;

        public IdleTask() : base(0)
        { }

        public static void Enable()
        {
            Task.Stopped = false;
            Tyr.Bot.TaskManager.Add(Task);
        }

        public override bool DoWant(Agent agent)
        {
            return true;
        }

        public override bool IsNeeded()
        {
            return true;
        }

        public override void OnFrame(Tyr tyr)
        {
            if (OverrideTarget != null)
                Target = OverrideTarget;
            else if (tyr.BaseManager.Natural.Owner == tyr.PlayerId)
                Target = tyr.BaseManager.NaturalDefensePos;
            else Target = tyr.BaseManager.MainDefensePos;

            foreach (Agent agent in units)
            {
                if (agent.Unit.UnitType == UnitTypes.LURKER && SC2Util.DistanceSq(agent.Unit.Pos, Target) < 3 * 3)
                {
                    agent.Order(Abilities.BURROW_DOWN);
                    continue;
                }
                if (agent.Unit.UnitType == UnitTypes.SIEGE_TANK && SC2Util.DistanceSq(agent.Unit.Pos, Target) < 3 * 3)
                {
                    agent.Order(Abilities.SIEGE);
                    continue;
                }
                if (agent.Unit.UnitType == UnitTypes.SIEGE_TANK_SIEGED && SC2Util.DistanceSq(agent.Unit.Pos, Target) > 3 * 3)
                {
                    agent.Order(Abilities.UNSIEGE);
                    continue;
                }
                if (FearEnemies && (agent.IsCombatUnit || agent.Unit.UnitType == UnitTypes.OVERSEER))
                {
                    Unit fleeEnemy = null;
                    float distance = 10 * 10;
                    foreach (Unit enemy in tyr.Enemies())
                    {
                        if (!UnitTypes.CombatUnitTypes.Contains(enemy.UnitType))
                            continue;
                        float dist = agent.DistanceSq(enemy);
                        if (dist < distance)
                        {
                            distance = dist;
                            fleeEnemy = enemy;
                        }
                    }
                    if (fleeEnemy != null)
                    {
                        PotentialHelper helper = new PotentialHelper(agent.Unit.Pos);
                        helper.From(fleeEnemy.Pos);
                        agent.Order(Abilities.MOVE, helper.Get());
                        continue;
                    }
                }
                if ((agent.IsCombatUnit || agent.Unit.UnitType == UnitTypes.OVERSEER) && SC2Util.DistanceSq(agent.Unit.Pos, Target) >= 5 * 5)
                    agent.Order(Abilities.MOVE, Target);
            }
        }
    }
}
