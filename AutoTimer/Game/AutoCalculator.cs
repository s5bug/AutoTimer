using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Logging.Internal;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.Sheets;
using Action = Lumina.Excel.Sheets.Action;

namespace AutoTimer.Game;

public class AutoCalculator {
    public const int WeaponskillActionCategoryId = 3;
    public const int SpellActionCategoryId = 2;

    public const int SkillspeedAttributeId = 45;
    public const int SpellspeedAttributeId = 46;

    public const uint RiddleOfWindId = 2687;
    public const uint InspirationId = 3689;

    private IClientState ClientState { get; init; }
    private IDataManager DataManager { get; init; }

    private Dictionary<uint, uint> Lv1ActionCache { get; init; }

    public AutoCalculator(IClientState clientState, IDataManager dataManager) {
        this.ClientState = clientState;
        this.DataManager = dataManager;

        this.Lv1ActionCache = new();
        // TODO look into ClassJobActionSort
        foreach (Action a in this.DataManager.GetExcelSheet<Action>()) {
            if (a.ClassJobLevel == 1) {
                var actionId = a.RowId;
                var classId = a.ClassJob.RowId;
                this.Lv1ActionCache[classId] = actionId;
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

            int? maybeSpeedStat = actionData.ActionCategory.RowId switch {
                // See CharacterPanelRefined Attributes
                WeaponskillActionCategoryId => GetAttribute(SkillspeedAttributeId),
                SpellActionCategoryId => GetAttribute(SpellspeedAttributeId),
                _ => null
            };
            if (maybeSpeedStat is not { } speedStat) return null;
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
        ClassJob? here = this.ClientState.LocalPlayer?.ClassJob.ValueNullable;

        // Only allow single links like MNK -> PUG to prevent infinite loops
        uint steps = 0;
        while (here is { } currentClass && steps < 2) {
            if (this.Lv1ActionCache.TryGetValue(currentClass.RowId, out var action)) return action;

            here = currentClass.ClassJobParent.ValueNullable;
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
            var iid = weap->ItemId;
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
            
            // See if Riddle of Earth is applied and apply its auto haste
            // See if Inspiration is applied and undo its haste (a consequence of measuring Fire in Red for haste)
            foreach (Dalamud.Game.ClientState.Statuses.Status status in this.ClientState.LocalPlayer.StatusList) {
                if (status.StatusId == RiddleOfWindId) {
                    lengthMultiplier *= 0.5f;
                }
                if (status.StatusId == InspirationId) {
                    lengthMultiplier *= (float) (1.0 / 0.75);
                }
            }

            return normalDelay.Multiply(lengthMultiplier);
        }
        return null;
    }
}
