using Newtonsoft.Json;
using System.Collections.Generic;
using SC2Sharp.CombatSim.Actions;

namespace SC2Sharp.CombatSim.CombatMicro
{
    [JsonObject(MemberSerialization.Fields)]
    public class MedivacHealClosest : CombatMicro
    {
        private long TargetTag = 0;
        public Action Act(SimulationState state, CombatUnit unit)
        {
            CombatUnit target = null;
            if (TargetTag != 0)
                target = state.GetUnit(TargetTag, unit.Owner);
            if (target == null)
            {
                List<CombatUnit> allies = unit.Owner == 1 ? state.Player1Units : state.Player2Units;
                float dist = 10000000000;
                foreach (CombatUnit ally in allies)
                {
                    if (!ally.HasAttribute(UnitAttribute.Biological))
                        continue;
                    if (ally.Health >= unit.HealthMax)
                        continue;

                    float newDist = unit.DistSq(ally);
                    if (newDist > dist)
                        continue;

                    target = ally;
                    TargetTag = ally.Tag;
                    dist = newDist;
                }
                if (target == null)
                {
                    foreach (CombatUnit ally in allies)
                    {
                        if (!ally.HasAttribute(UnitAttribute.Biological))
                            continue;
                        float newDist = unit.DistSq(ally);
                        if (newDist > dist)
                            continue;

                        target = ally;
                        TargetTag = ally.Tag;
                        dist = newDist;
                    }
                }
            }
            return new MedivacHeal(target);
        }
    }
}
