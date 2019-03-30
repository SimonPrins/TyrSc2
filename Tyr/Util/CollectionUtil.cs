using System;
using System.Collections.Generic;
using Tyr.Micro;

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

        public static void Add<TKey, TValue>(Dictionary<TKey, TValue> dict, TKey key, TValue value)
        {
            if (!dict.ContainsKey(key))
                dict.Add(key, value);
            else
                dict[key] = value;
        }
    }
}
