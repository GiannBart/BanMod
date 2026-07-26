//credits and licenses in the resources folder
using AmongUs.GameOptions;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Reflection.Emit;

namespace BanMod;

public static class Translator
{
    public const string LANGUAGE_FOLDER_NAME = "Language";
    public static Dictionary<string, Dictionary<int, string>> translateMaps;

    public static void Initialize()
    {
        LoadLangs();

        foreach (var lang in Enum.GetValues<SupportedLangs>())
        {
            if (File.Exists(@$"./{LANGUAGE_FOLDER_NAME}/{lang}.dat"))
            {
                UpdateCustomTranslation($"{lang}.dat");
                LoadCustomTranslation($"{lang}.dat", lang);
            }
        }

        TranslationAudit.RunStartupAudit();
    }

    public static void LoadLangs()
    {
        try
        {
            string jsonDirectory = "BanMod.Resources.Lang";
            var assembly = Assembly.GetExecutingAssembly();
            string[] jsonFileNames = GetJsonFileNames(assembly, jsonDirectory);

            translateMaps = [];

            if (jsonFileNames.Length == 0)
            {
                return;
            }

            foreach (string jsonFileName in jsonFileNames)
            {
                using Stream resourceStream = assembly.GetManifestResourceStream(jsonFileName);
                if (resourceStream != null)
                {
                    using StreamReader reader = new(resourceStream);
                    string jsonContent = reader.ReadToEnd();

                    Dictionary<string, string> jsonDictionary = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonContent);
                    if (jsonDictionary.TryGetValue("LanguageID", out string languageIdObj) && int.TryParse(languageIdObj, out int languageId))
                    {
                        jsonDictionary.Remove("LanguageID");

                        MergeJsonIntoTranslationMap(translateMaps, languageId, jsonDictionary);
                    }
                }
            }

            JsonSerializer.Serialize(translateMaps, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }
        catch (Exception ex)
        {
            BMLogger.Error($"Error: {ex}", "Translator");
        }

        if (!Directory.Exists(LANGUAGE_FOLDER_NAME)) Directory.CreateDirectory(LANGUAGE_FOLDER_NAME);

        CreateTemplateFile();
        foreach (var lang in Enum.GetValues<SupportedLangs>())
        {
            if (File.Exists(@$"./{LANGUAGE_FOLDER_NAME}/{lang}.dat"))
            {
                UpdateCustomTranslation($"{lang}.dat" /*, lang*/);
                LoadCustomTranslation($"{lang}.dat", lang);
            }
        }
    }

    static void MergeJsonIntoTranslationMap(Dictionary<string, Dictionary<int, string>> translationMaps, int languageId, Dictionary<string, string> jsonDictionary)
    {
        foreach (var kvp in jsonDictionary)
        {
            string textString = kvp.Key;
            if (kvp.Value is string translation)
            {
                if (!translationMaps.ContainsKey(textString))
                {
                    translationMaps[textString] = [];
                }

                translationMaps[textString][languageId] = translation.Replace("\\n", "\n").Replace("\\r", "\r");
            }
        }
    }

    static string[] GetJsonFileNames(Assembly assembly, string directoryName)
    {
        string[] resourceNames = assembly.GetManifestResourceNames();
        return resourceNames.Where(resourceName => resourceName.StartsWith(directoryName) && resourceName.EndsWith(".json")).ToArray();
    }

    public static string GetString(string s, Dictionary<string, string> replacementDic = null, bool console = false)
    {
        var langId = TranslationController.InstanceExists ? TranslationController.Instance.currentLanguage.languageID : SupportedLangs.English;
        if (console) langId = SupportedLangs.English;
        langId = GetUserTrueLang();
        string str = GetString(s, langId);
        if (replacementDic != null)
            foreach (var rd in replacementDic)
            {
                str = str.Replace(rd.Key, rd.Value);
            }

        return str;
    }

    public static string GetString(string str, SupportedLangs langId)
    {
        var res = $"<INVALID:{str}>";
        try
        {
            if (translateMaps.TryGetValue(str, out var dic) && (!dic.TryGetValue((int)langId, out res) || res == "" || (langId is not SupportedLangs.SChinese and not SupportedLangs.TChinese && Regex.IsMatch(res, @"[\u4e00-\u9fa5]") && res == GetString(str, SupportedLangs.SChinese)))) //str?????&???langId?res??
            {
                res = langId == SupportedLangs.English ? $"*{str}" : GetString(str, SupportedLangs.English);
            }

            if (!translateMaps.ContainsKey(str)) 
            {
                var stringNames = Enum.GetValues<StringNames>().Where(x => x.ToString() == str).ToArray();
                if (stringNames.Length > 0)
                    res = GetString(stringNames.FirstOrDefault());
            }
        }
        catch (Exception Ex)
        {
            BMLogger.Fatal($"Error oucured at [{str}] in String.csv", "Translator");
            BMLogger.Error("Here was the error:\n" + Ex, "Translator");
        }

        return res;
    }

