using SC2APIProtocol;
using System.Collections.Generic;
using Tyr.Agents;
using Tyr.Builds;
using Tyr.Util;

namespace Tyr.Tasks
{
    class TimingAttackTask : Task
    {
        public static TimingAttackTask Task = new TimingAttackTask();

        public int RequiredSize { get; set; } = 14;
        public int RetreatSize { get; set; } = 0;
        public uint UnitType;
        public HashSet<uint> ExcludeUnitTypes = new HashSet<uint>();

        public bool AttackSent = false;
        public bool DefendOtherAgents = false;

        private Agent MedivacRetreatTarget = null;
        private int MedivacRetreatTargetUpdateFrame = 0;

        public static void Enable()
        {
            Task.Stopped = false;
            Bot.Bot.TaskManager.Add(Task);
        }

        public TimingAttackTask() : base(5)
        {
            this.JoinCombatSimulation = true;
        }

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
                return Bot.Bot.UnitManager.Completed(UnitType) >= RequiredSize;
            int combatUnits = 0;
            foreach (uint combatType in UnitTypes.CombatUnitTypes)
                if (!UnitTypes.EquivalentTypes.ContainsKey(combatType)
                    && !ExcludeUnitTypes.Contains(combatType))
                    combatUnits += Bot.Bot.UnitManager.Completed(combatType);
            if (combatUnits >= RequiredSize)
            {
                AttackSent = true;
                return true;
            }
            if (Build.FoodUsed() > 194)
            {
                bool producing = false;
                foreach (Agent agent in Bot.Bot.UnitManager.Agents.Values)
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

        public override void OnFrame(Bot tyr)
        {
            if (units.Count <= RetreatSize && Units.Count > 0)
            {
                Clear();
                return;
            }

            tyr.DrawText("Army size: " + Units.Count);

            bool canAttackGround = false;
            foreach (Agent agent in Units)
                if (agent.CanAttackGround())
                    canAttackGround = true;

            if (!canAttackGround && Units.Count > 0)
            {
                DebugUtil.WriteLine("No anti grounnd units. Ending attack.");
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
                if (agent.Unit.UnitType == UnitTypes.MEDIVAC)
                {
                    UpdateMedivacRetreatTarget(tyr);
                    if (MedivacRetreatTarget != null)
                    {
                        Attack(agent, SC2Util.To2D(MedivacRetreatTarget.Unit.Pos));
                        continue;
                    }
                }

                if (defendAgent != null && agent.DistanceSq(defendAgent) >= 3 * 3 && agent.DistanceSq(defendAgent) <= 40 * 40)
                    Attack(agent, SC2Util.To2D(defendAgent.Unit.Pos));
                else
                    Attack(agent, tyr.TargetManager.AttackTarget);
            }
        }

        private void UpdateMedivacRetreatTarget(Bot tyr)
        {
            if (MedivacRetreatTargetUpdateFrame == tyr.Frame)
                return;
            MedivacRetreatTargetUpdateFrame = tyr.Frame;

            float distance = 1000 * 1000;
            foreach (Agent agent in Units)
            {
                if (agent.Unit.IsFlying)
                    continue;

                float newDist = agent.DistanceSq(tyr.TargetManager.AttackTarget);
                if (newDist < distance)
                {
                    distance = newDist;
                    MedivacRetreatTarget = agent;
                }
            }
        }
    }
}
