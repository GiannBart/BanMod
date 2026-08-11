////credits and licenses in the resources folder
//using BanMod;
//using System.Collections.Generic;
//using System.IO;
//using System.Reflection;
//using System.Text.Json;

//namespace BanMod;

//public static class OptionSaver
//{
//    [Obfuscation(Exclude = true)]
//    private static readonly DirectoryInfo SaveDataDirectoryInfo = new("./BAN_DATA/SETTINGS/SaveData/");
//    [Obfuscation(Exclude = true)]
//    private static readonly FileInfo OptionSaverFileInfo = new($"{SaveDataDirectoryInfo.FullName}/Options.json");

//    public static void Initialize()
//    {
//        if (!SaveDataDirectoryInfo.Exists)
//        {
//            SaveDataDirectoryInfo.Create();
//            SaveDataDirectoryInfo.Attributes |= FileAttributes.Hidden;
//        }
//        if (!OptionSaverFileInfo.Exists)
//        {
//            OptionSaverFileInfo.Create().Dispose();
//        }
//    }

//    public static bool DeleteSaveData(bool recreate = true)
//    {
//        try
//        {
//            if (!SaveDataDirectoryInfo.Exists)
//            {
//                SaveDataDirectoryInfo.Create();
//                SaveDataDirectoryInfo.Attributes |= FileAttributes.Hidden;
//            }

//            OptionSaverFileInfo.Refresh();

//            if (OptionSaverFileInfo.Exists)
//            {
//                OptionSaverFileInfo.Delete();
//                BMLogger.Info("Options.json deleted successfully.", "OptionSaver.DeleteSaveData");
//            }
//            else
//            {
//                BMLogger.Info("Options.json does not exist.", "OptionSaver.DeleteSaveData");
//            }

//            if (recreate)
//            {
//                Save();
//            }

//            return true;
//        }
//        catch (System.Exception error)
//        {
//            BMLogger.Error($"Error: {error}", "OptionSaver.DeleteSaveData");
//            return false;
//        }
//    }

//    public static void ResetSaveData()
//    {
//        if (DeleteSaveData(recreate: true))
//        {
//            BMLogger.Info("Save data reset completed.", "OptionSaver.ResetSaveData");
//        }
//    }

//    private static SerializableOptionsData GenerateOptionsData()
//    {
//        Dictionary<int, int> singleOptions = [];

//        foreach (var option in OptionItem.AllOptions)
//        {
//            if (option.IsSingleValue)
//            {
//                if (!singleOptions.TryAdd(option.Id, option.SingleValue))
//                {
//                    BMLogger.Warn($"Duplicate SingleOption ID: {option.Id}", "Option Saver");
//                }
//            }
//        }

//        return new SerializableOptionsData
//        {
//            Version = Version,
//            SingleOptions = singleOptions,
//        };
//    }
//    private static void LoadOptionsData(SerializableOptionsData serializableOptionsData)
//    {
//        if (serializableOptionsData == null)
//        {
//            Save();
//            return;
//        }

//        if (serializableOptionsData.Version != Version)
//        {
//            Save();
//            return;
//        }

//        Dictionary<int, int> singleOptions = serializableOptionsData.SingleOptions;

//        if (singleOptions == null)
//        {
//            Save();
//            return;
//        }

//        foreach (var singleOption in singleOptions)
//        {
//            var id = singleOption.Key;
//            var value = singleOption.Value;
//            if (OptionItem.FastOptions.TryGetValue(id, out var optionItem))
//            {
//                optionItem.SetValue(value, doSave: false);
//            }
//        }
//    }

//    public static void Save()
//    {
//        if (AmongUsClient.Instance != null && !AmongUsClient.Instance.AmHost) return;

//        try
//        {
//            if (!SaveDataDirectoryInfo.Exists)
//            {
//                SaveDataDirectoryInfo.Create();
//                SaveDataDirectoryInfo.Attributes |= FileAttributes.Hidden;
//            }

//            var jsonString = JsonSerializer.Serialize(GenerateOptionsData(), new JsonSerializerOptions { WriteIndented = true, });
//            File.WriteAllText(OptionSaverFileInfo.FullName, jsonString);
//        }
//        catch (System.Exception error)
//        {
//            BMLogger.Error($"Error: {error}", "OptionSaver.Save");
//        }
//    }

//    public static void Load()
//    {
//        try
//        {
//            if (!SaveDataDirectoryInfo.Exists)
//            {
//                SaveDataDirectoryInfo.Create();
//                SaveDataDirectoryInfo.Attributes |= FileAttributes.Hidden;
//            }

//            if (!OptionSaverFileInfo.Exists)
//            {
//                Save();
//                return;
//            }

//            var jsonString = File.ReadAllText(OptionSaverFileInfo.FullName);

//            if (jsonString.Length <= 0)
//            {
//                Save();
//                return;
//            }

//            LoadOptionsData(JsonSerializer.Deserialize<SerializableOptionsData>(jsonString));
//        }
//        catch (System.Exception error)
//        {
//            BMLogger.Error($"Error: {error}", "OptionSaver.Load");
//            Save();
//        }
//    }

//    [Obfuscation(Exclude = true, ApplyToMembers = true)]
//    public class SerializableOptionsData
//    {
//        public int Version { get; init; }
//        public Dictionary<int, int> SingleOptions { get; init; }
//    }

//    public static readonly int Version = 1;
//}
// credits and licenses in the resources folder

using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace BanMod;