    public static string GetString(StringNames stringName)
        => DestroyableSingleton<TranslationController>.Instance.GetString(stringName, new Il2CppReferenceArray<Il2CppSystem.Object>(0));

    public static string GetAuto(string key, params object[] args)
    {
        string raw = GetString(key);
        return string.Format(raw, args);
    }

    public static SupportedLangs GetUserTrueLang()
    {
        try
        {
            var name = CultureInfo.CurrentUICulture.Name;
            if (name.StartsWith("en")) return SupportedLangs.English;
            if (name.StartsWith("zh_CHT")) return SupportedLangs.TChinese;
            if (name.StartsWith("it_IT")) return SupportedLangs.Italian;
            if (name.StartsWith("zh")) return SupportedLangs.SChinese;
            if (name.StartsWith("ru")) return SupportedLangs.Russian;
            return TranslationController.Instance.currentLanguage.languageID;
        }
        catch
        {
            return SupportedLangs.English;
        }
    }

    static void UpdateCustomTranslation(string filename)
    {
        string path = @$"./{LANGUAGE_FOLDER_NAME}/{filename}";
        if (File.Exists(path))
        {
            try
            {
                List<string> textStrings = [];
                using (StreamReader reader = new(path, Encoding.GetEncoding("UTF-8")))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        string[] parts = line.Split(':');

                        if (parts.Length >= 1)
                        {
                            string textString = parts[0].Trim();
                            textStrings.Add(textString);
                        }
                    }
                }

                var sb = new StringBuilder();
                foreach (var templateString in translateMaps.Keys)
                {
                    if (!textStrings.Contains(templateString)) sb.Append($"{templateString}:\n");
                }

                using FileStream fileStream = new(path, FileMode.Append, FileAccess.Write);
                using StreamWriter writer = new(fileStream);
                writer.WriteLine(sb.ToString());
            }
            catch (Exception e)
            {
                BMLogger.Error("An error occurred: " + e.Message, "Translator");
            }
        }
    }

    public static void LoadCustomTranslation(string filename, SupportedLangs lang)
    {
        string path = @$"./{LANGUAGE_FOLDER_NAME}/{filename}";
        if (File.Exists(path))
        {
            try
            {
                using StreamReader sr = new(path, Encoding.GetEncoding("UTF-8"));
                string text;
                string[] tmp = [];
                while ((text = sr.ReadLine()) != null)
                {
                    tmp = text.Split(":");
                    if (tmp.Length > 1 && tmp[1] != "")
                    {
                        try
                        {
                            translateMaps[tmp[0]][(int)lang] = tmp.Skip(1).Join(delimiter: ":").Replace("\\n", "\n").Replace("\\r", "\r");
                        }
                        catch (KeyNotFoundException)
                        {
                            BMLogger.Warn($"Invalid Key?{tmp[0]}", "LoadCustomTranslation");
                        }
                    }
                }
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception e)
            {
                BMLogger.Error(e.ToString(), "Translator.LoadCustomTranslation");
            }
        }
        else
        {
            BMLogger.Error($"Custom Translation File Not Found?{filename}", "LoadCustomTranslation");
        }
    }

    private static void CreateTemplateFile()
    {
        var sb = new StringBuilder();
        foreach (var title in translateMaps) sb.Append($"{title.Key}:\n");
        File.WriteAllText(@$"./{LANGUAGE_FOLDER_NAME}/template.dat", sb.ToString());
    }

    public static void ExportCustomTranslation()
    {
        LoadLangs();
        var sb = new StringBuilder();
        var lang = TranslationController.Instance.currentLanguage.languageID;
        foreach (var title in translateMaps)
        {
            var text = title.Value.GetValueOrDefault((int)lang, "");
            sb.Append($"{title.Key}:{text.Replace("\n", "\\n").Replace("\r", "\\r")}\n");
        }

        File.WriteAllText(@$"./{LANGUAGE_FOLDER_NAME}/export_{lang}.dat", sb.ToString());
    }
}