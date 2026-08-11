//credits and licenses in the resources folder
using System;
using System.Collections.Generic;
using System.Linq;

namespace BanMod;

public static class EnumHelper
{
    public static T[] GetAllValues<T>() where T : Enum => Enum.GetValues(typeof(T)) as T[];
    public static string[] GetAllNames<T>() where T : Enum => Enum.GetNames(typeof(T));
    public static List<TEnum[]> Achunk<TEnum>(int chunkSize, bool shuffle = false, Func<TEnum, bool> exclude = null) where TEnum : Enum
    {
        List<TEnum[]> chunkedList = [];
        TEnum[] allValues = GetAllValues<TEnum>();
        if (exclude != null) allValues = allValues.Where(exclude).ToArray();

        for (int i = 0; i < allValues.Length; i += chunkSize)
        {
            TEnum[] chunk = new TEnum[Math.Min(chunkSize, allValues.Length - i)];
            Array.Copy(allValues, i, chunk, 0, chunk.Length);
            chunkedList.Add(chunk);
        }

        return chunkedList;

    }
}
