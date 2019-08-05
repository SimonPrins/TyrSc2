using SC2APIProtocol;
using System;
using System.Collections.Generic;
using Tyr.Agents;
using Tyr.CombatSim;
using Tyr.CombatSim.CombatMicro;
using Tyr.Tasks;
using Tyr.Util;

namespace Tyr.Managers
{
    public class CombatSimulation
    {
        public float EnemyDistance = 15;
        public float AllyDistance = 3;
        public int SimulationLength = 300;
        public bool Debug = false;

        public void OnFrame(Tyr tyr)
        {
            List<Unit> simulatedUnits = new List<Unit>();
            foreach (Task task in tyr.TaskManager.Tasks)
                task.AddCombatSimulationUnits(simulatedUnits);

            foreach (Unit unit in tyr.EnemyManager.LastSeen.Values)
            {
                if (unit.UnitType == UnitTypes.SIEGE_TANK_SIEGED || unit.UnitType == UnitTypes.WIDOW_MINE_BURROWED)
                    continue;
                if (tyr.EnemyManager.LastSeen.ContainsKey(unit.Tag) && tyr.Frame - tyr.EnemyManager.LastSeenFrame[unit.Tag] > 112)
                    continue;
                if (UnitTypes.CombatUnitTypes.Contains(unit.UnitType))
                    simulatedUnits.Add(unit);
            }
            foreach (UnitLocation unit in tyr.EnemyMineManager.Mines)
                if (tyr.EnemyManager.LastSeen.ContainsKey(unit.Tag))
                    simulatedUnits.Add(tyr.EnemyManager.LastSeen[unit.Tag]);
            foreach (UnitLocation unit in tyr.EnemyTankManager.Tanks)
                if (tyr.EnemyManager.LastSeen.ContainsKey(unit.Tag))
                    simulatedUnits.Add(tyr.EnemyManager.LastSeen[unit.Tag]);

            List<List<Unit>> simulationGroups = GroupUnits(simulatedUnits);

            HashSet<uint> myUpgrades = new HashSet<uint>();
            if (tyr.Observation.Observation.RawData.Player.UpgradeIds != null)
                foreach (uint upgrade in tyr.Observation.Observation.RawData.Player.UpgradeIds)
                    myUpgrades.Add(upgrade);
            HashSet<uint> enemyUpgrades = new HashSet<uint>();

            bool logSimulation = tyr.Frame % 22 == 0 && simulationGroups.Count > 0 && Debug;

            tyr.DrawText("Simulations: " + simulationGroups.Count);
            if (logSimulation)
                FileUtil.Debug("Simulations: " + simulationGroups.Count);


            bool printState = false;
            if (Tyr.Debug && tyr.Observation.Chat != null && tyr.Observation.Chat.Count > 0)
            {
                foreach (ChatReceived message in tyr.Observation.Chat)
                {
                    if (message.Message == "s")
                    {
                        printState = true;
                        break;
                    }
                }
            }

            int i = 0;
            foreach (List<Unit> simulationGroup in simulationGroups)
            {
                SimulationState state = GetState(tyr, simulationGroup, myUpgrades, enemyUpgrades, false);
                if (printState)
                    state.SafeToFile("SimulationState-" + tyr.Frame + "-" + i + ".txt");
                float myResources = GetResources(state, true);
                float enemyResources = GetResources(state, false);
                if (printState)
                    TestCombatSim.TestCombat(state, SimulationLength);
                else
                    state.Simulate(SimulationLength);

                float myNewResources = GetResources(state, true);
                float enemyNewResources = GetResources(state, false);
                tyr.DrawText("SimulationResult me: " + myResources + " -> " + myNewResources + " his: " + enemyResources + " -> " + enemyNewResources);
                if (logSimulation)
                    FileUtil.Debug("SimulationResult me: " + myResources + " -> " + myNewResources + " his: " + enemyResources + " -> " + enemyNewResources);

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
                        if (SC2Util.DistanceSq(current.Pos, compare.Pos) > (current.Owner != Tyr.Bot.PlayerId || compare.Owner != Tyr.Bot.PlayerId ? EnemyDistance * EnemyDistance : AllyDistance * AllyDistance))
                            continue;
                        simulationGroup.Add(compare);
                        CollectionUtil.RemoveAt(simulatedUnits, i);
                    }
                }
                simulationGroups.Add(simulationGroup);
            }
            return simulationGroups;
        }

        private SimulationState GetState(Tyr tyr, List<Unit> simulationGroup, HashSet<uint> myUpgrades, HashSet<uint> enemyUpgrades, bool flee)
        {
            SimulationState state = new SimulationState();
            foreach (Unit unit in simulationGroup)
            {

                List<CombatMicro> micro;
                if (flee && unit.Owner == tyr.PlayerId)
                    micro = new List<CombatMicro>() { new Flee() };
                else
                    micro = GetMicro(unit);
                state.AddUnit(SimulationUtil.FromUnit(unit, micro, unit.Owner == tyr.PlayerId ? myUpgrades : enemyUpgrades));
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
                if (Tyr.Bot.UnitManager.Agents.ContainsKey(unit.Tag) && Tyr.Bot.Frame - Tyr.Bot.UnitManager.Agents[unit.Tag].CombatSimulationDecisionFrame < 10)
                {
                    if (Tyr.Bot.UnitManager.Agents[unit.Tag].CombatSimulationDecision == CombatSimulationDecision.Proceed)
                        prevProceed++;
                    else if (Tyr.Bot.UnitManager.Agents[unit.Tag].CombatSimulationDecision == CombatSimulationDecision.FallBack)
                        prevFallBack++;
                }

            float partProceed;
            if (prevFallBack + prevProceed == 0)
                partProceed = 0;
            else
                partProceed = (float)prevProceed / (prevProceed + prevFallBack);
            Tyr.Bot.DrawText("Proceed: " + partProceed);
            if (enemyResources - enemyNewResources >= (myResources - myNewResources) * (1.1 - 0.3 * partProceed))
                ApplyDecision(simulationGroup, CombatSimulationDecision.Proceed);
            else
            {
                SimulationState fleeState = GetState(Tyr.Bot, simulationGroup, myUpgrades, enemyUpgrades, true);
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
                if (Tyr.Bot.UnitManager.Agents.ContainsKey(unit.Tag))
                {
                    Tyr.Bot.UnitManager.Agents[unit.Tag].CombatSimulationDecision = decision;
                    Tyr.Bot.UnitManager.Agents[unit.Tag].CombatSimulationDecisionFrame = Tyr.Bot.Frame;
                }
        }
    }
}
