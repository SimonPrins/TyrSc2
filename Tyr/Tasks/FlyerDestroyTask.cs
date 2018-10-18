using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Util;

namespace Tyr.Tasks
{
    class FlyerDestroyTask : Task
    {
        public static FlyerDestroyTask Task = new FlyerDestroyTask();
        private bool Search = true;
        public int RequiredSize { get; set; } = 8;
        private PriorityTargetting Targetting = new PriorityTargetting();

        public FlyerDestroyTask() : base(5)
        {
            Targetting.DefaultPriorities.MaxRange = 15;
            foreach (uint type in UnitTypes.AirAttackTypes)
                Targetting.DefaultPriorities[type] = 5;
            Targetting.DefaultPriorities[UnitTypes.STARPORT] = 4;
        }

        public static void Enable()
        {
            Task.Stopped = false;
            Tyr.Bot.TaskManager.Add(Task);
        }

        public override bool DoWant(Agent agent)
        {
            return agent.Unit.UnitType == UnitTypes.VOID_RAY || agent.Unit.UnitType == UnitTypes.CARRIER;
        }

        public override bool IsNeeded()
        {
            return Tyr.Bot.UnitManager.Completed(UnitTypes.VOID_RAY) + Tyr.Bot.UnitManager.Completed(UnitTypes.CARRIER) >= RequiredSize;
        }

        public override void OnFrame(Tyr tyr)
        {
            Point2D targetLocation = GetTarget();
            bool starportsRemain = false;
            /*
            foreach (Unit unit in tyr.Enemies())
                if (unit.UnitType == UnitTypes.STARPORT)
                {
                    starportsRemain = true;
                    Search = true;
                    break;
                }
            */
            tyr.DrawText("Search: " + Search);
            tyr.DrawSphere(targetLocation);
            foreach (Agent agent in units)
            {
                Unit target = Targetting.GetTarget(agent);
                if (Search)
                {
                    if (target != null && (target.UnitType == UnitTypes.STARPORT ||
                        (UnitTypes.AirAttackTypes.Contains(target.UnitType) && agent.DistanceSq(target) <= 15 * 15)))
                    {
                        agent.Order(Abilities.ATTACK, target.Tag);
                        tyr.DrawLine(agent.Unit.Pos, target.Pos);
                    }
                    else
                        agent.Order(Abilities.MOVE, targetLocation);

                    if (agent.DistanceSq(targetLocation) <= 4 * 4 && !starportsRemain)
                        Search = false;
                }
                else
                {
                    if (target != null)
                    {
                        agent.Order(Abilities.ATTACK, target.Tag);
                        tyr.DrawLine(agent.Unit.Pos, target.Pos);
                    }
                    else
                        agent.Order(Abilities.ATTACK, targetLocation);
                }
            }
        }

        public Point2D GetTarget()
        {
            if (Search)
                return Tyr.Bot.TargetManager.PotentialEnemyStartLocations[0];
            Unit enemyTarget = null;
            Point2D enemyLocation = Tyr.Bot.TargetManager.PotentialEnemyStartLocations[0];
            float dist = 1000 * 1000;
            bool isStarport = false;
            foreach (Unit unit in Tyr.Bot.Enemies())
            {
                if (unit.UnitType != UnitTypes.STARPORT && isStarport)
                    continue;

                if (!UnitTypes.BuildingTypes.Contains(unit.UnitType))
                    continue;

                float newDist = SC2Util.DistanceSq(enemyLocation, unit.Pos);
                if (unit.UnitType == UnitTypes.STARPORT && !isStarport)
                {
                    isStarport = true;
                    dist = newDist;
                    enemyTarget = unit;
                    continue;
                }
                if (newDist < dist)
                {
                    dist = newDist;
                    enemyTarget = unit;
                }
            }
            if (enemyTarget != null)
                return SC2Util.To2D(enemyTarget.Pos);

            return Tyr.Bot.TargetManager.AttackTarget;
        }
    }
}
