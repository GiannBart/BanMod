//credits and licenses in the resources folder
using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using LogLevel = BepInEx.Logging.LogLevel;

namespace BanMod
{
    public static class BMLogger
    {
        private static ManualLogSource _pluginLogger;
        private static bool _enabled = true;

        private static readonly List<string> DisableList = new();
        private static readonly List<string> SendToGameList = new();
        private static readonly HashSet<string> NowDetailedErrorLog = new();

        public static void Init(ManualLogSource pluginLogger)
        {
            _pluginLogger = pluginLogger;
            LogInfo("Logger initialized", "Logger");
        }

        public static void Enable() => _enabled = true;
        public static void Disable() => _enabled = false;

        public static void Enable(string tag, bool toGame = false)
        {
            DisableList.Remove(tag);

            if (toGame)
            {
                if (!SendToGameList.Contains(tag))
                    SendToGameList.Add(tag);
            }
            else
            {
                SendToGameList.Remove(tag);
            }
        }

        public static void Disable(string tag)
        {
            if (!DisableList.Contains(tag))
                DisableList.Add(tag);
        }

        public static void SendInGame(string text)
        {
            if (!_enabled) return;

            try
            {
                if (DestroyableSingleton<HudManager>._instance)
                {
                    DestroyableSingleton<HudManager>.Instance.Notifier.AddDisconnectMessage(text);
                }
            }
            catch
            {
            }

            Warn(text, "SendInGame");
        }

        private static void WritePluginLog(LogLevel level, string line)
        {
            if (_pluginLogger == null) return;

            switch (level)
            {
                case LogLevel.Info:
                    _pluginLogger.LogInfo(line);
                    break;
                case LogLevel.Warning:
                    _pluginLogger.LogWarning(line);
                    break;
                case LogLevel.Error:
                    _pluginLogger.LogError(line);
                    break;
                case LogLevel.Fatal:
                    _pluginLogger.LogFatal(line);
                    break;
                case LogLevel.Message:
                    _pluginLogger.LogMessage(line);
                    break;
                case LogLevel.Debug:
                    _pluginLogger.LogDebug(line);
                    break;
                default:
                    _pluginLogger.LogInfo(line);
                    break;
            }
        }

        private static void SendToLogger(
            string text,
            LogLevel level = LogLevel.Info,
            string tag = "BanMod",
            bool escapeCRLF = true,
            int lineNumber = 0,
            string fileName = "",
            bool multiLine = false)
        {
            if (!_enabled) return;
            if (DisableList.Contains(tag)) return;

            text ??= "null";

            if (SendToGameList.Contains(tag))
            {
                try
                {
                    if (DestroyableSingleton<HudManager>._instance)
                    {
                        DestroyableSingleton<HudManager>.Instance.Notifier.AddDisconnectMessage($"[{tag}] {text}");
                    }
                }
                catch
                {
                }
            }

            string timeText = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string finalText;

            if ((level == LogLevel.Error || level == LogLevel.Fatal) &&
                !multiLine &&
                !NowDetailedErrorLog.Contains(tag))
            {
                StackFrame stack = new(2);
                string className = stack.GetMethod()?.ReflectedType?.Name ?? "UnknownClass";
                string memberName = stack.GetMethod()?.Name ?? "UnknownMethod";

                finalText =
                    $"[{timeText}][{level}][{className}.{memberName}({Path.GetFileName(fileName)}:{lineNumber})][{tag}] {text}";

                NowDetailedErrorLog.Add(tag);
            }
            else
            {
                if (escapeCRLF && !multiLine)
                    text = text.Replace("\r", "\\r").Replace("\n", "\\n");

                finalText = $"[{timeText}][{level}][{tag}] {text}";
            }

            if (!multiLine)
            {
                WritePluginLog(level, finalText);
                return;
            }

            string[] lines = finalText.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
            foreach (string line in lines)
            {
                WritePluginLog(level, line);
            }
        }

        public static void Log(
            LogLevel level,
            object message,
            string tag = "BanMod",
            bool escapeCRLF = true,
            [CallerLineNumber] int lineNumber = 0,
            [CallerFilePath] string fileName = "",
            bool multiLine = false)
        {
            SendToLogger(message?.ToString() ?? "null", level, tag, escapeCRLF, lineNumber, fileName, multiLine);
        }

        public static void Test(
            object content,
            string tag = "======= Test =======",
            bool escapeCRLF = true,
            [CallerLineNumber] int lineNumber = 0,
            [CallerFilePath] string fileName = "",
            bool multiLine = false)
        {
            SendToLogger(content?.ToString() ?? "null", LogLevel.Debug, tag, escapeCRLF, lineNumber, fileName, multiLine);
        }

