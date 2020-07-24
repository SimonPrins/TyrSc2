using System.Collections.Generic;
using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Util;

namespace Tyr.Tasks
{
    class SCVRushTask : Task
    {
        public int TakeWorkers = 12;
        private HashSet<ulong> regenerating = new HashSet<ulong>();
        PriorityTargetting RangedTargetting = new PriorityTargetting();
        PriorityTargetting CloseTargetting = new PriorityTargetting();

        public SCVRushTask() : base(9)
        {
            RangedTargetting.DefaultPriorities.MaxRange = 10;
            RangedTargetting.DefaultPriorities[UnitTypes.LARVA] = -1;
            RangedTargetting.DefaultPriorities[UnitTypes.EGG] = -1;
            RangedTargetting.DefaultPriorities[UnitTypes.OVERLORD] = -1;

            foreach (uint t in UnitTypes.BuildingTypes)
                RangedTargetting.DefaultPriorities[t] = -1;

            CloseTargetting.DefaultPriorities.MaxRange = 2;
            CloseTargetting.DefaultPriorities[UnitTypes.LARVA] = -1;
            CloseTargetting.DefaultPriorities[UnitTypes.EGG] = -1;
            CloseTargetting.DefaultPriorities[UnitTypes.OVERLORD] = -1;

            foreach (uint t in UnitTypes.BuildingTypes)
                CloseTargetting.DefaultPriorities[t] = -1;
        }

        public override bool DoWant(Agent agent)
        {
            return agent.IsWorker && (TakeWorkers > 0 || 
                SC2Util.DistanceSq(agent.Unit.Pos, Bot.Bot.MapAnalyzer.StartLocation) >= 20 * 20 && SC2Util.DistanceSq(agent.Unit.Pos, Bot.Bot.MapAnalyzer.StartLocation) <= 41 * 41);
        }

        public override List<UnitDescriptor> GetDescriptors()
        {
            List<UnitDescriptor> result = new List<UnitDescriptor>();
            result.Add(new UnitDescriptor() { Count = TakeWorkers, UnitTypes = UnitTypes.WorkerTypes });
            return result;
        }

        public override bool IsNeeded()
        {
            return true;
        }

        public override void Add(Agent agent)
        {
            base.Add(agent);
            TakeWorkers--;
        }

        public override void OnFrame(Bot tyr)
        {
            ulong mineral = 0;
            if (tyr.BaseManager.Main.BaseLocation.MineralFields.Count > 0)
                mineral = tyr.BaseManager.Main.BaseLocation.MineralFields[0].Tag;

            foreach (Agent agent in units)
            {
                if (!regenerating.Contains(agent.Unit.Tag) && agent.Unit.Shield <= 3 && agent.Unit.UnitType == UnitTypes.PROBE)
                    regenerating.Add(agent.Unit.Tag);
                else if (regenerating.Contains(agent.Unit.Tag) && agent.Unit.Shield == agent.Unit.ShieldMax)
                    regenerating.Remove(agent.Unit.Tag);

                if (regenerating.Contains(agent.Unit.Tag))
                {
                    bool flee = false;
                    foreach (Unit enemy in tyr.Observation.Observation.RawData.Units)
                    {
                        if (enemy.Alliance != Alliance.Enemy)
                            continue;
                        if (!UnitTypes.CombatUnitTypes.Contains(enemy.UnitType) && !UnitTypes.WorkerTypes.Contains(enemy.UnitType))
                            continue;

                        if (SC2Util.DistanceSq(agent.Unit.Pos, enemy.Pos) <= 3 * 3)
                        {
                            flee = true;
                            break;
                        }
                    }

                    if (flee)
                    {
                        if (mineral == 0)
                            agent.Order(Abilities.MOVE, SC2Util.To2D(tyr.MapAnalyzer.StartLocation));
                        else
                            agent.Order(Abilities.MOVE, mineral);
                    }
                    else
                        agent.Order(Abilities.ATTACK, tyr.TargetManager.AttackTarget);
                }
                else
                {
                    Unit broodling = GetBroodling(agent);
                    if (broodling != null)
                    {
                        if (mineral == 0)
                            agent.Order(Abilities.MOVE, SC2Util.To2D(tyr.MapAnalyzer.StartLocation));
                        else
                            agent.Order(Abilities.MOVE, mineral);
                        continue;
                    }

                    Unit closeTarget = CloseTargetting.GetTarget(agent);
                    if (closeTarget != null)
                    {
                        agent.Order(Abilities.ATTACK, closeTarget.Tag);
                        continue;
                    }

                    Unit rangeTarget = RangedTargetting.GetTarget(agent);
                    if (rangeTarget != null)
                    {
                        agent.Order(Abilities.ATTACK, rangeTarget.Tag);
                        continue;
                    }
                    if (agent.Unit.WeaponCooldown <= 3 || mineral == 0)
                        agent.Order(Abilities.ATTACK, tyr.TargetManager.AttackTarget);
                    else
                        agent.Order(Abilities.MOVE, mineral);
                }
            }
        }

        private Unit GetBroodling(Agent agent)
        {
            Unit broodling = null;
            float dist = 6 * 6;
            foreach (Unit enemy in Bot.Bot.Enemies())
            {
                if (enemy.UnitType != UnitTypes.BROODLING)
                    continue;

                float newDist = agent.DistanceSq(enemy);
                if (newDist < dist)
                {
                    dist = newDist;
                    broodling = enemy;
                }
            }
            return broodling;
        }
    }
}
