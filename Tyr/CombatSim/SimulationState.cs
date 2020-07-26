using Newtonsoft.Json;
using System.Collections.Generic;
using SC2Sharp.CombatSim.Actions;
using SC2Sharp.CombatSim.Buffs;
using SC2Sharp.CombatSim.DamageProcessors;
using SC2Sharp.Util;

namespace SC2Sharp.CombatSim
{
    public class SimulationState
    {
        public int SimulationFrame = 0;
        public List<CombatUnit> Player1Units = new List<CombatUnit>();
        public List<CombatUnit> Player2Units = new List<CombatUnit>();

        public void AddUnit(CombatUnit unit)
        {
            if (unit.Owner == 1)
                Player1Units.Add(unit);
            else if (unit.Owner == 2)
                Player2Units.Add(unit);
            else if (Bot.Debug)
                throw new System.ArgumentException("Can't add unit. Owner " + unit.Owner + "unknown.");
        }

        public CombatUnit GetUnit(long tag)
        {
            foreach (CombatUnit unit in Player1Units)
                if (unit.Tag == tag)
                    return unit;
            foreach (CombatUnit unit in Player2Units)
                if (unit.Tag == tag)
                    return unit;
            return null;
        }

        public CombatUnit GetUnit(long tag, int owner)
        {
            if (owner == 1)
            {
                foreach (CombatUnit unit in Player1Units)
                    if (unit.Tag == tag)
                        return unit;
            }
            else if (owner == 2)
            {
                foreach (CombatUnit unit in Player2Units)
                    if (unit.Tag == tag)
                        return unit;
            }
            return null;
        }

        public bool Simulate(int steps)
        {
            for (int i = 0; i < steps; i++)
            {
                if (Player1Units.Count == 0 || Player2Units.Count == 0)
                    return true;
                Step();
            }
            return false;
        }

        private void Step()
        {
            foreach (CombatUnit unit in Player1Units)
                StepUnit(unit);
            foreach (CombatUnit unit in Player2Units)
                StepUnit(unit);

            Clear(Player1Units);
            Clear(Player2Units);

            SimulationFrame++;
        }

        public void Clear(List<CombatUnit> units)
        {
            for (int i = units.Count - 1; i >= 0; i--)
            {
                CombatUnit unit = units[i];
                if (unit.Health <= 0)
                {
                    units[i] = units[units.Count - 1];
                    units.RemoveAt(units.Count - 1);
                }
            }
        }

        private void StepUnit(CombatUnit unit)
        {
            if (unit.Energy < unit.EnergyMax && unit.EnergyMax > 0)
                unit.Energy = System.Math.Min(unit.Energy + 0.03516f, unit.EnergyMax);

            if (unit.FramesUntilNextAttack > 0)
                unit.FramesUntilNextAttack--;
            if (unit.SecondaryAttackFrame == SimulationFrame && unit.AdditionalAttacksRemaining > 0)
                unit.PerformAdditionalAttack(this);

            Action action = unit.GetAction(this);
            
            if (!(action is Attack))
                unit.AdditionalAttacksRemaining = 0;
            action.Perform(this, unit);

            foreach (Buff buff in unit.Buffs)
                buff.OnFrame(this, unit);

            bool buffRemoved = false;
            for (int i = unit.Buffs.Count - 1; i >= 0; i--)
            {
                Buff buff = unit.Buffs[i];
                if (buff.ExpireFrame > SimulationFrame)
                    continue;
                buffRemoved = true;
                unit.Buffs.RemoveAt(i);
                if (buff is DamageProcessor)
                    unit.DamageProcessors.Remove((DamageProcessor)buff);
            }
            if (buffRemoved)
                unit.RecalculateBuffs();
        }

        public static SimulationState LoadFromFile(string filename)
        {
            string data = FileUtil.ReadFile(filename);

            var jsonSerializerSettings = new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.All,
                PreserveReferencesHandling = PreserveReferencesHandling.Objects
            };
            return JsonConvert.DeserializeObject<SimulationState>(data, jsonSerializerSettings);
        }

        public void SafeToFile(string filename)
        {
            FileUtil.WriteToFile(filename, Serialize(), true);
        }

        public string Serialize()
        {
            var jsonSerializerSettings = new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.All,
                PreserveReferencesHandling = PreserveReferencesHandling.Objects
            };
            return JsonConvert.SerializeObject(this, jsonSerializerSettings);
        }
    }
}
