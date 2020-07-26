using SC2APIProtocol;
using System.Collections.Generic;
using SC2Sharp.Agents;
using SC2Sharp.Util;

namespace SC2Sharp.Micro
{
    public class TargetObserversController : CustomController
    {
        private ulong TargetObserver = 0;
        private Unit Observer = null;
        private int UpdatedFrame = 0;
        private Dictionary<ulong, int> FocusTempests = new Dictionary<ulong, int>();
        public override bool DetermineAction(Agent agent, Point2D target)
        {
            if (agent.Unit.UnitType != UnitTypes.TEMPEST)
                return false;
            UpdateTarget();

            if (FocusTempests.ContainsKey(agent.Unit.Tag))
            {
                if (agent.DistanceSq(Observer) <= 14 * 14)
                {
                    FocusTempests[agent.Unit.Tag] = Bot.Main.Frame;
                    agent.Order(Abilities.ATTACK, Observer.Tag);
                    return true;
                }
                FocusTempests.Remove(agent.Unit.Tag);
                return false;
            }

            if (TargetObserver != 0 && FocusTempests.Count >= 2)
                return false;

            if (TargetObserver != 0)
            {
                if (agent.DistanceSq(Observer) <= 14 * 14)
                {
                    FocusTempests[agent.Unit.Tag] = Bot.Main.Frame;
                    agent.Order(Abilities.ATTACK, Observer.Tag);
                    return true;
                }
                return false;
            }

            foreach (Unit enemy in Bot.Main.EnemyManager.GetEnemies())
            {
                if (enemy.UnitType != UnitTypes.OBSERVER)
                    continue;
                if (agent.DistanceSq(enemy) >= 14 * 14)
                    continue;
                TargetObserver = enemy.Tag;
                Observer = enemy;

                FocusTempests[agent.Unit.Tag] = Bot.Main.Frame;
                agent.Order(Abilities.ATTACK, Observer.Tag);
                return true;
            }

            return false;
        }

        public void UpdateTarget()
        {
            if (Bot.Main.Frame == UpdatedFrame)
                return;
            UpdatedFrame = Bot.Main.Frame;

            if (TargetObserver != 0)
            {
                bool found = false;
                foreach (Unit enemy in Bot.Main.EnemyManager.GetEnemies())
                {
                    if (enemy.Tag == TargetObserver)
                    {
                        found = true;
                        Observer = enemy;
                        break;
                    }
                }
                if (!found)
                {
                    TargetObserver = 0;
                    Observer = null;
                    FocusTempests.Clear();
                }
            }

            List<ulong> remove = new List<ulong>();
            foreach (ulong tag in FocusTempests.Keys)
                if (FocusTempests[tag] < Bot.Main.Frame - 1)
                    remove.Add(tag);
            foreach (ulong tag in remove)
                FocusTempests.Remove(tag);
        }
    }
}
