using System;
using System.Collections.Generic;
using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Util;

namespace Tyr.Tasks
{
    class AdeptKillSquadTask : Task
    {
        public int MaxUnits = 10;
        public AdeptKillSquadTask() : base(10)
        { }

        public override bool DoWant(Agent agent)
        {
            if (units.Count >= MaxUnits)
                return false;
            if (agent.Unit.UnitType != UnitTypes.ADEPT)
                return false;

            if (SC2Util.DistanceSq(agent.Unit.Pos, Tyr.Bot.MapAnalyzer.StartLocation) <= 40 * 40)
                return false;

            foreach (Unit enemy in Tyr.Bot.Observation.Observation.RawData.Units)
            {
                if (enemy.Alliance != Alliance.Enemy)
                    continue;

                if (!UnitTypes.WorkerTypes.Contains(enemy.UnitType))
                    continue;

                if (SC2Util.DistanceSq(enemy.Pos, agent.Unit.Pos) <= 9 * 9)
                    return true;
            }
            return false;
        }

        public override List<UnitDescriptor> GetDescriptors()
        {
            List<UnitDescriptor> result = new List<UnitDescriptor>();
            result.Add(new UnitDescriptor() { Pos = Tyr.Bot.TargetManager.AttackTarget, Count = MaxUnits - units.Count, UnitTypes = new HashSet<uint>() { UnitTypes.ADEPT } });
            return result;
        }

        public override bool IsNeeded()
        {
            return true;
        }

        public override void OnFrame(Tyr tyr)
        {
            bool workersRemain = false;
            foreach (Unit enemy in Tyr.Bot.Observation.Observation.RawData.Units)
            {
                if (enemy.Alliance != Alliance.Enemy)
                    continue;

                if (UnitTypes.WorkerTypes.Contains(enemy.UnitType))
                {
                    workersRemain = true;
                    break;
                }
            }

            if (!workersRemain)
            {
                Clear();
                return;
            }

            foreach (Agent agent in units)
            {
                Unit target = null;
                float dist = 10 * 10;
                bool underThreat = false;
                foreach (Unit enemy in Tyr.Bot.Observation.Observation.RawData.Units)
                {
                    if (enemy.Alliance != Alliance.Enemy)
                        continue;

                    float newDist = SC2Util.DistanceSq(enemy.Pos, agent.Unit.Pos);
                    if (newDist <= 9 * 9 && UnitTypes.CombatUnitTypes.Contains(enemy.UnitType))
                    {
                        underThreat = true;
                        break;
                    }
                    if (newDist >= dist)
                        continue;

                    if (!UnitTypes.WorkerTypes.Contains(enemy.UnitType))
                        continue;

                    dist = newDist;
                    target = enemy;
                }
                if (underThreat || target == null)
                    tyr.MicroController.Attack(agent, tyr.TargetManager.AttackTarget);
                else
                    agent.Order(Abilities.ATTACK, target.Tag);
            }
        }
    }
}
