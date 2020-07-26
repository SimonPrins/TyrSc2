using SC2APIProtocol;
using System;
using System.Collections.Generic;
using SC2Sharp.Agents;
using SC2Sharp.CombatSim;
using SC2Sharp.CombatSim.CombatMicro;
using SC2Sharp.Tasks;
using SC2Sharp.Util;

namespace SC2Sharp.Managers
{
    public class CombatSimulation
    {
        public float EnemyDistance = 15;
        public float AllyDistance = 3;
        public int SimulationLength = 300;
        public bool Debug = false;
        public bool ShowStats = false;

        public float MyStartResources;
        public float MyFinalResources;
        public float EnemyStartResources;
        public float EnemyFinalResources;

        public void OnFrame(Bot bot)
        {
            List<Unit> simulatedUnits = new List<Unit>();
            foreach (Task task in bot.TaskManager.Tasks)
                task.AddCombatSimulationUnits(simulatedUnits);

            foreach (Unit unit in bot.EnemyManager.LastSeen.Values)
            {
                if (unit.UnitType == UnitTypes.SIEGE_TANK_SIEGED || unit.UnitType == UnitTypes.WIDOW_MINE_BURROWED)
                    continue;
                if (bot.EnemyManager.LastSeen.ContainsKey(unit.Tag) && bot.Frame - bot.EnemyManager.LastSeenFrame[unit.Tag] > 112)
                    continue;
                if (UnitTypes.CombatUnitTypes.Contains(unit.UnitType))
                    simulatedUnits.Add(unit);
            }
            foreach (UnitLocation unit in bot.EnemyMineManager.Mines)
                if (bot.EnemyManager.LastSeen.ContainsKey(unit.Tag))
                    simulatedUnits.Add(bot.EnemyManager.LastSeen[unit.Tag]);
            foreach (UnitLocation unit in bot.EnemyTankManager.Tanks)
                if (bot.EnemyManager.LastSeen.ContainsKey(unit.Tag))
                    simulatedUnits.Add(bot.EnemyManager.LastSeen[unit.Tag]);

            List<List<Unit>> simulationGroups = GroupUnits(simulatedUnits);

            HashSet<uint> myUpgrades = new HashSet<uint>();
            if (bot.Observation.Observation.RawData.Player.UpgradeIds != null)
                foreach (uint upgrade in bot.Observation.Observation.RawData.Player.UpgradeIds)
                    myUpgrades.Add(upgrade);
            HashSet<uint> enemyUpgrades = new HashSet<uint>();

            bool logSimulation = bot.Frame % 22 == 0 && simulationGroups.Count > 0 && Debug;

            if (ShowStats)
                bot.DrawText("Simulations: " + simulationGroups.Count);
            if (logSimulation)
                FileUtil.Debug("Simulations: " + simulationGroups.Count);


            bool printState = false;
            if (Bot.Debug && bot.Observation.Chat != null && bot.Observation.Chat.Count > 0)
            {
                foreach (ChatReceived message in bot.Observation.Chat)
                {
                    if (message.Message == "s")
                    {
                        printState = true;
                        break;
                    }
                }
            }

            MyStartResources = 0;
            MyFinalResources = 0;
            EnemyStartResources = 0;
            EnemyFinalResources = 0;
            int i = 0;
            foreach (List<Unit> simulationGroup in simulationGroups)
            {
                SimulationState state = GetState(bot, simulationGroup, myUpgrades, enemyUpgrades, false);
                if (printState)
                    state.SafeToFile("SimulationState-" + bot.Frame + "-" + i + ".txt");
                float myResources = GetResources(state, true);
                float enemyResources = GetResources(state, false);
                if (printState)
                    TestCombatSim.TestCombat(state, SimulationLength);
                else
                    state.Simulate(SimulationLength);

                float myNewResources = GetResources(state, true);
                float enemyNewResources = GetResources(state, false);
                if (ShowStats)
                    bot.DrawText("SimulationResult me: " + myResources + " -> " + myNewResources + " his: " + enemyResources + " -> " + enemyNewResources);
                if (logSimulation)
                    FileUtil.Debug("SimulationResult me: " + myResources + " -> " + myNewResources + " his: " + enemyResources + " -> " + enemyNewResources);

                if (myResources > 0
                    && enemyResources > 0)
                {
                    MyStartResources += myResources;
                    MyFinalResources += myNewResources;
                    EnemyStartResources += enemyResources;
                    EnemyFinalResources += enemyNewResources;
                }

                MakeDecision(simulationGroup, state, myResources, myNewResources, enemyResources, enemyNewResources, myUpgrades, enemyUpgrades);
                i++;
            }

            if (logSimulation)
                FileUtil.Debug("");
        }

