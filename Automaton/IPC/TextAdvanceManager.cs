using ECommons.DalamudServices;
using System.Collections.Generic;

namespace Automaton.IPC;

internal static class TextAdvanceManager
{
    private static bool WasChanged = false;

    private static bool IsBusy => P.TaskManager.IsBusy;
    internal static void Tick()
    {
        if (WasChanged)
        {
            if (!IsBusy)
            {
                WasChanged = false;
                UnlockTA();
            }
        }
        if (IsBusy)
        {
            WasChanged = true;
            LockTA();
        }
    }
    internal static void LockTA()
    {
        if (Svc.PluginInterface.TryGetData<HashSet<string>>("TextAdvance.StopRequests", out var data))
        {
            data.Add(Name);
        }
    }

    internal static void UnlockTA()
    {
        if (Svc.PluginInterface.TryGetData<HashSet<string>>("TextAdvance.StopRequests", out var data))
        {
            data.Remove(Name);
        }
    }
}
