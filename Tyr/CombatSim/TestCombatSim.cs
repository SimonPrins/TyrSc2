using SC2APIProtocol;
using System.Collections.Generic;
using System.Diagnostics;
using Tyr.Agents;
using Tyr.CombatSim.ActionProcessors;
using Tyr.CombatSim.Buffs;
using Tyr.CombatSim.CombatMicro;
using Tyr.Util;

namespace Tyr.CombatSim
{
    public class TestCombatSim
    {
        private static ulong Tag = 0;
        public static void Test()
        {
            SimulationState state = GetState();
            TestCombat(state);
        }

        private static void TestCombat(SimulationState state)
        {
            TestCombat(state, 1000);
        }

        public static void TestCombat(SimulationState state, int frames)
        {
            float previousHealth = 0;
            Stopwatch stopWatch = Stopwatch.StartNew();
            float time = stopWatch.ElapsedMilliseconds;
            
            for (int i = 0; i <= frames; i++)
            {
                float newHealth = 0;
                foreach (CombatUnit unit in state.Player1Units)
                    newHealth += unit.Health + unit.Shield;
                foreach (CombatUnit unit in state.Player2Units)
                    newHealth += unit.Health + unit.Shield;

                if (System.Math.Abs(newHealth - previousHealth) >= 3)
                {
                    previousHealth = newHealth;
                    FileUtil.Debug("Simulation frame: " + state.SimulationFrame);
                    FileUtil.Debug("My units:");
                    foreach (CombatUnit unit in state.Player1Units)
                        DebugUnit(unit);
                    FileUtil.Debug("Enemy units:");
                    foreach (CombatUnit unit in state.Player2Units)
                        DebugUnit(unit);
                    FileUtil.Debug("");
                }
                if (state.Simulate(1))
                {
                    FileUtil.Debug("Finished early at: " + state.SimulationFrame);
                    break;
                }
            }
            FileUtil.Debug("Time taken: " + (stopWatch.ElapsedMilliseconds - time));
            FileUtil.Debug("");
        }

        private static SimulationState GetState()
        {
            SimulationState state = new SimulationState();

            for (int i = 0; i < 3; i++)
                state.AddUnit(CreateUnit(UnitTypes.PHOTON_CANNON, true, -10, 150, 150, 0, 0, false, new HashSet<uint>() { UpgradeType.Charge }));

            for (int i = 0; i < 5; i++)
                state.AddUnit(CreateUnit(UnitTypes.STALKER, false, 10, 80, 80, 0, 0, false, new HashSet<uint>() { UpgradeType.ConcussiveShells }));
            return state;
        }

        private static CombatUnit CreateUnit(uint unitType, bool player1Owned, float x, int health, int shield, int energy, int energyMax, bool isFlying, HashSet<uint> upgrades)
        {
            Tag++;
            Unit unit = new Unit();
            unit.Owner = player1Owned ? (int)Bot.Bot.PlayerId : (3 - (int)Bot.Bot.PlayerId);
            unit.Pos = new SC2APIProtocol.Point() { X = x };
            unit.Health = health;
            unit.HealthMax = health;
            unit.Shield = shield;
            unit.ShieldMax = shield;
            unit.Energy = energy;
            unit.EnergyMax = energyMax;
            unit.UnitType = unitType;
            unit.Tag = Tag;
            unit.IsFlying = isFlying;
            CombatMicro.CombatMicro micro;
            if (unit.UnitType == UnitTypes.MEDIVAC)
                micro = new MedivacHealClosest();
            else if(unit.UnitType == UnitTypes.SIEGE_TANK_SIEGED)
                micro = new AttackClosestSiegeTank();
            else
                micro = new AttackClosest();
            CombatUnit result = SimulationUtil.FromUnit(unit, micro, upgrades);

            return result;
        }

        public static void TestSerialization(SimulationState state)
        {
            state.SafeToFile("SimulationState.txt");
        }

        public static SimulationState TestDeserialization()
        {
            return SimulationState.LoadFromFile("SimulationState.txt");
        }

        private static void DebugUnit(CombatUnit unit)
        {
            FileUtil.Debug("{");
            FileUtil.Debug("  Tag = " + unit.Tag);
            FileUtil.Debug("  Pos = " + unit.Pos);
            FileUtil.Debug("  Health = " + unit.Health);
            if (unit.ShieldMax > 0)
                FileUtil.Debug("  Shield = " + unit.Shield);
            if (unit.EnergyMax > 0)
                FileUtil.Debug("  Energy = " + unit.Energy);
            FileUtil.Debug("  MovementSpeed = " + unit.MovementSpeed);
            FileUtil.Debug("  Buffs = {");
            foreach (Buff buff in unit.Buffs)
                FileUtil.Debug("    " + buff.GetType().Name);
            FileUtil.Debug("  }");
            FileUtil.Debug("}");
        }
    }
}
