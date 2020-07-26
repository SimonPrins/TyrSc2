using SC2APIProtocol;
using System.Collections.Generic;
using SC2Sharp.Agents;
using SC2Sharp.Managers;
using SC2Sharp.Util;

namespace SC2Sharp.Tasks
{
    class ClearOverlordsTask : Task
    {
        private Dictionary<ulong, Point2D> Overlords = new Dictionary<ulong, Point2D>();
        private Point2D Target;
        private ulong TargetTag;
        public ClearOverlordsTask() : base(7)
        { }

        public override bool DoWant(Agent agent)
        {
            return agent.Unit.UnitType == UnitTypes.STALKER && Target != null && units.Count == 0;
        }

        public override List<UnitDescriptor> GetDescriptors()
        {
            List<UnitDescriptor> result = new List<UnitDescriptor>();
            result.Add(new UnitDescriptor() { Count = 1, UnitTypes = new HashSet<uint>() { UnitTypes.STALKER } });
            return result;
        }

        public override bool IsNeeded()
        {
            return true;
        }

        public override void OnFrame(Bot bot)
        {
            float distance;
            if (Target == null)
                distance = 10000;
            else
            {
                if (units.Count > 0 && SC2Util.DistanceSq(units[0].Unit.Pos, Target) <= 5 * 5)
                {
                    bool dead = true;
                    foreach (Unit enemy in bot.Observation.Observation.RawData.Units)
                        if (enemy.Tag == TargetTag)
                        {
                            dead = false;
                            break;
                        }

                    if (dead)
                    {
                        Target = null;
                        distance = 10000;
                        TargetTag = 0;
                    }
                    else
                        distance = SC2Util.DistanceSq(bot.MapAnalyzer.StartLocation, Target) * 0.8f;
                }
                else
                    distance = SC2Util.DistanceSq(bot.MapAnalyzer.StartLocation, Target) * 0.8f;
            }
            foreach (Unit enemy in bot.Observation.Observation.RawData.Units)
            {
                if (enemy.Alliance != Alliance.Enemy)
                    continue;
                if (enemy.UnitType != UnitTypes.OVERLORD
                    && enemy.UnitType != UnitTypes.OVERSEER)
                    continue;

                if (enemy.Tag == TargetTag)
                    Target = SC2Util.To2D(enemy.Pos);
                
                Overlords[enemy.Tag] = SC2Util.To2D(enemy.Pos);
            }

            List<ulong> remove = new List<ulong>();
            foreach (KeyValuePair<ulong, Point2D> pair in Overlords)
            {
                bool nearEnemyBase = false;
                if (units.Count > 0 && SC2Util.DistanceSq(units[0].Unit.Pos, pair.Value) <= 5 * 5)
                {
                    bool dead = true;
                    foreach (Unit enemy in bot.Observation.Observation.RawData.Units)
                        if (enemy.Tag == pair.Key)
                        {
                            dead = false;
                            break;
                        }

                    if (dead)
                    {
                        remove.Add(pair.Key);
                        continue;
                    }
                }
                foreach (BuildingLocation loc in bot.EnemyManager.EnemyBuildings.Values)
                {
                    if (SC2Util.DistanceSq(loc.Pos, pair.Value) <= 8 * 8)
                    {
                        nearEnemyBase = true;
                        break;
                    }
                }
                if (nearEnemyBase)
                {
                    remove.Add(pair.Key);
                    continue;
                }

                float newDist = SC2Util.DistanceSq(pair.Value, bot.MapAnalyzer.StartLocation);
                if (newDist < distance)
                {
                    distance = newDist;
                    Target = pair.Value;
                    TargetTag = pair.Key;
                }
            }

            foreach (ulong tag in remove)
                Overlords.Remove(tag);
            
            if (Target == null)
                Clear();
            else
                foreach (Agent agent in units)
                    bot.MicroController.Attack(agent, Target);
        }
    }
}
