using System;
using System.Collections.Generic;
using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Managers;
using Tyr.Util;

namespace Tyr.Tasks
{
    public class WorkerDefenseTask : Task
    {
        public static List<WorkerDefenseTask> Tasks = new List<WorkerDefenseTask>();
        public int DefenseRadius = 12;
        public int PlanetaryDefenseRadius = 8;
        public int WorkerPullRadius = 20;
        public int CannonDefenseRadius = 40;
        public Base Base;
        private int desiredDefenders;
        private Unit target;
        public bool OnlyDefendInsideMain = false;
        private bool DefendProxy;
        private bool CannonsFinished;

        public WorkerDefenseTask(Base b) : base(7)
        {
            Base = b;
        }

        public static void Enable()
        {
            if (Tasks.Count == 0)
            {
                foreach (Base b in Tyr.Bot.BaseManager.Bases)
                {
                    WorkerDefenseTask task = new WorkerDefenseTask(b);
                    Tasks.Add(task);
                }
            }

            foreach (Task task in Tasks)
            {
                task.Stopped = false;
                Tyr.Bot.TaskManager.Add(task);
            }
        }

        public override bool DoWant(Agent agent)
        {
            return agent.IsWorker
                && (agent.Unit.UnitType != UnitTypes.PROBE || agent.Unit.Shield >= agent.Unit.ShieldMax - 2f)
                && agent.Unit.Shield + agent.Unit.Health > 5
                && (SC2Util.DistanceSq(agent.Unit.Pos, Base.BaseLocation.Pos) <= WorkerPullRadius * WorkerPullRadius || DefendProxy)
                && SC2Util.DistanceSq(agent.Unit.Pos, Base.BaseLocation.Pos) <= CannonDefenseRadius * CannonDefenseRadius
                && units.Count < desiredDefenders
                && !BuildingType.BuildingAbilities.Contains((int)agent.CurrentAbility());
        }

