using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Logging.Internal;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.GeneratedSheets;
using Action = Lumina.Excel.GeneratedSheets.Action;

namespace AutoTimer.Game;

public class AutoCalculator {
    public static readonly int SkillspeedAttributeId = 45;
    public static readonly int SpellspeedAttributeId = 46;

    public static readonly uint RiddleOfWindId = 2687;
    
    private IClientState ClientState { get; init; }
    private IDataManager DataManager { get; init; }

    private Dictionary<uint, uint> Lv1ActionCache { get; init; }

    public AutoCalculator(IClientState clientState, IDataManager dataManager) {
        this.ClientState = clientState;
        this.DataManager = dataManager;

        this.Lv1ActionCache = new();
        var log = new ModuleLog("AutoCalculator");
        // TODO look into ClassJobActionSort
        foreach (Action a in this.DataManager.GetExcelSheet<Action>()) {
            if (a.ClassJobLevel == 1) {
                var actionId = a.RowId;
                var classId = a.ClassJob.Row;
                this.Lv1ActionCache[classId] = actionId;
                log.Information($"Lv1ActionCache[{classId}] = {actionId}");
            }
        }
    }

    // Thx GearsetHelperPlugin
    private static double CalculateBaseGcd(int coefficient, int extra, int levelModifier, int modifier = 0, int haste = 0) {
        return Math.Floor(
                   Math.Floor((1000.0 - Math.Floor(coefficient * (double) extra / levelModifier)) * 2500.0 / 1000.0) *
                   Math.Floor(
                       (
                           Math.Floor(
                               Math.Floor(100.0 - modifier) *
                               (100.0 - haste) / 100.0
                           )
                       ) * -1
                   ) / -100.0
               ) / 1000.0;
    }

    public int GetAttribute(int attribute) {
        unsafe {
            return PlayerState.Instance()->Attributes[attribute];
        }
    }

    public float? GetBaseGcd() {
        uint? a = this.GetLv1Action();
        if (a is { } lv1Action) {
            var actionData = this.DataManager.GetExcelSheet<Action>().GetRow(lv1Action);

            byte level = this.ClientState.LocalPlayer.Level;

            int speedStat = actionData.ActionCategory.Value.Name.ToString() switch {
                // See CharacterPanelRefined Attributes
                "Weaponskill" => GetAttribute(SkillspeedAttributeId),
                "Spell" => GetAttribute(SpellspeedAttributeId)
            };
            int baseSpeed = this.DataManager.GetExcelSheet<ParamGrow>().GetRow(level).BaseSpeed;
            int extraSpeed = speedStat - baseSpeed;

            // See GearSetHelper
            int speedCoefficient = 130;

            var growth =
                this.DataManager.GetExcelSheet<ParamGrow>().GetRow(level);
            float fgcd = (float) CalculateBaseGcd(speedCoefficient, extraSpeed, growth.LevelModifier, 0);
            return fgcd;
        }
        return null;
    }

    public uint? GetLv1Action() {
        ClassJob? here = this.ClientState.LocalPlayer.ClassJob.GameData;

        // Only allow single links like MNK -> PUG to prevent infinite loops
        uint steps = 0;
        while (here != null && steps < 2) {
            if (this.Lv1ActionCache.TryGetValue(here.RowId, out var action)) return action;

            here = here.ClassJobParent.Value;
            steps = steps + 1;
        }

        return null;
    }

    public float? GetCurrentGcd() {
        uint? a = this.GetLv1Action();
        if (a is { } lv1Action) {
            return ActionManager.GetAdjustedRecastTime(ActionType.Action, lv1Action) / 1000.0f;
        }
        return null;
    }

    public TimeSpan GetWeaponDelay() {
        unsafe {
            var im = InventoryManager.Instance();
            var eq = im->GetInventoryContainer(InventoryType.EquippedItems);
            var weap = eq->GetInventorySlot(0);
            var iid = weap->ItemID;
            var ms = this.DataManager.GetExcelSheet<Item>().GetRow(iid).Delayms;
            return TimeSpan.FromMilliseconds(ms);
        }
    }

    public TimeSpan? GetAutoAttackDelay() {
        float? bgcd = this.GetBaseGcd();
        float? cgcd = this.GetCurrentGcd();
        if (bgcd is { } baseGcd && cgcd is { } currentGcd) {
            TimeSpan normalDelay = this.GetWeaponDelay();
            float lengthMultiplier = currentGcd / baseGcd;
            
            // See if Riddle of Earth is applied
            foreach (Dalamud.Game.ClientState.Statuses.Status status in this.ClientState.LocalPlayer.StatusList) {
                if (status.StatusId == RiddleOfWindId) {
                    lengthMultiplier *= 0.5f;
                }
            }

            return normalDelay.Multiply(lengthMultiplier);
        }
        return null;
    }
}
