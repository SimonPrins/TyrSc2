using SC2APIProtocol;
using System;
using System.Collections.Generic;
using System.Linq;
using Tyr.Agents;
using Tyr.StrategyAnalysis;
using Tyr.Util;

namespace Tyr
{
    public class EnemyStrategyAnalyzer
    {
        public List<Strategy> Strategies = new List<Strategy>();

        public bool NoProxyTerranConfirmed;
        public bool NoProxyGatewayConfirmed;

        public HashSet<uint> EncounteredEnemies = new HashSet<uint>();

        public Dictionary<uint, int> EnemyCounts = new Dictionary<uint, int>();
        public Dictionary<ulong, uint> CountedEnemies = new Dictionary<ulong, uint>();
        public Dictionary<uint, int> TotalEnemyCounts = new Dictionary<uint, int>();

        public EnemyStrategyAnalyzer()
        {
            foreach (Type strategyType in typeof(Strategy).Assembly.GetTypes().Where(type => typeof(Strategy).IsAssignableFrom(type)))
            {
                if (strategyType.IsAbstract)
                    continue;
                Strategy strategy = (Strategy)strategyType.GetMethod("Get").Invoke(null, new object[0]);
                Strategies.Add(strategy);
            }
        }
        public void Load(string[] lines)
        {
            HashSet<string> lineSet = new HashSet<string>();
            foreach (string line in lines)
                lineSet.Add(line);

            foreach (Strategy strategy in Bot.Main.EnemyStrategyAnalyzer.Strategies)
                strategy.Load(lineSet);
        }

        public void Set(string name)
        {
            FileUtil.Register(name);
        }

        public void OnFrame(Bot tyr)
        {
            EnemyCounts = new Dictionary<uint, int>();
            foreach (Unit unit in tyr.Enemies())
            {
                if (!CountedEnemies.ContainsKey(unit.Tag) || CountedEnemies[unit.Tag] != unit.UnitType)
                {
                    if (!CountedEnemies.ContainsKey(unit.Tag))
                        CountedEnemies.Add(unit.Tag, unit.UnitType);
                    else
                        CountedEnemies[unit.Tag] = unit.UnitType;

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

            foreach (Strategy strategy in Strategies)
                strategy.OnFrame();

            if (!NoProxyTerranConfirmed
                && tyr.EnemyRace == Race.Terran
                && Expanded.Get().Detected)
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

            if (!NoProxyGatewayConfirmed
                && tyr.EnemyRace == Race.Protoss
                && Expanded.Get().Detected)
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
