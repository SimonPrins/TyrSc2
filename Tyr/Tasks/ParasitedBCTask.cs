using SC2APIProtocol;
using System.Collections.Generic;
using Tyr.Agents;
using Tyr.Micro;
using Tyr.Util;

namespace Tyr.Tasks
{
    class ParasitedBCTask : Task
    {
        public static ParasitedBCTask Task = new ParasitedBCTask();
        private Dictionary<ulong, int> ParasitedFrame = new Dictionary<ulong, int>();


        public ParasitedBCTask() : base(15)
        { }

        public static void Enable()
        {
            Enable(Task);
        }

        public override List<UnitDescriptor> GetDescriptors()
        {
            List<UnitDescriptor> result = new List<UnitDescriptor>();
            result.Add(new UnitDescriptor() { UnitTypes = new HashSet<uint>() { UnitTypes.BATTLECRUISER} });
            return result;
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
            foreach (Agent agent in Units)
            {
                if (!ParasitedFrame.ContainsKey(agent.Unit.Tag))
                    ParasitedFrame.Add(agent.Unit.Tag, tyr.Frame);

                if (tyr.Frame - ParasitedFrame[agent.Unit.Tag] == 1)
                {
                    agent.Order(Abilities.CANCEL);
                    continue;
                }

                if (tyr.Frame - ParasitedFrame[agent.Unit.Tag] == 5)
                {
                    Unit target = null;
                    int value = 0;
                    foreach (Unit enemy in tyr.Enemies())
                    {
                        if (!UnitTypes.LookUp.ContainsKey(enemy.UnitType))
                            continue;

                        UnitTypeData data = UnitTypes.LookUp[enemy.UnitType];
                        int newVal = (int)data.MineralCost + (int)data.VespeneCost;
                        if (newVal > value)
                        {
                            target = enemy;
                            value = newVal;
                        }
                    }
                    if (target != null)
                        agent.Order(401, target.Tag);
                    continue;
                }

                if (tyr.Frame - ParasitedFrame[agent.Unit.Tag] == 180)
                {
                    bool jumped = false;
                    foreach (Agent airAttacker in tyr.UnitManager.Agents.Values)
                    {
                        if (!airAttacker.CanAttackAir() || airAttacker.Unit.UnitType == UnitTypes.INFESTOR || airAttacker.Unit.UnitType == UnitTypes.INFESTOR_BURROWED)
                            continue;
                        if (UnitTypes.LookUp[airAttacker.Unit.UnitType].Race != Race.Zerg)
                            continue;

                        int count = 0;
                        foreach (Agent airAttacker2 in tyr.UnitManager.Agents.Values)
                        {
                            if (!airAttacker.CanAttackAir() || airAttacker.Unit.UnitType == UnitTypes.INFESTOR || airAttacker.Unit.UnitType == UnitTypes.INFESTOR_BURROWED)
                                continue;
                            if (UnitTypes.LookUp[airAttacker.Unit.UnitType].Race != Race.Zerg)
                                continue;
                            if (airAttacker.Unit.Tag == airAttacker2.Unit.Tag)
                                continue;
                            if (airAttacker.DistanceSq(airAttacker2) <= 8 * 8)
                                count++;
                        }
                        if (count >= 15)
                        {
                            agent.Order(2358, SC2Util.To2D(airAttacker.Unit.Pos));
                            DebugUtil.WriteLine("Jumping BC to attackers.");
                            jumped = true;
                            break;
                        }
                    }
                    if (!jumped)
                    {
                        agent.Order(2358, tyr.TargetManager.PotentialEnemyStartLocations[0]);
                        DebugUtil.WriteLine("Jumping BC to enemy start location.");
                    }
                    continue;
                }
                if (tyr.Frame - ParasitedFrame[agent.Unit.Tag] > 180)
                    continue;

                if (InfestorController.NeuralControllers.ContainsKey(agent.Unit.Tag) && tyr.UnitManager.Agents.ContainsKey(InfestorController.NeuralControllers[agent.Unit.Tag]))
                    agent.Order(Abilities.MOVE, SC2Util.To2D(tyr.UnitManager.Agents[InfestorController.NeuralControllers[agent.Unit.Tag]].Unit.Pos));
                else
                    Attack(agent, tyr.TargetManager.AttackTarget);
            }
        }
    }
}