        public override bool IsNeeded()
        {
            if (Base.ResourceCenter != null && Base.ResourceCenter.Unit.UnitType == UnitTypes.PLANETARY_FORTRESS)
            {
                Clear();
                return false;
            }
            desiredDefenders = 0;
            target = null;
            int totalEnemies = 0;
            int combatEnemies = 0;
            float distance = DefenseRadius * DefenseRadius + 1;

            bool mainDefense = Base == Tyr.Bot.BaseManager.Main;
            if (mainDefense)
                distance = 1000 * 1000;
            else if (Base.ResourceCenter != null && Base.ResourceCenter.Unit.UnitType == UnitTypes.PLANETARY_FORTRESS)
                distance = PlanetaryDefenseRadius * PlanetaryDefenseRadius + 1;

            Dictionary<uint, int> enemyCounts = new Dictionary<uint, int>();

            DefendProxy = false;

            int combatUnits = 0;
            foreach (Agent agent in Tyr.Bot.UnitManager.Agents.Values)
                if (UnitTypes.CombatUnitTypes.Contains(agent.Unit.UnitType))
                    combatUnits++;
            bool alreadyDefended = combatUnits >= 5;

            if (Base.Owner != Tyr.Bot.PlayerId)
            {
                Clear();
                return false;
            }
            foreach (Unit unit in Tyr.Bot.Enemies())
            {
                if (unit.IsFlying)
                    continue;

                if (unit.UnitType == UnitTypes.ADEPT_PHASE_SHIFT
                    || unit.UnitType == UnitTypes.KD8_CHARGE)
                    continue;

                if (unit.UnitType == UnitTypes.CHANGELING
                    || unit.UnitType == UnitTypes.CHANGELING_MARINE
                    || unit.UnitType == UnitTypes.CHANGELING_MARINE_SHIELD
                    || unit.UnitType == UnitTypes.CHANGELING_ZEALOT
                    || unit.UnitType == UnitTypes.CHANGELING_ZERGLING
                    || unit.UnitType == UnitTypes.CHANGELING_ZERGLING_WINGS)
                    continue;

                bool enemyInMain = Tyr.Bot.MapAnalyzer.MainAndPocketArea[(int)unit.Pos.X, (int)unit.Pos.Y];

                if ((OnlyDefendInsideMain || mainDefense) && !enemyInMain)
                    continue;
                
                float newDist = SC2Util.DistanceSq(unit.Pos, Base.BaseLocation.Pos);
                

                if (Tyr.Bot.EnemyStrategyAnalyzer.TotalCount(UnitTypes.ZEALOT) + Tyr.Bot.EnemyStrategyAnalyzer.TotalCount(UnitTypes.STALKER) == 0 && !alreadyDefended && (newDist < CannonDefenseRadius * CannonDefenseRadius + 1 || (enemyInMain && mainDefense)))
                {
                    if (UnitTypes.BuildingTypes.Contains(unit.UnitType))
                    {
                        DefendProxy = true;
                        totalEnemies++;
                        if (unit.BuildProgress >= 0.95 
                            && (unit.UnitType == UnitTypes.PHOTON_CANNON || unit.UnitType == UnitTypes.SPINE_CRAWLER))
                        {
                            Clear();
                            return false;
                        }
                        if (target == null || newDist < distance)
                        {
                            distance = newDist;
                            target = unit;
                            continue;
                        }
                    }
                }
                if (newDist < DefenseRadius * DefenseRadius + 1 || (enemyInMain && mainDefense))
                {
                    totalEnemies++;
                    if (UnitTypes.CombatUnitTypes.Contains(unit.UnitType))
                        combatEnemies++;
                    if (!enemyCounts.ContainsKey(unit.UnitType))
                        enemyCounts.Add(unit.UnitType, 0);
                    enemyCounts[unit.UnitType]++;
                }

                if (newDist > distance)
                    continue;

                distance = newDist;
                target = unit;
            }

            // Do not suicide against overwhelming enemy forces.
            if (Count(enemyCounts, UnitTypes.MARINE) + Count(enemyCounts, UnitTypes.MARAUDER) >= 4
                || Count(enemyCounts, UnitTypes.HELLION) + Count(enemyCounts, UnitTypes.HELLBAT) >= 2
                || Count(enemyCounts, UnitTypes.ROACH) >= 3
                || Count(enemyCounts, UnitTypes.HYDRALISK) >= 3
                || Count(enemyCounts, UnitTypes.ADEPT) >= 1
                || Count(enemyCounts, UnitTypes.STALKER) >= 2
                || Count(enemyCounts, UnitTypes.ZEALOT) >= 3
                || Count(enemyCounts, UnitTypes.ZERGLING) >= 11
                || Count(enemyCounts, UnitTypes.BANELING) > 0
                || Count(enemyCounts, UnitTypes.COLOSUS) > 0
                || Count(enemyCounts, UnitTypes.REAPER) > 0
                || Count(enemyCounts, UnitTypes.WIDOW_MINE) > 0
                || Count(enemyCounts, UnitTypes.WIDOW_MINE_BURROWED) > 0
                || Count(enemyCounts, UnitTypes.REACTOR) > 0
                || combatEnemies - Count(enemyCounts, UnitTypes.ZERGLING) >= 9)
            {
                desiredDefenders = 0;
                Clear();
                return false;
            }

            if (combatEnemies > 8)
            {
                int myCombatUnits = 0;
                bool enoughDefenses = false;
                foreach (Agent agent in Tyr.Bot.UnitManager.Agents.Values)
                    if (UnitTypes.CombatUnitTypes.Contains(agent.Unit.UnitType))
                    {
                        myCombatUnits++;
                        if (myCombatUnits >= 5)
                        {
                            enoughDefenses = true;
                            break;
                        }
                    }

                if (enoughDefenses)
                {
                    desiredDefenders = 0;
                    Clear();
                    return false;
                }
            }

            if (target != null && totalEnemies == 1 && UnitTypes.WorkerTypes.Contains(target.UnitType))
                desiredDefenders = 1;
            else if (Tyr.Bot.EnemyStrategyAnalyzer.WorkerRushDetected)
                desiredDefenders = totalEnemies;
            else
                desiredDefenders = 3 * totalEnemies + (DefendProxy ? 2 : 0);

            if (desiredDefenders == 0 && units.Count > 0)
                Clear();

            return desiredDefenders > 0;
        }

        public override List<UnitDescriptor> GetDescriptors()
        {
            List<UnitDescriptor> result = new List<UnitDescriptor>();
            result.Add(new UnitDescriptor() { Pos = target == null ? null : SC2Util.To2D(target.Pos), Count = Math.Max(0, desiredDefenders - units.Count), UnitTypes = UnitTypes.WorkerTypes });
            return result;
        }

        private int Count(Dictionary<uint, int> enemyCounts, uint unitType)
        {
            if (!enemyCounts.ContainsKey(unitType))
                return 0;
            return enemyCounts[unitType];
        }

        public override void OnFrame(Tyr tyr)
        {
            if (Units.Count > 0)
                tyr.DrawText("Defending workers: " + Units.Count);
            if (target == null)
            {
                Clear();
                return;
            }

            while (Units.Count > desiredDefenders)
                IdleTask.Task.Add(PopAt(Units.Count - 1));

            // Remove probes whose shields have depleted.
            if (tyr.MyRace == Race.Protoss)
                for (int i = units.Count - 1; i >= 0; i--)
                    if (units[i].Unit.Shield <= 1)
                    {
                        IdleTask.Task.Add(units[i]);
                        units[i] = units[units.Count - 1];
                        units.RemoveAt(units.Count - 1);
                    }
            // Remove low health workers.
                for (int i = units.Count - 1; i >= 0; i--)
                    if (units[i].Unit.Shield + units[i].Unit.Health <= 5)
                    {
                        IdleTask.Task.Add(units[i]);
                        units[i] = units[units.Count - 1];
                        units.RemoveAt(units.Count - 1);
                    }

            foreach (Agent agent in units)
                agent.Order(Abilities.ATTACK, SC2Util.To2D(target.Pos));
        }
    }
}