        private List<List<Unit>> GroupUnits(List<Unit> simulatedUnits)
        {
            List<List<Unit>> simulationGroups = new List<List<Unit>>();

            while (simulatedUnits.Count > 0)
            {
                List<Unit> simulationGroup = new List<Unit>();

                simulationGroup.Add(simulatedUnits[simulatedUnits.Count - 1]);
                simulatedUnits.RemoveAt(simulatedUnits.Count - 1);

                for (int j = 0; j < simulationGroup.Count; j++)
                {
                    Unit current = simulationGroup[j];
                    for (int i = simulatedUnits.Count - 1; i >= 0; i--)
                    {
                        Unit compare = simulatedUnits[i];
                        if (SC2Util.DistanceSq(current.Pos, compare.Pos) > (current.Owner != Bot.Main.PlayerId || compare.Owner != Bot.Main.PlayerId ? EnemyDistance * EnemyDistance : AllyDistance * AllyDistance))
                            continue;
                        simulationGroup.Add(compare);
                        CollectionUtil.RemoveAt(simulatedUnits, i);
                    }
                }
                simulationGroups.Add(simulationGroup);
            }
            return simulationGroups;
        }

        private SimulationState GetState(Bot bot, List<Unit> simulationGroup, HashSet<uint> myUpgrades, HashSet<uint> enemyUpgrades, bool flee)
        {
            SimulationState state = new SimulationState();
            foreach (Unit unit in simulationGroup)
            {

                List<CombatMicro> micro;
                if (flee && unit.Owner == bot.PlayerId)
                    micro = new List<CombatMicro>() { new Flee() };
                else
                    micro = GetMicro(unit);
                state.AddUnit(SimulationUtil.FromUnit(unit, micro, unit.Owner == bot.PlayerId ? myUpgrades : enemyUpgrades));
            }
            return state;
        }

        private List<CombatMicro> GetMicro(Unit unit)
        {
            CombatMicro micro;

            if (unit.UnitType == UnitTypes.MEDIVAC)
                micro = new MedivacHealClosest();
            else if (unit.UnitType == UnitTypes.SIEGE_TANK_SIEGED)
                micro = new AttackClosestSiegeTank();
            else
                micro = new AttackClosest();

            return new List<CombatMicro>() { micro };
        }

        private float GetResources(SimulationState state, bool mine)
        {
            float resources = 0;
            foreach (CombatUnit unit in mine ? state.Player1Units : state.Player2Units)
                if (UnitTypes.LookUp.ContainsKey(unit.UnitType))
                    resources += UnitTypes.LookUp[unit.UnitType].MineralCost + UnitTypes.LookUp[unit.UnitType].VespeneCost * 2;
            return resources;
        }

        private void MakeDecision(List<Unit> simulationGroup, SimulationState state, float myResources, float myNewResources, float enemyResources, float enemyNewResources, HashSet<uint> myUpgrades, HashSet<uint> enemyUpgrades)
        {
            if (myResources == 0)
                return;
            if (enemyResources == 0)
            {
                ApplyDecision(simulationGroup, CombatSimulationDecision.None);
                return;
            }

            int prevProceed = 0;
            int prevFallBack = 0;
            foreach (Unit unit in simulationGroup)
                if (Bot.Main.UnitManager.Agents.ContainsKey(unit.Tag) && Bot.Main.Frame - Bot.Main.UnitManager.Agents[unit.Tag].CombatSimulationDecisionFrame < 10)
                {
                    if (Bot.Main.UnitManager.Agents[unit.Tag].CombatSimulationDecision == CombatSimulationDecision.Proceed)
                        prevProceed++;
                    else if (Bot.Main.UnitManager.Agents[unit.Tag].CombatSimulationDecision == CombatSimulationDecision.FallBack)
                        prevFallBack++;
                }

            float partProceed;
            if (prevFallBack + prevProceed == 0)
                partProceed = 0;
            else
                partProceed = (float)prevProceed / (prevProceed + prevFallBack);
            if (ShowStats)
                Bot.Main.DrawText("Proceed: " + partProceed);
            if (enemyResources - enemyNewResources >= (myResources - myNewResources) * (1.1 - 0.3 * partProceed))
                ApplyDecision(simulationGroup, CombatSimulationDecision.Proceed);
            else
            {
                SimulationState fleeState = GetState(Bot.Main, simulationGroup, myUpgrades, enemyUpgrades, true);
                state.Simulate(100);
                float myFleeResources = GetResources(fleeState, true);
                if (enemyResources - enemyNewResources >= (myFleeResources - myNewResources) * (1.1 - 0.3 * partProceed))
                    ApplyDecision(simulationGroup, CombatSimulationDecision.Proceed);
                else
                    ApplyDecision(simulationGroup, CombatSimulationDecision.FallBack);
            }
        }

        private void ApplyDecision(List<Unit> simulationGroup, CombatSimulationDecision decision)
        {
            foreach (Unit unit in simulationGroup)
                if (Bot.Main.UnitManager.Agents.ContainsKey(unit.Tag))
                {
                    Bot.Main.UnitManager.Agents[unit.Tag].CombatSimulationDecision = decision;
                    Bot.Main.UnitManager.Agents[unit.Tag].CombatSimulationDecisionFrame = Bot.Main.Frame;
                }
        }
    }
}
