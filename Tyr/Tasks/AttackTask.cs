using Tyr.Agents;

namespace Tyr.Tasks
{
    class AttackTask : Task
    {
        public static AttackTask Task = new AttackTask();

        public int LeaveAtHome = 1;
        public uint UnitType = UnitTypes.HELLION;

        public static void Enable()
        {
            Task.Stopped = false;
            Bot.Bot.TaskManager.Add(Task);
        }

        public AttackTask() : base(10)
        { }

        public override bool DoWant(Agent agent)
        {
            if (UnitType != 0)
                return agent.Unit.UnitType == UnitType 
                    || (UnitTypes.EquivalentTypes.ContainsKey(agent.Unit.UnitType) && UnitTypes.EquivalentTypes[agent.Unit.UnitType].Contains(UnitType));
            else
                return agent.IsCombatUnit;
        }

        public override bool IsNeeded()
        {
            if (UnitType != 0)
                return Bot.Bot.UnitManager.Completed(UnitType) > LeaveAtHome;
            int combatUnits = 0;
            foreach (uint combatType in UnitTypes.CombatUnitTypes)
                if (!UnitTypes.EquivalentTypes.ContainsKey(combatType))
                    combatUnits += Bot.Bot.UnitManager.Completed(combatType);
            if (combatUnits > LeaveAtHome)
                return true;
            return false;
        }

        public override void OnFrame(Bot tyr)
        {
            foreach (Agent agent in units)
                Attack(agent, tyr.TargetManager.AttackTarget);
        }
    }
}