public static class OptionSaver
{
    [Obfuscation(Exclude = true)]
    private static readonly DirectoryInfo SaveDataDirectoryInfo =
        new("./BAN_DATA/SETTINGS/SaveData/");

    [Obfuscation(Exclude = true)]
    private static readonly FileInfo OptionSaverFileInfo =
        new($"{SaveDataDirectoryInfo.FullName}/Options.json");

    public static readonly int Version = 2;

    public static void Initialize()
    {
        EnsureSaveDirectory();

        if (!OptionSaverFileInfo.Exists)
            OptionSaverFileInfo.Create().Dispose();
    }

    public static bool DeleteSaveData(bool recreate = true)
    {
        try
        {
            EnsureSaveDirectory();

            OptionSaverFileInfo.Refresh();

            if (OptionSaverFileInfo.Exists)
            {
                OptionSaverFileInfo.Delete();

                BMLogger.Info(
                    "Options.json deleted successfully.",
                    "OptionSaver.DeleteSaveData"
                );
            }
            else
            {
                BMLogger.Info(
                    "Options.json does not exist.",
                    "OptionSaver.DeleteSaveData"
                );
            }

            if (recreate)
                Save();

            return true;
        }
        catch (System.Exception error)
        {
            BMLogger.Error(
                $"Error: {error}",
                "OptionSaver.DeleteSaveData"
            );

            return false;
        }
    }

    public static void ResetSaveData()
    {
        if (DeleteSaveData(recreate: true))
        {
            BMLogger.Info(
                "Save data reset completed.",
                "OptionSaver.ResetSaveData"
            );
        }
    }

    private static void EnsureSaveDirectory()
    {
        if (SaveDataDirectoryInfo.Exists)
            return;

        SaveDataDirectoryInfo.Create();
        SaveDataDirectoryInfo.Attributes |= FileAttributes.Hidden;
    }

    private static string GetSaveKey(OptionItem option)
    {
        // Gli ID ora sono automatici e possono cambiare tra versioni.
        // Category + Name resta invece stabile e distingue anche nomi
        // ripetuti come "Action".
        return $"{option.Category}:{option.Name}";
    }

    private static SerializableOptionsData GenerateOptionsData()
    {
        Dictionary<string, int> singleOptions = new();

        foreach (var option in OptionItem.AllOptions)
        {
            if (!option.IsSingleValue)
                continue;

            string key = GetSaveKey(option);

            if (!singleOptions.TryAdd(key, option.SingleValue))
            {
                BMLogger.Warn(
                    $"Duplicate option save key: {key}",
                    "OptionSaver"
                );
            }
        }

        return new SerializableOptionsData
        {
            Version = Version,
            SingleOptions = singleOptions
        };
    }

    private static void LoadOptionsData(
        SerializableOptionsData serializableOptionsData)
    {
        if (serializableOptionsData == null)
        {
            Save();
            return;
        }

        if (serializableOptionsData.Version != Version)
        {
            BMLogger.Info(
                $"Options save version changed: {serializableOptionsData.Version} -> {Version}. Recreating save data.",
                "OptionSaver.Load"
            );

            Save();
            return;
        }

        Dictionary<string, int> singleOptions =
            serializableOptionsData.SingleOptions;

        if (singleOptions == null)
        {
            Save();
            return;
        }

        Dictionary<string, OptionItem> optionsByKey = new();

        foreach (var option in OptionItem.AllOptions)
        {
            if (!option.IsSingleValue)
                continue;

            string key = GetSaveKey(option);

            if (!optionsByKey.TryAdd(key, option))
            {
                BMLogger.Warn(
                    $"Duplicate runtime option save key: {key}",
                    "OptionSaver.Load"
                );
            }
        }

        foreach (var savedOption in singleOptions)
        {
            if (!optionsByKey.TryGetValue(
                    savedOption.Key,
                    out var optionItem))
            {
                continue;
            }

            optionItem.SetValue(
                savedOption.Value,
                doSave: false,
                doSync: false
            );
        }

        OptionItem.SyncAllOptions();
    }

    public static void Save()
    {
        if (AmongUsClient.Instance != null &&
            !AmongUsClient.Instance.AmHost)
        {
            return;
        }

        try
        {
            EnsureSaveDirectory();

            string jsonString = JsonSerializer.Serialize(
                GenerateOptionsData(),
                new JsonSerializerOptions
                {
                    WriteIndented = true
                }
            );

            File.WriteAllText(
                OptionSaverFileInfo.FullName,
                jsonString
            );
        }
        catch (System.Exception error)
        {
            BMLogger.Error(
                $"Error: {error}",
                "OptionSaver.Save"
            );
        }
    }

    public static void Load()
    {
        try
        {
            EnsureSaveDirectory();

            OptionSaverFileInfo.Refresh();

            if (!OptionSaverFileInfo.Exists)
            {
                Save();
                return;
            }

            string jsonString =
                File.ReadAllText(OptionSaverFileInfo.FullName);

            if (string.IsNullOrWhiteSpace(jsonString))
            {
                Save();
                return;
            }

            SerializableOptionsData data =
                JsonSerializer.Deserialize<SerializableOptionsData>(
                    jsonString
                );

            LoadOptionsData(data);
        }
        catch (System.Exception error)
        {
            BMLogger.Error(
                $"Error: {error}",
                "OptionSaver.Load"
            );

            Save();
        }
    }

    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public class SerializableOptionsData
    {
        public int Version { get; init; }

        public Dictionary<string, int> SingleOptions { get; init; }
    }
}
