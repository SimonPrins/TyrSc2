using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Util;

namespace Tyr.Tasks
{
    class TimingAttackTask : Task
    {
        public static TimingAttackTask Task = new TimingAttackTask();

        public int RequiredSize { get; set; } = 14;
        public int RetreatSize { get; set; } = 0;
        public uint UnitType;

        public bool AttackSent = false;
        public bool DefendOtherAgents = true;

        public static void Enable()
        {
            Task.Stopped = false;
            Tyr.Bot.TaskManager.Add(Task);
        }

        public TimingAttackTask() : base(5)
        { }

        public override bool DoWant(Agent agent)
        {
            if (UnitType != 0)
                return agent.Unit.UnitType == UnitType;
            else
                return agent.IsCombatUnit;
        }

        public override bool IsNeeded()
        {
            if (UnitType != 0)
                return Tyr.Bot.UnitManager.Completed(UnitType) >= RequiredSize;
            int combatUnits = 0;
            foreach (uint combatType in UnitTypes.CombatUnitTypes)
                combatUnits += Tyr.Bot.UnitManager.Completed(combatType);
            if (combatUnits >= RequiredSize)
            {
                AttackSent = true;
                return true;
            }
            return false;
        }

        public override void OnFrame(Tyr tyr)
        {
            if (units.Count <= RetreatSize)
            {
                Clear();
                return;
            }
            Agent defendAgent = null;
            if (DefendOtherAgents)
            {
                foreach (Agent agent in units)
                {
                    foreach (Unit enemy in tyr.Enemies())
                    {
                        if (!UnitTypes.CombatUnitTypes.Contains(enemy.UnitType))
                            continue;

                        if (SC2Util.DistanceSq(agent.Unit.Pos, enemy.Pos) <= 9 * 9)
                        {
                            defendAgent = agent;
                            break;
                        }
                    }
                    if (defendAgent != null)
                        break;
                }
            }

            foreach (Agent agent in units)
            {
                if (defendAgent != null && SC2Util.DistanceSq(agent.Unit.Pos, defendAgent.Unit.Pos) >= 3 * 3 && SC2Util.DistanceSq(agent.Unit.Pos, defendAgent.Unit.Pos) <= 40 * 40)
                    tyr.MicroController.Attack(agent, SC2Util.To2D(defendAgent.Unit.Pos));
                else
                    tyr.MicroController.Attack(agent, tyr.TargetManager.AttackTarget);
            }
        }
    }
}
