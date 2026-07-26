//credits and licenses in the resources folder
using System;
using System.Collections.Generic;

namespace BanMod
{
    public interface IRandom
    {
        public int Next(int maxValue);
        public int Next(int minValue, int maxValue);

        public static Dictionary<int, Type> randomTypes = new()
        {
            { 0, typeof(NetRandomWrapper) },
        };

        public static IRandom Instance { get; private set; }
        public static void SetInstance(IRandom instance)
        {
            if (instance != null)
                Instance = instance;
        }

        public static void SetInstanceById(int id)
        {
            if (randomTypes.TryGetValue(id, out var type))
            {
                if (Instance == null || Instance.GetType() != type)
                {
                    Instance = Activator.CreateInstance(type) as IRandom ?? Instance;
                }
            }
            else BMLogger.Warn($"???ID: {id}", "IRandom.SetInstanceById");
        }
        public class NetRandomWrapper : IRandom
        {
            public Random wrapping;

            public NetRandomWrapper() : this(new Random())
            { }
            public NetRandomWrapper(int seed) : this(new Random(seed))
            { }
            public NetRandomWrapper(Random instance)
            {
                wrapping = instance;
            }

            public int Next(int minValue, int maxValue) => wrapping.Next(minValue, maxValue);
            public int Next(int maxValue) => wrapping.Next(maxValue);
            public int Next() => wrapping.Next();
        }
    }
}
