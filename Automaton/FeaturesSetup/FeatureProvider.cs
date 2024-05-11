using ECommons.DalamudServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Automaton.FeaturesSetup;

public class FeatureProvider(Assembly assembly) : IDisposable
{
    public bool Disposed { get; protected set; } = false;

    public List<BaseFeature> Features { get; } = [];

    public Assembly Assembly { get; init; } = assembly;

    public virtual void LoadFeatures()
    {
        foreach (var t in Assembly.GetTypes().Where(x => x.IsSubclassOf(typeof(Feature)) && !x.IsAbstract))
            try
            {
                var feature = (Feature)Activator.CreateInstance(t);
                feature.InterfaceSetup(P, Svc.PluginInterface, C, this);
                feature.Setup();
                if (feature.Ready && C.EnabledTweaks.Contains(t.Name) || feature.FeatureType == FeatureType.Commands)
                    if (feature.FeatureType == FeatureType.Disabled || feature.IsDebug && !C.ShowDebug)
                        feature.Disable();
                    else
                        feature.Enable();

                Features.Add(feature);
            }
            catch (Exception ex)
            {
                Svc.Log.Error(ex, $"Feature not loaded: {t.Name}");
            }
    }

    public void UnloadFeatures()
    {
        foreach (var t in Features)
            if (t.Enabled || t.FeatureType == FeatureType.Commands)
                try
                {
                    t.Disable();
                }
                catch (Exception ex)
                {
                    Svc.Log.Error(ex, $"Cannot disable {t.Name}");
                }
        Features.Clear();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        UnloadFeatures();
        Disposed = true;
    }
}
