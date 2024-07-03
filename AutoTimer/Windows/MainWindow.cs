using System;
using System.Numerics;
using AutoTimer.Game;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Windowing;
using Dalamud.Logging.Internal;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using ImGuiNET;

namespace AutoTimer.Windows;

public class MainWindow : Window, IDisposable {
    public const uint MonkClassJobId = 20;
    public const uint NinjaClassJobId = 30;
    
    private AutoTimerPlugin plugin;
    private IClientState clientState;

    private IDalamudTextureWrap GaugeImage;
    private IDalamudTextureWrap GaugeMonkImage;
    private IDalamudTextureWrap GaugeNinjaImage;
    private IDalamudTextureWrap ProgressImage;
    private IDalamudTextureWrap TcjProgressImage;

    public MainWindow(
        AutoTimerPlugin plugin,
        IClientState clientState,
        IDalamudTextureWrap gauge,
        IDalamudTextureWrap gaugeMonk,
        IDalamudTextureWrap gaugeNinja,
        IDalamudTextureWrap progress,
        IDalamudTextureWrap tcjProgress
    ) : base(
        "AutoTimer", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoInputs |
                     ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.AlwaysAutoResize) {
        this.plugin = plugin;
        this.clientState = clientState;

        this.GaugeImage = gauge;
        this.GaugeMonkImage = gaugeMonk;
        this.GaugeNinjaImage = gaugeNinja;
        this.ProgressImage = progress;
        this.TcjProgressImage = tcjProgress;
    }

    private IDalamudTextureWrap JobDependentGaugeImage() {
        if (this.clientState.LocalPlayer is { } localPlayer) {
            return localPlayer.ClassJob.Id switch {
                MonkClassJobId => this.GaugeMonkImage,
                NinjaClassJobId => this.GaugeNinjaImage,
                _ => this.GaugeImage
            };
        }
        return this.GaugeImage;
    }

    public void Dispose() {
        
    }

    public override void Update() {
        if (this.plugin.Configuration.LockBar) {
            this.Flags |= ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoBackground;
        }
        else {
            this.Flags &= ~(ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoBackground);
        }
    }

    private TimeSpan? tcjEndPrediction = null;
    private bool tcjTicked = false;
    
