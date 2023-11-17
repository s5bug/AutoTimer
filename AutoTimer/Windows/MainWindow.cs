using System;
using System.Numerics;
using AutoTimer.Game;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Client.Game;
using ImGuiNET;

namespace AutoTimer.Windows;

public class MainWindow : Window, IDisposable {
    private AutoTimerPlugin Plugin;

    private IDalamudTextureWrap GaugeImage;
    private IDalamudTextureWrap GaugeMonkImage;
    private IDalamudTextureWrap ProgressImage;

    public MainWindow(
        AutoTimerPlugin plugin,
        IDalamudTextureWrap gauge,
        IDalamudTextureWrap gaugeMonk,
        IDalamudTextureWrap progress
    ) : base(
        "AutoTimer", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoBackground) {
        this.SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(160, 35),
            MaximumSize = new Vector2(160, 35)
        };

        this.Plugin = plugin;

        this.GaugeImage = gauge;
        this.GaugeMonkImage = gaugeMonk;
        this.ProgressImage = progress;
    }

    public void Dispose() {
        
    }

    public override void Update() {
        if (this.Plugin.Configuration.LockBar) {
            this.Flags |= ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoBackground;
        }
        else {
            this.Flags &= ~(ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoBackground);
        }
    }

    public override void Draw() {
        var tsla = this.Plugin.HooksListener.TimeSinceLastAuto();
        
        // ImGui.Text($"Time since last auto: {tsla}");
        // ImGui.Text($"Lv1 Action: {this.Plugin.AutoCalculator.GetLv1Action()}");
        // ImGui.Text($"Skillspeed: {this.Plugin.AutoCalculator.GetAttribute(45)}");
        // ImGui.Text($"Spellspeed: {this.Plugin.AutoCalculator.GetAttribute(46)}");
        // ImGui.Text($"BASE: {this.Plugin.AutoCalculator.GetBaseGcd()}");
        // ImGui.Text($"CURR: {this.Plugin.AutoCalculator.GetCurrentGcd()}");
        // ImGui.Text($"Base Weapon Delay: {this.Plugin.AutoCalculator.GetWeaponDelay()}");
        // ImGui.Text($"Total Delay: {this.Plugin.AutoCalculator.GetAutoAttackDelay()}");

        var td = this.Plugin.AutoCalculator.GetAutoAttackDelay();
        if (td is { } autoAttackDelay) {
            double progress = Math.Min(1.0, tsla.Divide(autoAttackDelay));
            
            ImGui.SetCursorPos(new Vector2(2, 2));
            if (this.Plugin.Configuration.UseMonkGauge) {
                ImGui.Image(this.GaugeMonkImage.ImGuiHandle,
                            new Vector2(this.GaugeMonkImage.Width, this.GaugeMonkImage.Height));
            }
            else {
                ImGui.Image(this.GaugeImage.ImGuiHandle,
                            new Vector2(this.GaugeImage.Width, this.GaugeImage.Height));
            }

            ImGui.SetCursorPos(new Vector2(2, 2));
            ImGui.Image(this.ProgressImage.ImGuiHandle, new Vector2(this.ProgressImage.Width * (float) progress, this.ProgressImage.Height), new Vector2(0, 0), new Vector2((float) progress, 1));
        }
        

        // if (ImGui.Button("Show Settings")) {
        //     this.Plugin.DrawConfigUI();
        // }
    }
}
