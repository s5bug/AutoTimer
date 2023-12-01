using System;
using System.Collections.Generic;
using System.Diagnostics;
using AutoTimer.Game;
using Dalamud.Game.ClientState.Statuses;
using Dalamud.Logging.Internal;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using Action = Lumina.Excel.GeneratedSheets.Action;

namespace AutoTimer;

public class AutoTimerHooksListener : HooksListener {
    public const uint AutoAttackActionCategoryId = 1;
    public const uint TenChiJinStatusId = 1186;
    private Stopwatch AutoStopwatch { get; init; } = new();
    private IClientState ClientState { get; init; }
    private IDataManager DataManager { get; init; }

    private HashSet<uint> AutoAttackActionIds { get; init; }

    public AutoTimerHooksListener(IClientState clientState, IDataManager dataManager) {
        this.ClientState = clientState;
        this.DataManager = dataManager;

        this.AutoAttackActionIds = new HashSet<uint>();
        foreach (var action in this.DataManager.GetExcelSheet<Action>()) {
            if (action.ActionCategory.Row == AutoAttackActionCategoryId) {
                this.AutoAttackActionIds.Add(action.RowId);
            }
        }
    }

    public void HandleActionEffect1(uint actorId, ActionEffect1 actionEffect1) {
        if (actorId == this.ClientState.LocalPlayer?.ObjectId) {
            // AutoAttack Action from us
            if (actionEffect1.Header.EffectDisplayType == AbilityDisplayType.ShowActionName &&
                this.AutoAttackActionIds.Contains(actionEffect1.Header.ActionId)) {
                // AutoAttack from us
                this.AutoStopwatch.Restart();
                // Which means we should also no longer be ticking Tcj
                this.TcjStart = null;
            }
        }
    }

    public TimeSpan TimeSinceLastAuto() {
        return this.AutoStopwatch.Elapsed;
    }
    
    public TimeSpan? TcjStart { get; set; }

    public bool HasTcjBuff() {
        if (this.ClientState.LocalPlayer is { } localPlayer) {
            foreach(var status in localPlayer.StatusList) {
                if (status.StatusId == TenChiJinStatusId) {
                    return true;
                }
            }
        }
        return false;
    }

    public void FrameworkUpdate(IFramework framework) {
        if (this.ClientState.LocalPlayer is { } localPlayer) {
            if (localPlayer.IsCasting && localPlayer.CurrentCastTime < (localPlayer.TotalCastTime - 0.5)) {
                this.AutoStopwatch.Stop();
            }
            else {
                this.AutoStopwatch.Start();
            }
        }
    }
    
}
