using SC2APIProtocol;
using System.Collections.Generic;
using Tyr.Agents;
using Tyr.Util;

namespace Tyr
{
    public class EnemyStrategyAnalyzer
    {
        public bool CannonRushDetected;
        public bool LiftingDetected;
        public bool MassRoachDetected;
        public bool MassHydraDetected;
        public bool EarlyPool;
        public bool FourRaxDetected;
        public bool NoProxyTerranConfirmed;
        public bool ReaperRushDetected;
        public bool TerranTechDetected;
        public bool MechDetected;
        public bool BioDetected;
        public bool Expanded;
        public bool ThreeGateDetected;
        public bool NoProxyGatewayConfirmed;
        public bool WorkerRushDetected;
        public bool SkyTossDetected;

        public HashSet<uint> EncounteredEnemies = new HashSet<uint>();

        public Dictionary<uint, int> EnemyCounts = new Dictionary<uint, int>();
        public HashSet<ulong> CountedEnemies = new HashSet<ulong>();
        public Dictionary<uint, int> TotalEnemyCounts = new Dictionary<uint, int>();

        public void OnFrame(Tyr tyr)
        {
            EnemyCounts = new Dictionary<uint, int>();
            foreach (Unit unit in tyr.Enemies())
            {
                if (!CountedEnemies.Contains(unit.Tag))
                {
                    CountedEnemies.Add(unit.Tag);

                    if (!TotalEnemyCounts.ContainsKey(unit.UnitType))
                        TotalEnemyCounts.Add(unit.UnitType, 1);
                    else
                        TotalEnemyCounts[unit.UnitType]++;
                }
                EncounteredEnemies.Add(unit.UnitType);
                if (!EnemyCounts.ContainsKey(unit.UnitType))
                    EnemyCounts.Add(unit.UnitType, 1);
                else
                    EnemyCounts[unit.UnitType]++;
            }

            if (Count(UnitTypes.ROACH) >= 10)
            {
                MassRoachDetected = true;
                tyr.PreviousEnemyStrategies.SetMassRoach();
            }
            if (Count(UnitTypes.HYDRALISK) >= 10)
            {
                MassHydraDetected = true;
                tyr.PreviousEnemyStrategies.SetMassHydra();
            }

            if (!CannonRushDetected)
            {
                foreach (Unit unit in tyr.Enemies())
                {
                    if (unit.UnitType != UnitTypes.PYLON && unit.UnitType != UnitTypes.PHOTON_CANNON)
                        continue;
                    if (SC2Util.DistanceSq(unit.Pos, SC2Util.To2D(tyr.MapAnalyzer.StartLocation)) <= 40 * 40)
                    {
                        CannonRushDetected = true;
                        tyr.PreviousEnemyStrategies.SetCannonRush();
                        break;
                    }
                }
            }

            if (!LiftingDetected)
                foreach (Unit unit in tyr.Enemies())
                    if (unit.IsFlying && UnitTypes.BuildingTypes.Contains(unit.UnitType))
                    {
                        LiftingDetected = true;
                        tyr.PreviousEnemyStrategies.SetLifting();
                        break;
                    }

            if (!EarlyPool)
            {
                if (Count(UnitTypes.HATCHERY) < 2 && Count(UnitTypes.SPAWNING_POOL) > 0 && tyr.Frame <= 60f * 22.4f * 1.75f)
                    EarlyPool = true;
            }

            if (Count(UnitTypes.NEXUS)
                + Count(UnitTypes.HATCHERY)
                + Count(UnitTypes.LAIR)
                + Count(UnitTypes.HIVE)
                + Count(UnitTypes.COMMAND_CENTER)
                + Count(UnitTypes.COMMAND_CENTER_FLYING)
                + Count(UnitTypes.ORBITAL_COMMAND)
                + Count(UnitTypes.ORBITAL_COMMAND_FLYING)
                + Count(UnitTypes.PLANETARY_FORTRESS) >= 2)
                Expanded = true;

            // When we encounter three barracks within 3 minutes of the game, we assume the enemy probably has a fourth one somewhere as well.
            if (!FourRaxDetected
                && tyr.Frame <= 22.4 * 60 * 3
                && Count(UnitTypes.BARRACKS) + Count(UnitTypes.BARRACKS_FLYING) >= 3)
            {
                FourRaxDetected = true;
                tyr.PreviousEnemyStrategies.SetFourRax();
            }

            if (!NoProxyTerranConfirmed
                && tyr.EnemyRace == Race.Terran
                && Expanded)
            {
                NoProxyTerranConfirmed = true;
            }

            if (!NoProxyTerranConfirmed
                    && tyr.EnemyRace == Race.Terran)
            {
                foreach (Unit unit in tyr.Enemies())
                {
                    if (unit.UnitType == UnitTypes.BARRACKS
                        && SC2Util.DistanceSq(tyr.MapAnalyzer.StartLocation, unit.Pos) >= 40 * 40)
                    {
                        NoProxyTerranConfirmed = true;
                        break;
                    }
                }
            }

            if (!ReaperRushDetected
                && Count(UnitTypes.REAPER) >= 2
                && tyr.Frame <= 22.4 * 60 * 4)
            {
                ReaperRushDetected = true;
                tyr.PreviousEnemyStrategies.SetReaperRush();
            }

            if (!TerranTechDetected
                && (Count(UnitTypes.SIEGE_TANK) + Count(UnitTypes.SIEGE_TANK_SIEGED) > 0
                    || Count(UnitTypes.MEDIVAC) > 0
                    || Count(UnitTypes.BANSHEE) > 0
                    || Count(UnitTypes.THOR) > 0
                    || Count(UnitTypes.HELLION) + Count(UnitTypes.HELLBAT) >= 2
                    || Count(UnitTypes.GHOST) > 0
                    || Count(UnitTypes.MARAUDER) >= 3)
                    || Count(UnitTypes.LIBERATOR) > 0
                    || Count(UnitTypes.WIDOW_MINE) >= 2
                    || Count(UnitTypes.CYCLONE) > 0)
            {
                TerranTechDetected = true;
                tyr.PreviousEnemyStrategies.SetTerranTech();
            }

            if (!MechDetected
                && (Count(UnitTypes.THOR) > 0
                    || Count(UnitTypes.HELLION) + Count(UnitTypes.HELLBAT) >= 5
                    || Count(UnitTypes.CYCLONE) > 2))
            {
                MechDetected = true;
                tyr.PreviousEnemyStrategies.SetMech();
            }

            if (!BioDetected
                && (Count(UnitTypes.MEDIVAC) > 0
                    || Count(UnitTypes.MARAUDER) + Count(UnitTypes.MARINE) >= 20))
            {
                BioDetected = true;
                tyr.PreviousEnemyStrategies.SetBio();
            }

            if (!NoProxyGatewayConfirmed
                && tyr.EnemyRace == Race.Protoss
                && Expanded)
            {
                NoProxyGatewayConfirmed = true;
            }

            if (!NoProxyGatewayConfirmed
                    && tyr.EnemyRace == Race.Protoss)
            {
                foreach (Unit unit in tyr.Enemies())
                {
                    if (unit.UnitType == UnitTypes.GATEWAY
                        && SC2Util.DistanceSq(tyr.MapAnalyzer.StartLocation, unit.Pos) >= 40 * 40)
                    {
                        NoProxyGatewayConfirmed = true;
                        break;
                    }
                }
            }

            if (!ThreeGateDetected
                && tyr.Frame <= 22.4 * 60 * 3
                && Count(UnitTypes.GATEWAY) >= 3)
            {
                ThreeGateDetected = true;
                tyr.PreviousEnemyStrategies.SetThreeGate();
            }

            if (!WorkerRushDetected)
            {
                int farWorkers = 0;
                foreach (Unit unit in tyr.Enemies())
                {
                    if (!UnitTypes.WorkerTypes.Contains(unit.UnitType))
                        continue;

                    // See if this worker is far from the enemy base.
                    bool far = true;
                    foreach (Point2D start in tyr.TargetManager.PotentialEnemyStartLocations)
                        if (SC2Util.DistanceSq(unit.Pos, start) <= 40 * 40)
                            far = false;

                    if (far)
                        farWorkers++;
                }
                if (farWorkers >= 5)
                    WorkerRushDetected = true;
            }

            if (!SkyTossDetected && Tyr.Bot.EnemyRace == Race.Protoss || Tyr.Bot.EnemyRace == Race.Random)
            {
                if (Count(UnitTypes.CARRIER) + Count(UnitTypes.MOTHERSHIP) + Count(UnitTypes.INTERCEPTOR) > 0)
                {
                    SkyTossDetected = true;
                    tyr.PreviousEnemyStrategies.SetSkyToss();
                }
            }
        }

        public int Count(uint unitType)
        {
            if (!EnemyCounts.ContainsKey(unitType))
                return 0;
            return EnemyCounts[unitType];
        }

        public int TotalCount(uint unitType)
        {
            if (!TotalEnemyCounts.ContainsKey(unitType))
                return 0;
            return TotalEnemyCounts[unitType];
        }
    }
}
