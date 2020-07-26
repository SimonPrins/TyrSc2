using Newtonsoft.Json;
using System.Collections.Generic;
using SC2Sharp.CombatSim.Actions;

namespace SC2Sharp.CombatSim.CombatMicro
{
    [JsonObject(MemberSerialization.Fields)]
    public class Flee : CombatMicro
    {
        private long TargetTag = 0;
        public Action Act(SimulationState state, CombatUnit unit)
        {
            CombatUnit target = null;
            if (TargetTag != 0)
                target = state.GetUnit(TargetTag, 3 - unit.Owner);
            if (target == null)
            {
                List<CombatUnit> enemies = unit.Owner == 2 ? state.Player1Units : state.Player2Units;
                float dist = 10000000000;
                foreach (CombatUnit enemy in enemies)
                {
                    float newDist = unit.DistSq(enemy);
                    if (newDist > dist)
                        continue;
                    if (unit.GetWeapon(enemy) == null)
                        continue;
                    target = enemy;
                    TargetTag = enemy.Tag;
                    dist = newDist;
                }
            }
            return new Move(target.Pos, false);
        }
    }
}
