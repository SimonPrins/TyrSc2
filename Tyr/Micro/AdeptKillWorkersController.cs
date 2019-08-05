using SC2APIProtocol;
using System.Collections.Generic;
using Tyr.Agents;
using Tyr.Util;

namespace Tyr.Micro
{
    public class AdeptKillWorkersController : CustomController
    {
        private Dictionary<ulong, int> TargetCount = new Dictionary<ulong, int>();
        private Dictionary<ulong, ulong> Targets = new Dictionary<ulong, ulong>();
        public override bool DetermineAction(Agent agent, Point2D target)
        {
            if (agent.Unit.UnitType != UnitTypes.ADEPT)
                return false;

            if (AttackTarget(agent))
                return true;

            float dist = 10 * 10;
            bool alreadyTargeted = false;
            Unit killTarget = null;
            foreach (Unit enemy in Tyr.Bot.Enemies())
            {
                if (!UnitTypes.WorkerTypes.Contains(enemy.UnitType))
                    continue;

                int newTargetted = CollectionUtil.Get(TargetCount, enemy.Tag);
                if (newTargetted >= 2)
                    continue;

                if (newTargetted == 0 && alreadyTargeted)
                    continue;

                float newDist = agent.DistanceSq(enemy);
                if (newDist >= 10 * 10)
                    continue;
                if (newDist >= dist
                    && (newTargetted == 0 || alreadyTargeted))
                    continue;
                killTarget = enemy;
                dist = newDist;
                alreadyTargeted = newTargetted > 0;
            }

            if (killTarget != null)
            {
                CollectionUtil.Increment(TargetCount, killTarget.Tag);
                CollectionUtil.Set(Targets, agent.Unit.Tag, killTarget.Tag);
                agent.Order(Abilities.ATTACK, killTarget.Tag);
                return true;
            }

            return false;
        }

        private bool AttackTarget(Agent agent)
        {
            if (Targets.ContainsKey(agent.Unit.Tag))
            {
                ulong target = Targets[agent.Unit.Tag];
                if (!Tyr.Bot.EnemyManager.LastSeenFrame.ContainsKey(target)
                    || Tyr.Bot.EnemyManager.LastSeenFrame[target] <= Tyr.Bot.Frame - 1)
                {
                    Targets.Remove(agent.Unit.Tag);
                    TargetCount[target]--;
                    return false;
                }
                if (Tyr.Bot.EnemyManager.LastSeen[target] != null
                    && agent.DistanceSq(Tyr.Bot.EnemyManager.LastSeen[target]) >= 12 * 12)
                {
                    Targets.Remove(agent.Unit.Tag);
                    TargetCount[target]--;
                    return false;
                }
                agent.Order(Abilities.ATTACK, target);
                return true;
            }
            return false;
        }
    }
}