    public override void Draw() {
        var tsla = this.plugin.HooksListener.TimeSinceLastAuto();
        
        // ImGui.Text($"Time since last auto: {tsla}");
        // ImGui.Text($"Lv1 Action: {this.Plugin.AutoCalculator.GetLv1Action()}");
        // ImGui.Text($"Skillspeed: {this.Plugin.AutoCalculator.GetAttribute(45)}");
        // ImGui.Text($"Spellspeed: {this.Plugin.AutoCalculator.GetAttribute(46)}");
        // ImGui.Text($"BASE: {this.Plugin.AutoCalculator.GetBaseGcd()}");
        // ImGui.Text($"CURR: {this.Plugin.AutoCalculator.GetCurrentGcd()}");
        // ImGui.Text($"Base Weapon Delay: {this.Plugin.AutoCalculator.GetWeaponDelay()}");
        // ImGui.Text($"Total Delay: {this.Plugin.AutoCalculator.GetAutoAttackDelay()}");

        var td = this.plugin.AutoCalculator.GetAutoAttackDelay();
        if (td is { } autoAttackDelay) {
            double progress = Math.Min(1.0, tsla.Divide(autoAttackDelay));
            
            if (this.plugin.HooksListener.HasTcjBuff() && this.plugin.HooksListener.TcjStart is null) {
                this.plugin.HooksListener.TcjStart = tsla;
            }

            Vector2 backgroundPos = ImGui.GetCursorPos();
            var gaugeImage = this.plugin.Configuration.BarTypeChoice switch {
                Configuration.BarType.AlwaysPlain => this.GaugeImage,
                Configuration.BarType.AlwaysMonk => this.GaugeMonkImage,
                Configuration.BarType.AlwaysNinja => this.GaugeNinjaImage,
                Configuration.BarType.JobDependent => JobDependentGaugeImage()
            };
            ImGui.Image(gaugeImage.ImGuiHandle,
                        new Vector2(gaugeImage.Width, gaugeImage.Height));


            var hasTcjBuff = this.plugin.HooksListener.HasTcjBuff();
            // If we're past our TCJ ending prediction and no longer have TCJ, reset tcjStart
            var pastEp = this.tcjEndPrediction is { } tcjep && tsla > tcjep && !hasTcjBuff;
            // If we haven't ticked TCJ yet and no longer have TCJ, reset tcjStart
            var earlyDisengage = !this.tcjTicked && !hasTcjBuff;
            if (pastEp || earlyDisengage) {
                this.plugin.HooksListener.TcjStart = null;
            }
            
            if (this.plugin.HooksListener.TcjStart is { } tcjStart) {
                if (tsla > autoAttackDelay) {
                    this.tcjTicked = true;
                }
                
                // Possible times for the next auto are 0.25, 1.25, 2.25, etc
                // Without Predictive TCJ, find the next auto after now (tsla)
                //   nextAuto = floorSeconds(max(tsla, autoDelay) + 0.75s) + 0.25s
                //   As soon as TCJ is entered:
                //     The TCJ bar should be empty until (tsla > autoDelay)
                //     Progress should be progress through 1s tick (progress between N.25 and (N+1).25) 
                // Predictive TCJ restricts the next autos to the ones that happen 2.85s after TCJ is entered
                //   Calculate tcjEnd as tcjStart + 2.85s
                //   Calculate tcjEndAuto as floorSeconds(tcjEnd + 0.75s) + 0.25s
                //   tickLength = tsla > tcjEndAuto ? 1.0 : tcjEndAuto - tcjStart
                //   nextAuto = tsla > tcjEndAuto ? predictiveTcj : tcjEndAuto
                //   As soon as TCJ is entered:
                //     The TCJ bar should start filling up immediately
                //     Progress should be progress until nextAuto

                TimeSpan nextAuto;
                double tickLength;

                TimeSpan tcjEnd = tcjStart + TimeSpan.FromSeconds(2.85);
                TimeSpan tcjEndAuto = TimeSpan.FromSeconds((tcjEnd + TimeSpan.FromSeconds(0.75)).Seconds) +
                                      TimeSpan.FromSeconds(0.25);
                if (this.plugin.Configuration.PredictiveTcj && tsla < tcjEndAuto) {
                    tickLength = (tcjEndAuto - tcjStart).TotalSeconds;
                    nextAuto = tcjEndAuto;
                }
                else {
                    var minDelay = tsla > autoAttackDelay ? tsla : autoAttackDelay;
                    tickLength = tsla > autoAttackDelay ? 1.0 : autoAttackDelay.TotalSeconds;
                    nextAuto = TimeSpan.FromSeconds((minDelay + TimeSpan.FromSeconds(0.75)).Seconds) +
                               TimeSpan.FromSeconds(0.25);
                }
                this.tcjEndPrediction = nextAuto;
                
                double tcjTickProgress = Math.Min(1.0, 1.0 - Math.Min(1.0, (nextAuto - tsla).TotalSeconds / tickLength));
                
                ImGui.SetCursorPos(backgroundPos);
                ImGui.Image(
                    this.TcjProgressImage.ImGuiHandle,
                    new Vector2(this.TcjProgressImage.Width * (float) tcjTickProgress, this.TcjProgressImage.Height),
                    new Vector2(0, 0), new Vector2((float) tcjTickProgress, 1));
            }
            else {
                ImGui.SetCursorPos(backgroundPos);
                ImGui.Image(
                    this.ProgressImage.ImGuiHandle, 
                    new Vector2(this.ProgressImage.Width * (float) progress, this.ProgressImage.Height), 
                    new Vector2(0, 0), new Vector2((float) progress, 1));

                // If we've auto-attacked we're no longer in TCJ
                this.tcjEndPrediction = null;
                this.tcjTicked = false;
            }
        }
        

        // if (ImGui.Button("Show Settings")) {
        //     this.Plugin.DrawConfigUI();
        // }
    }
}
