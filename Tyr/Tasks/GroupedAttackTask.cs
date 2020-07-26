using System.Collections.Generic;
using SC2Sharp.Agents;
using SC2Sharp.Builds;

namespace SC2Sharp.Tasks
{
    class GroupedAttackTask : Task
    {
        public static GroupedAttackTask Task = new GroupedAttackTask();

        public int RequiredSize { get; set; } = 14;
        public int RetreatSize { get; set; } = 0;
        public uint UnitType;
        public HashSet<uint> ExcludeUnitTypes = new HashSet<uint>();

        public bool AttackSent = false;

        CombatGroup CombatGroup = new CombatGroup();

        public static void Enable()
        {
            Task.Stopped = false;
            Bot.Main.TaskManager.Add(Task);
        }

        public GroupedAttackTask() : base(5)
        { }

        public override bool DoWant(Agent agent)
        {
            if (UnitType != 0)
                return agent.Unit.UnitType == UnitType 
                    || (UnitTypes.EquivalentTypes.ContainsKey(agent.Unit.UnitType) && UnitTypes.EquivalentTypes[agent.Unit.UnitType].Contains(UnitType));
            else
                return agent.IsCombatUnit && !ExcludeUnitTypes.Contains(agent.Unit.UnitType);
        }

        public override bool IsNeeded()
        {
            if (UnitType != 0)
                return Bot.Main.UnitManager.Completed(UnitType) >= RequiredSize;
            int combatUnits = 0;
            foreach (uint combatType in UnitTypes.CombatUnitTypes)
                if (!UnitTypes.EquivalentTypes.ContainsKey(combatType)
                    && !ExcludeUnitTypes.Contains(combatType))
                    combatUnits += Bot.Main.UnitManager.Completed(combatType);
            if (combatUnits >= RequiredSize)
            {
                AttackSent = true;
                return true;
            }
            if (Build.FoodUsed() > 194)
            {
                bool producing = false;
                foreach (Agent agent in Bot.Main.UnitManager.Agents.Values)
                {
                    if (agent.Unit.UnitType == UnitTypes.FACTORY
                        || agent.Unit.UnitType == UnitTypes.BARRACKS
                        || agent.Unit.UnitType == UnitTypes.STARPORT
                        || agent.Unit.UnitType == UnitTypes.GATEWAY
                        || agent.Unit.UnitType == UnitTypes.ROBOTICS_FACILITY
                        || agent.Unit.UnitType == UnitTypes.STARGATE
                        || agent.Unit.UnitType == UnitTypes.LARVA
                        || agent.Unit.UnitType == UnitTypes.EGG)
                    {
                        if (agent.Unit.Orders != null
                            && agent.Unit.Orders.Count > 0)
                        {
                            producing = true;
                            break;
                        }
                    }
                }
                if (producing)
                {
                    AttackSent = true;
                    return true;
                }
            }
            return false;
        }

        public override void OnFrame(Bot bot)
        {
            if (units.Count <= RetreatSize)
            {
                Clear();
                return;
            }

            bool canAttackGround = false;
            foreach (Agent agent in Units)
                if (agent.CanAttackGround())
                    canAttackGround = true;

            if (!canAttackGround)
            {
                Clear();
                return;
            }

            CombatGroup.Units = Units;
            CombatGroup.AttackAt(bot.TargetManager.AttackTarget, this);
        }
    }
}
