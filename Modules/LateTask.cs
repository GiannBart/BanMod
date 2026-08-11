//credits and licenses in the resources folder
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace BanMod;

class LateTask
{
    public static readonly List<LateTask> Tasks = [];
    private readonly Action action;
    private readonly string callerData;
    private readonly bool log;
    private readonly string name;
    private float timer;

    private LateTask(Action action, float time, string name, bool log, string callerData)
    {
        this.action = action;
        timer = time;
        this.name = name;
        this.log = log;
        this.callerData = callerData;
        Tasks.Add(this);
        if (log && name is not "" and not "No Name Task")
            BMLogger.Info("\"" + name + "\" is created", "LateTask");
    }
    public static void Clear()
    {
        try
        {
            Tasks.Clear();
        }
        catch { }
    }
    private bool Run(float deltaTime)
    {
        timer -= deltaTime;
        if (timer <= 0)
        {
            action();
            return true;
        }

        return false;
    }

    public static void New(Action action, float time, string name = "No Name Task", bool log = true, [CallerFilePath] string path = "", [CallerMemberName] string member = "", [CallerLineNumber] int line = 0, bool repeatUntilTrue = false)
        => _ = new LateTask(action, time, name, log, $"created at {path.Split('\\')[^1]}, by member {member}, at line {line}");

    public static void Update(float deltaTime)
    {
        foreach (var task in Tasks.ToArray())
        {
            try
            {
                if (task.Run(deltaTime))
                {
                    if (task.name is not "" and not "No Name Task" && task.log)
                        BMLogger.Info($"\"{task.name}\" is finished", "LateTask");
                    Tasks.Remove(task);
                }
            }
            catch (Exception ex)
            {
                BMLogger.Error($"{ex.GetType()}: {ex.Message}  in \"{task.name}\" ({task.callerData})\n{ex.StackTrace}", "LateTask.Error", false);
                Tasks.Remove(task);
            }
        }
    }
}