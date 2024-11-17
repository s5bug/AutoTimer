using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

namespace AutoTimer;

[Serializable]
public class Configuration : IPluginConfiguration {
    public int Version { get; set; } = 0;

    public enum BarType {
        AlwaysPlain,
        AlwaysMonk,
        AlwaysNinja,
        JobDependent
    }

    public static string BarTypeName(BarType barType) {
        return barType switch {
            BarType.AlwaysPlain => "Always Plain",
            BarType.AlwaysMonk => "Always Monk",
            BarType.AlwaysNinja => "Always Ninja",
            BarType.JobDependent => "Job Dependent"
        };
    }

    public BarType BarTypeChoice { get; set; } = BarType.AlwaysPlain;
    public bool PredictiveTcj { get; set; } = true;
    public bool LockBar { get; set; } = true;
    public bool BarOpen { get; set; } = false;
    public bool BarLabel { get; set; } = true;
    public double Scale { get; set; } = 1.0;
    
    public bool HideOutOfCombat { get; set; } = false;
    public bool HideInCutscene { get; set; } = false;
    public bool HideWhileOccupied { get; set; } = false;

    // the below exist just to make saving less cumbersome
    [NonSerialized]
    private IDalamudPluginInterface? PluginInterface;

    public void Initialize(IDalamudPluginInterface pluginInterface) {
        this.PluginInterface = pluginInterface;
    }

    public void Save() {
        this.PluginInterface!.SavePluginConfig(this);
    }
}
