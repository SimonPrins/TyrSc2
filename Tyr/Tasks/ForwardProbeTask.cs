using SC2APIProtocol;
using System;
using System.Collections.Generic;
using SC2Sharp.Agents;
using SC2Sharp.Managers;
using SC2Sharp.MapAnalysis;
using SC2Sharp.Util;

namespace SC2Sharp.Tasks
{
    class ForwardProbeTask : Task
    {
        public static ForwardProbeTask Task = new ForwardProbeTask();

        private Point2D EnemyMain;
        private Point2D EnemyNatural;
        private List<Base> EnemyBases = new List<Base>();
        public int EnemyBaseRange = 60;

        public ForwardProbeTask() : base(8)
        { }

        public static void Enable()
        {
            Enable(Task);
        }

        public override bool DoWant(Agent agent)
        {
            if (BuildingType.BuildingAbilities.Contains((int)agent.CurrentAbility()))
                return false;
            return agent.IsWorker && units.Count == 0;
        }

        public override List<UnitDescriptor> GetDescriptors()
        {
            List<UnitDescriptor> result = new List<UnitDescriptor>();
            result.Add(new UnitDescriptor() { Pos = Bot.Main.TargetManager.AttackTarget, Count = 1, UnitTypes = UnitTypes.WorkerTypes });
            return result;
        }

        public override bool IsNeeded()
        {
            return Bot.Main.Frame > 1;
        }

        public override void OnFrame(Bot bot)
        {
            UpdateEnemyBases();
            if (units.Count == 0)
                return;
            Bot.Main.DrawText("Enemy bases for scouting: " + EnemyBases.Count);
            Base target = null;
            float dist = 1000000;
            foreach (Base loc in EnemyBases)
            {
                float newDist = Units[0].DistanceSq(loc.BaseLocation.Pos);
                if (newDist < dist)
                {
                    dist = newDist;
                    target = loc;
                }
            }

            foreach (Agent agent in units)
            {
                Unit fleeEnemy = null;
                float enemyDist = 13 * 13;
                foreach (Unit enemy in bot.Enemies())
                {
                    if (!UnitTypes.CanAttackGround(enemy.UnitType))
                        continue;
                    float newDist = agent.DistanceSq(enemy);
                    if (newDist < enemyDist)
                    {
                        enemyDist = newDist;
                        fleeEnemy = enemy;
                    }
                }

                if (fleeEnemy != null)
                {
                    agent.Order(Abilities.MOVE, agent.From(fleeEnemy, 4));
                    continue;
                }

                if (target != null)
                {
                    agent.Order(Abilities.MOVE, target.BaseLocation.Pos);
                    if (RemoveBase(target, agent))
                            EnemyBases.Remove(target);
                }
            }
        }

        private bool RemoveBase(Base target, Agent agent)
        {
            if (agent.DistanceSq(target.BaseLocation.Pos) <= 6 * 6)
                return true;
            if (target.Owner == -1)
                return false;

            foreach (Unit enemy in Bot.Main.Enemies())
            {
                if (enemy.UnitType != UnitTypes.PHOTON_CANNON
                    && enemy.UnitType != UnitTypes.BUNKER
                    && enemy.UnitType != UnitTypes.SPINE_CRAWLER
                    && !UnitTypes.ResourceCenters.Contains(enemy.UnitType))
                    continue;
                if (SC2Util.DistanceSq(enemy.Pos, target.BaseLocation.Pos) > 8 * 8)
                    continue;
                return true;
            }
            return false;
        }
        
        private void UpdateEnemyBases()
        {
            if (EnemyBases.Count > 0)
                return;

            if (EnemyMain == null && Bot.Main.TargetManager.PotentialEnemyStartLocations.Count == 1)
                EnemyMain = Bot.Main.TargetManager.PotentialEnemyStartLocations[0];
            if (EnemyNatural == null && Bot.Main.TargetManager.PotentialEnemyStartLocations.Count == 1)
            {
                BaseLocation enemyNaturalBase = Bot.Main.MapAnalyzer.GetEnemyNatural();
                if (enemyNaturalBase != null)
                    EnemyNatural = enemyNaturalBase.Pos;
            }
            
            foreach (Base b in Bot.Main.BaseManager.Bases)
            {
                if (b.Owner != -1)
                    continue;
                if (SC2Util.DistanceSq(b.BaseLocation.Pos, EnemyNatural) <= 2 * 2)
                    continue;
                float enemyMainDistance = SC2Util.DistanceSq(b.BaseLocation.Pos, EnemyMain);
                if (enemyMainDistance <= 2 * 2 || enemyMainDistance >= EnemyBaseRange * EnemyBaseRange)
                    continue;
                EnemyBases.Add(b);
            }
        }
    }
}
