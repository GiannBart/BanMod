using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Text.RegularExpressions;

namespace BanMod;

public static class TranslationAudit
{
    private static readonly object Unknown = new();

    private static readonly OpCode[] OneByteOpCodes = new OpCode[0x100];
    private static readonly OpCode[] TwoByteOpCodes = new OpCode[0x100];

    // Lingue che vuoi controllare.
    // Aggiungi o rimuovi qui se la mod supporta altre lingue.
    private static readonly SupportedLangs[] LanguagesToCheck =
    [
        SupportedLangs.English,
        SupportedLangs.Italian,
        SupportedLangs.Russian,
        SupportedLangs.SChinese,
        SupportedLangs.TChinese
    ];

    static TranslationAudit()
    {
        foreach (var field in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.GetValue(null) is not OpCode opCode)
                continue;

            ushort value = unchecked((ushort)opCode.Value);

            if (value < 0x100)
                OneByteOpCodes[value] = opCode;
            else if ((value & 0xff00) == 0xfe00)
                TwoByteOpCodes[value & 0xff] = opCode;
        }
    }

    public static void RunStartupAudit()
    {
        try
        {
            Directory.CreateDirectory(Translator.LANGUAGE_FOLDER_NAME);

            var assembly = Assembly.GetExecutingAssembly();

            var usedKeys = FindTranslationKeysUsedByMod(assembly);
            var jsonKeys = Translator.translateMaps?.Keys.ToHashSet() ?? [];

            var allKeys = new HashSet<string>(jsonKeys);
            allKeys.UnionWith(usedKeys);

            var report = new StringBuilder();

            report.AppendLine("BANMOD TRANSLATION AUDIT");
            report.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine($"Assembly: {assembly.GetName().Name}");
            report.AppendLine();

            report.AppendLine($"Keys used in code: {usedKeys.Count}");
            report.AppendLine($"Keys found in JSON/custom maps: {jsonKeys.Count}");
            report.AppendLine($"Total keys checked: {allKeys.Count}");
            report.AppendLine();

            WriteKeysUsedButNotInAnyJson(report, usedKeys, jsonKeys);
            WriteMissingTranslationsByLanguage(report, allKeys);
            WriteSuspiciousFallbackTranslations(report, allKeys);
            WritePlaceholderProblems(report, allKeys);

            string path = $"./{Translator.LANGUAGE_FOLDER_NAME}/missing_translations.txt";
            File.WriteAllText(path, report.ToString(), Encoding.UTF8);

            BMLogger.Warn($"Translation audit completed. Check {path}", "TranslationAudit");
        }
        catch (Exception e)
        {
            BMLogger.Error(e.ToString(), "TranslationAudit");
        }
    }

    private static void WriteKeysUsedButNotInAnyJson(StringBuilder report, HashSet<string> usedKeys, HashSet<string> jsonKeys)
    {
        var missingEverywhere = usedKeys
            .Where(key => !jsonKeys.Contains(key))
            .OrderBy(key => key)
            .ToList();

        report.AppendLine("=== KEYS USED IN CODE BUT MISSING FROM EVERY JSON ===");

        if (missingEverywhere.Count == 0)
        {
            report.AppendLine("None");
        }
        else
        {
            foreach (var key in missingEverywhere)
                report.AppendLine($"- {key}");
        }

        report.AppendLine();
    }

    private static void WriteMissingTranslationsByLanguage(StringBuilder report, HashSet<string> allKeys)
    {
        report.AppendLine("=== MISSING TRANSLATIONS BY LANGUAGE ===");

        bool foundAny = false;

        foreach (var lang in LanguagesToCheck)
        {
            var missing = new List<string>();

            foreach (var key in allKeys.OrderBy(x => x))
            {
                if (!Translator.translateMaps.TryGetValue(key, out var translations))
                {
                    missing.Add(key);
                    continue;
                }

                if (!translations.TryGetValue((int)lang, out var value) || string.IsNullOrWhiteSpace(value))
                {
                    missing.Add(key);
                }
            }

            if (missing.Count == 0)
                continue;

            foundAny = true;

            report.AppendLine();
            report.AppendLine($"[{lang}] missing {missing.Count}:");

            foreach (var key in missing)
                report.AppendLine($"- {key}");
        }

        if (!foundAny)
            report.AppendLine("None");

        report.AppendLine();
    }

    private static void WriteSuspiciousFallbackTranslations(StringBuilder report, HashSet<string> allKeys)
    {
        report.AppendLine("=== POSSIBLY UNTRANSLATED / SUSPICIOUS VALUES ===");

        bool foundAny = false;

        foreach (var key in allKeys.OrderBy(x => x))
        {
            string english = GetTranslationValue(key, SupportedLangs.English);

            foreach (var lang in LanguagesToCheck)
            {
                if (lang == SupportedLangs.English)
                    continue;

                string value = GetTranslationValue(key, lang);

                if (string.IsNullOrWhiteSpace(value))
                    continue;

                // Caso classico: italiano/russo uguale all'inglese.
                if (!string.IsNullOrWhiteSpace(english) && value == english)
                {
                    foundAny = true;
                    report.AppendLine($"[{lang}] same as English: {key} = {value}");
                }

                // Caso classico: lingua non cinese con testo cinese dentro.
                if (lang is not SupportedLangs.SChinese and not SupportedLangs.TChinese && ContainsChinese(value))
                {
                    foundAny = true;
                    report.AppendLine($"[{lang}] contains Chinese characters: {key} = {value}");
                }
            }
        }

        if (!foundAny)
            report.AppendLine("None");

        report.AppendLine();
    }

    private static void WritePlaceholderProblems(StringBuilder report, HashSet<string> allKeys)
    {
        report.AppendLine("=== PLACEHOLDER MISMATCHES ===");

        bool foundAny = false;

        foreach (var key in allKeys.OrderBy(x => x))
        {
            string english = GetTranslationValue(key, SupportedLangs.English);

            if (string.IsNullOrWhiteSpace(english))
                continue;

            var englishPlaceholders = ExtractPlaceholders(english);

            foreach (var lang in LanguagesToCheck)
            {
                if (lang == SupportedLangs.English)
                    continue;

                string value = GetTranslationValue(key, lang);

                if (string.IsNullOrWhiteSpace(value))
                    continue;

                var translatedPlaceholders = ExtractPlaceholders(value);

                if (!englishPlaceholders.SetEquals(translatedPlaceholders))
                {
                    foundAny = true;

                    report.AppendLine($"[{lang}] placeholder mismatch: {key}");
                    report.AppendLine($"  English: {english}");
                    report.AppendLine($"  {lang}: {value}");
                    report.AppendLine($"  English placeholders: {string.Join(", ", englishPlaceholders)}");
                    report.AppendLine($"  {lang} placeholders: {string.Join(", ", translatedPlaceholders)}");
                }
            }
        }

        if (!foundAny)
            report.AppendLine("None");

        report.AppendLine();
    }

    private static string GetTranslationValue(string key, SupportedLangs lang)
    {
        if (Translator.translateMaps == null)
            return "";

        if (!Translator.translateMaps.TryGetValue(key, out var translations))
            return "";

        if (!translations.TryGetValue((int)lang, out var value))
            return "";

        return value ?? "";
    }

    private static bool ContainsChinese(string value)
    {
        return Regex.IsMatch(value, @"[\u4e00-\u9fff]");
    }

    private static HashSet<string> ExtractPlaceholders(string value)
    {
        var result = new HashSet<string>();

        foreach (Match match in Regex.Matches(value, @"\{[0-9]+[^}]*\}"))
            result.Add(match.Value);

        return result;
    }

    private static HashSet<string> FindTranslationKeysUsedByMod(Assembly assembly)
    {
        var result = new HashSet<string>();

        foreach (var type in SafeGetTypes(assembly))
        {
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                ScanMethodForTranslationKeys(method, result);
            }

            foreach (var constructor in type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                ScanMethodForTranslationKeys(constructor, result);
            }
        }

        return result;
    }

    private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException e)
        {
            return e.Types.Where(t => t != null);
        }
        catch
        {
            return [];
        }
    }

    private static void ScanMethodForTranslationKeys(MethodBase method, HashSet<string> result)
    {
        try
        {
            var body = method.GetMethodBody();

            if (body == null)
                return;

            var il = body.GetILAsByteArray();

            if (il == null || il.Length == 0)
                return;

            var module = method.Module;
            var stack = new Stack<object>();

            Type[] typeArgs = method.DeclaringType != null && method.DeclaringType.IsGenericType
                ? method.DeclaringType.GetGenericArguments()
                : Type.EmptyTypes;

            Type[] methodArgs = method.IsGenericMethod
                ? method.GetGenericArguments()
                : Type.EmptyTypes;

            int index = 0;

            while (index < il.Length)
            {
                OpCode opCode = ReadOpCode(il, ref index);
                int token = 0;
                string loadedString = null;

                switch (opCode.OperandType)
                {
                    case OperandType.InlineNone:
                        break;

                    case OperandType.ShortInlineI:
                    case OperandType.ShortInlineVar:
                    case OperandType.ShortInlineBrTarget:
                        index += 1;
                        break;

                    case OperandType.InlineVar:
                        index += 2;
                        break;

                    case OperandType.InlineI:
                    case OperandType.ShortInlineR:
                    case OperandType.InlineBrTarget:
                    case OperandType.InlineField:
                    case OperandType.InlineSig:
                    case OperandType.InlineTok:
                    case OperandType.InlineType:
                        token = BitConverter.ToInt32(il, index);
                        index += 4;
                        break;

                    case OperandType.InlineMethod:
                        token = BitConverter.ToInt32(il, index);
                        index += 4;
                        break;

                    case OperandType.InlineString:
                        token = BitConverter.ToInt32(il, index);
                        index += 4;

                        try
                        {
                            loadedString = module.ResolveString(token);
                        }
                        catch
                        {
                            loadedString = null;
                        }

                        break;

                    case OperandType.InlineI8:
                    case OperandType.InlineR:
                        index += 8;
                        break;

                    case OperandType.InlineSwitch:
                        int count = BitConverter.ToInt32(il, index);
                        index += 4 + count * 4;
                        break;
                }

                if (opCode == OpCodes.Ldstr)
                {
                    stack.Push(loadedString ?? "");
                    continue;
                }

                if (opCode == OpCodes.Call || opCode == OpCodes.Callvirt)
                {
                    MethodBase calledMethod = null;

                    try
                    {
                        calledMethod = module.ResolveMethod(token, typeArgs, methodArgs);
                    }
                    catch
                    {
                    }

                    int parameterCount = 0;

                    if (calledMethod != null)
                    {
                        parameterCount = calledMethod.GetParameters().Length;

                        if (!calledMethod.IsStatic)
                            parameterCount += 1;
                    }

                    object[] args = PopArgs(stack, parameterCount);

                    if (calledMethod != null && IsTranslatorCall(calledMethod))
                    {
                        if (args.Length > 0 && args[0] is string key && !string.IsNullOrWhiteSpace(key))
                            result.Add(key);
                    }

                    if (calledMethod is MethodInfo calledInfo && calledInfo.ReturnType != typeof(void))
                        stack.Push(Unknown);

                    continue;
                }

                if (opCode == OpCodes.Newobj)
                {
                    ConstructorInfo constructor = null;

                    try
                    {
                        constructor = module.ResolveMethod(token, typeArgs, methodArgs) as ConstructorInfo;
                    }
                    catch
                    {
                    }

                    int parameterCount = constructor?.GetParameters().Length ?? 0;
                    PopMany(stack, parameterCount);
                    stack.Push(Unknown);
                    continue;
                }

                ApplyBasicStackEffect(opCode, stack);
            }
        }
        catch
        {
            // Non bloccare il gioco solo perché un metodo non è leggibile.
        }
    }

    private static bool IsTranslatorCall(MethodBase method)
    {
        if (method.DeclaringType != typeof(Translator))
            return false;

        return method.Name == nameof(Translator.GetString)
            || method.Name == nameof(Translator.GetAuto);
    }

    private static OpCode ReadOpCode(byte[] il, ref int index)
    {
        byte first = il[index++];

        if (first != 0xFE)
            return OneByteOpCodes[first];

        byte second = il[index++];
        return TwoByteOpCodes[second];
    }

    private static object[] PopArgs(Stack<object> stack, int count)
    {
        var args = new object[count];

        for (int i = count - 1; i >= 0; i--)
            args[i] = PopOne(stack);

        return args;
    }

    private static void PopMany(Stack<object> stack, int count)
    {
        for (int i = 0; i < count; i++)
            PopOne(stack);
    }

    private static object PopOne(Stack<object> stack)
    {
        return stack.Count > 0 ? stack.Pop() : Unknown;
    }

    private static void ApplyBasicStackEffect(OpCode opCode, Stack<object> stack)
    {
        switch (opCode.StackBehaviourPop)
        {
            case StackBehaviour.Pop0:
                break;

            case StackBehaviour.Pop1:
            case StackBehaviour.Popi:
            case StackBehaviour.Popref:
                PopMany(stack, 1);
                break;

            case StackBehaviour.Pop1_pop1:
            case StackBehaviour.Popi_pop1:
            case StackBehaviour.Popi_popi:
            case StackBehaviour.Popi_popi8:
            case StackBehaviour.Popi_popr4:
            case StackBehaviour.Popi_popr8:
            case StackBehaviour.Popref_pop1:
            case StackBehaviour.Popref_popi:
                PopMany(stack, 2);
                break;

            case StackBehaviour.Popi_popi_popi:
            case StackBehaviour.Popref_popi_pop1:
            case StackBehaviour.Popref_popi_popi:
            case StackBehaviour.Popref_popi_popi8:
            case StackBehaviour.Popref_popi_popr4:
            case StackBehaviour.Popref_popi_popr8:
            case StackBehaviour.Popref_popi_popref:
                PopMany(stack, 3);
                break;

            case StackBehaviour.Varpop:
                break;
        }

        switch (opCode.StackBehaviourPush)
        {
            case StackBehaviour.Push0:
                break;

            case StackBehaviour.Push1:
            case StackBehaviour.Pushi:
            case StackBehaviour.Pushi8:
            case StackBehaviour.Pushr4:
            case StackBehaviour.Pushr8:
            case StackBehaviour.Pushref:
                stack.Push(Unknown);
                break;

            case StackBehaviour.Push1_push1:
                stack.Push(Unknown);
                stack.Push(Unknown);
                break;

            case StackBehaviour.Varpush:
                break;
        }
    }
}