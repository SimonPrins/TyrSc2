using System.Collections.Generic;

namespace SC2Sharp.Util
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

        public static T RemoveAt<T>(List<T> list, int i)
        {
            T result = list[i];
            list[i] = list[list.Count - 1];
            list.RemoveAt(list.Count - 1);
            return result;
        }

        public static int Get<U>(Dictionary<U, int> dict, U key)
        {
            if (dict.ContainsKey(key))
                return dict[key];
            else
                return 0;
        }

        public static void Set<U>(Dictionary<ulong, U> dict, ulong key, U value)
        {
            if (dict.ContainsKey(key))
                dict[key] = value;
            else dict.Add(key, value);
        }
    }
}
