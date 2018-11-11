using System.Collections.Generic;

namespace Tyr.Util
{
    public class CollectionUtil
    {
        public static void Increment<TKey>(Dictionary<TKey, int> dict, TKey key)
        {
            if (!dict.ContainsKey(key))
                dict.Add(key, 1);
            else
                dict[key]++;
        }
    }
}
