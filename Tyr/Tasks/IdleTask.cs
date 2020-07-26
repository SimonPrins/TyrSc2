using SC2APIProtocol;
using SC2Sharp.Agents;
using SC2Sharp.Micro;
using SC2Sharp.Util;

namespace SC2Sharp.Tasks
{
    public class IdleTask : Task
    {
        public static IdleTask Task = new IdleTask();
        public Point2D Target;
        public Point2D OverrideTarget;
        internal bool FearEnemies;
        public int IdleRange = 5;
        public bool AttackMove = false;

        public bool RetreatFarOverlords = true;

        private YamatoController YamatoController = new YamatoController();

        public IdleTask() : base(0)
        { }

        public static void Enable()
        {
            Task.Stopped = false;
            Bot.Main.TaskManager.Add(Task);
        }

        public override bool DoWant(Agent agent)
        {
            return true;
        }

        public override bool IsNeeded()
        {
            return true;
        }

        public override void OnFrame(Bot bot)
        {
            if (OverrideTarget != null)
                Target = OverrideTarget;
            else if (bot.BaseManager.Natural.Owner == bot.PlayerId)
                Target = bot.BaseManager.NaturalDefensePos;
            else Target = bot.BaseManager.MainDefensePos;

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
                if (YamatoController.DetermineAction(agent, Target))
                {
                    continue;
                }
                if (agent.Unit.UnitType == UnitTypes.SIEGE_TANK_SIEGED && SC2Util.DistanceSq(agent.Unit.Pos, Target) > IdleRange * IdleRange)
                {
                    if (AttackMove)
                        Attack(agent, Target);
                    else
                        agent.Order(Abilities.UNSIEGE);
                    continue;
                }
                if (agent.Unit.UnitType == UnitTypes.LIBERATOR_AG)
                {
                    Attack(agent, Target);
                    continue;
                }
                if (agent.Unit.UnitType == UnitTypes.OVERLORD 
                    && agent.DistanceSq(bot.MapAnalyzer.StartLocation) >= 80 * 80
                    && RetreatFarOverlords)
                {
                    agent.Order(Abilities.MOVE, SC2Util.To2D(bot.MapAnalyzer.StartLocation));
                    continue;
                }
                if (FearEnemies && (agent.IsCombatUnit || agent.Unit.UnitType == UnitTypes.OVERSEER || agent.Unit.UnitType == UnitTypes.RAVEN))
                {
                    Unit fleeEnemy = null;
                    float distance = 10 * 10;
                    foreach (Unit enemy in bot.Enemies())
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
                if ((agent.IsCombatUnit || agent.Unit.UnitType == UnitTypes.OVERSEER || agent.Unit.UnitType == UnitTypes.OBSERVER) && SC2Util.DistanceSq(agent.Unit.Pos, Target) >= IdleRange * IdleRange)
                {
                    if (AttackMove)
                        Attack(agent, Target);
                    else
                        agent.Order(Abilities.MOVE, Target);
                    continue;
                }
                if (AttackMove && agent.Unit.UnitType == UnitTypes.SIEGE_TANK && SC2Util.DistanceSq(agent.Unit.Pos, Target) < IdleRange * IdleRange && bot.Frame % 67 == 0)
                {
                    agent.Order(Abilities.SIEGE);
                    continue;
                }
            }
        }
    }
}