        public static void Info(
            string text,
            string tag = "BanMod",
            bool escapeCRLF = true,
            [CallerLineNumber] int lineNumber = 0,
            [CallerFilePath] string fileName = "",
            bool multiLine = false)
        {
            SendToLogger(text, LogLevel.Info, tag, escapeCRLF, lineNumber, fileName, multiLine);
        }

        public static void Warn(
            string text,
            string tag = "BanMod",
            bool escapeCRLF = true,
            [CallerLineNumber] int lineNumber = 0,
            [CallerFilePath] string fileName = "",
            bool multiLine = false)
        {
            SendToLogger(text, LogLevel.Warning, tag, escapeCRLF, lineNumber, fileName, multiLine);
        }

        public static void Error(
            string text,
            string tag = "BanMod",
            bool escapeCRLF = true,
            [CallerLineNumber] int lineNumber = 0,
            [CallerFilePath] string fileName = "",
            bool multiLine = false)
        {
            SendToLogger(text, LogLevel.Error, tag, escapeCRLF, lineNumber, fileName, multiLine);
        }

        public static void Fatal(
            string text,
            string tag = "BanMod",
            bool escapeCRLF = true,
            [CallerLineNumber] int lineNumber = 0,
            [CallerFilePath] string fileName = "",
            bool multiLine = false)
        {
            SendToLogger(text, LogLevel.Fatal, tag, escapeCRLF, lineNumber, fileName, multiLine);
        }

        public static void Msg(
            string text,
            string tag = "BanMod",
            bool escapeCRLF = true,
            [CallerLineNumber] int lineNumber = 0,
            [CallerFilePath] string fileName = "",
            bool multiLine = false)
        {
            SendToLogger(text, LogLevel.Message, tag, escapeCRLF, lineNumber, fileName, multiLine);
        }

        public static void Exception(
            Exception ex,
            string tag = "Exception",
            [CallerLineNumber] int lineNumber = 0,
            [CallerFilePath] string fileName = "",
            bool multiLine = true)
        {
            SendToLogger(ex?.ToString() ?? "null", LogLevel.Error, tag, false, lineNumber, fileName, multiLine);
        }

        public static void CurrentMethod(
            [CallerLineNumber] int lineNumber = 0,
            [CallerFilePath] string fileName = "")
        {
            StackFrame stack = new(1);
            Msg(
                $"\"{stack.GetMethod()?.ReflectedType?.Name}.{stack.GetMethod()?.Name}\" Called in \"{Path.GetFileName(fileName)}({lineNumber})\"",
                "Method");
        }

        public static void LogInfo(
            object message,
            string tag = "BanMod",
            [CallerLineNumber] int lineNumber = 0,
            [CallerFilePath] string fileName = "")
        {
            SendToLogger(message?.ToString() ?? "null", LogLevel.Info, tag, true, lineNumber, fileName, false);
        }

        public static void LogWarning(
            object message,
            string tag = "BanMod",
            [CallerLineNumber] int lineNumber = 0,
            [CallerFilePath] string fileName = "")
        {
            SendToLogger(message?.ToString() ?? "null", LogLevel.Warning, tag, true, lineNumber, fileName, false);
        }

        public static void LogError(
            object message,
            string tag = "BanMod",
            [CallerLineNumber] int lineNumber = 0,
            [CallerFilePath] string fileName = "")
        {
            SendToLogger(message?.ToString() ?? "null", LogLevel.Error, tag, true, lineNumber, fileName, false);
        }

        public static void LogFatal(
            object message,
            string tag = "BanMod",
            [CallerLineNumber] int lineNumber = 0,
            [CallerFilePath] string fileName = "")
        {
            SendToLogger(message?.ToString() ?? "null", LogLevel.Fatal, tag, true, lineNumber, fileName, false);
        }

        public static void LogMessage(
            object message,
            string tag = "BanMod",
            [CallerLineNumber] int lineNumber = 0,
            [CallerFilePath] string fileName = "")
        {
            SendToLogger(message?.ToString() ?? "null", LogLevel.Message, tag, true, lineNumber, fileName, false);
        }

        public static void LogDebug(
            object message,
            string tag = "BanMod",
            [CallerLineNumber] int lineNumber = 0,
            [CallerFilePath] string fileName = "")
        {
            SendToLogger(message?.ToString() ?? "null", LogLevel.Debug, tag, true, lineNumber, fileName, false);
        }
    }
}