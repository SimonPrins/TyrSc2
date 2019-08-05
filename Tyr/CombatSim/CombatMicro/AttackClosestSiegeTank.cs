using Newtonsoft.Json;
using System.Collections.Generic;
using Tyr.CombatSim.Actions;

namespace Tyr.CombatSim.CombatMicro
{
    [JsonObject(MemberSerialization.Fields)]
    public class AttackClosestSiegeTank : CombatMicro
    {
        private long TargetTag = 0;
        public Action Act(SimulationState state, CombatUnit unit)
        {
            CombatUnit target = null;
            if (TargetTag != 0)
            {
                target = state.GetUnit(TargetTag, 3 - unit.Owner);
                if (target != null && unit.DistSq(target) <= 2 * 2)
                {
                    target = null;
                    TargetTag = 0;
                }
            }
            if (target == null)
            {
                List<CombatUnit> enemies = unit.Owner == 2 ? state.Player1Units : state.Player2Units;
                float dist = 10000000000;
                foreach (CombatUnit enemy in enemies)
                {
                    float newDist = unit.DistSq(enemy);
                    if (newDist > dist)
                        continue;
                    if (newDist <= 2 * 2)
                        continue;
                    if (!enemy.IsGround)
                        continue;
                    target = enemy;
                    TargetTag = enemy.Tag;
                    dist = newDist;
                }
            }
            return new AttackSiegeTank(target);
        }
    }
}
