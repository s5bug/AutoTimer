using System;
using System.Numerics;
using Dalamud.Interface.Components;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace AutoTimer.Windows;

public class ConfigWindow : Window, IDisposable {
    private Configuration Configuration;

    public ConfigWindow(AutoTimerPlugin plugin) : base(
        "AutoTimer Configuration",
        ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
        ImGuiWindowFlags.NoScrollWithMouse) {
        this.Size = new Vector2(232, 150);
        this.SizeCondition = ImGuiCond.Always;

        this.Configuration = plugin.Configuration;
    }

    public void Dispose() { }

    public override void Draw() {
        if (ImGui.BeginCombo("Bar Type", Configuration.BarTypeName(this.Configuration.BarTypeChoice))) {
            foreach (var barType in Enum.GetValues<Configuration.BarType>()) {
                var isSelected = this.Configuration.BarTypeChoice == barType;
                if (ImGui.Selectable(Configuration.BarTypeName(barType), isSelected)) {
                    this.Configuration.BarTypeChoice = barType;
                    // can save immediately on change, if you don't want to provide a "Save and Close" button
                    this.Configuration.Save();
                }
                if (isSelected) {
                    ImGui.SetItemDefaultFocus();
                }
            }
            ImGui.EndCombo();
        }

        var configValue = this.Configuration.PredictiveTcj;
        if (ImGui.Checkbox("Predictive TCJ", ref configValue)) {
            this.Configuration.PredictiveTcj = configValue;
            this.Configuration.Save();
        }
        ImGuiComponents.HelpMarker(
            "Assumes that the user will completely execute Ten-Chi-Jin without dropping GCD, which will last " +
            "2.85 seconds. Without this option, the Ten-Chi-Jin auto-attack bar will cycle, displaying each possible " +
            "auto-attack. With this option, the bar instead will delay the first cycle to 3.25, 4.25, or 5.25 seconds, " +
            "depending on when Ten-Chi-Jin was entered. Enable the \"Bar Type\" option for Ninja to see optimal " +
            "Ten-Chi-Jin entrance timings assuming 2.12 Huton."
            );

        configValue = this.Configuration.LockBar;
        if (ImGui.Checkbox("Lock Bar", ref configValue)) {
            this.Configuration.LockBar = configValue;
            this.Configuration.Save();
        }
    }
}
