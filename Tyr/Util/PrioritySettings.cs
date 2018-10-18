using System.Collections.Generic;

namespace Tyr.Util
{
    public class PrioritySettings
    {
        public Dictionary<uint, int> TypePriorities = new Dictionary<uint, int>();
        public float MaxRange = 0;

        public int this[uint t]
        {
            get
            {
                if (!TypePriorities.ContainsKey(t))
                    return int.MinValue;
                else
                    return TypePriorities[t];
            }
            set
            {
                if (!TypePriorities.ContainsKey(t))
                    TypePriorities.Add(t, value);
                else
                    TypePriorities[t] = value;
            }
        }
    }
}
