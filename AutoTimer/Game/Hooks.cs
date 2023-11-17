using System;
using System.Runtime.InteropServices;
using Dalamud.Game;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;

namespace AutoTimer.Game; 

public class Hooks : IDisposable {
    private delegate void ActionEffect1HandlerDelegate(
        uint rcx_actorId,
        IntPtr rdx_data
    );

    private readonly Hook<ActionEffect1HandlerDelegate> actionEffect1HandlerHook;
    
    private ISigScanner SigScanner { get; init; }
    private HooksListener HooksListener { get; init; }

    public Hooks(HooksListener listener, ISigScanner scanner, IGameInteropProvider gameInteropProvider) {
        this.SigScanner = scanner;
        this.HooksListener = listener;

        var actionEffect1HandlerPtr = this.SigScanner.ScanText(Signatures.ActionEffect1);
        this.actionEffect1HandlerHook = gameInteropProvider.HookFromAddress<ActionEffect1HandlerDelegate>(
            actionEffect1HandlerPtr, this.ActionEffect1HandlerDetour);
    }
    
    public void Enable() {
        this.actionEffect1HandlerHook.Enable();
    }

    public void Disable() {
        this.actionEffect1HandlerHook.Disable();
    }

    public void Dispose() {
        this.actionEffect1HandlerHook.Dispose();
    }
    
    private void ActionEffect1HandlerDetour(
        uint rcx_actorId,
        IntPtr rdx_data
    ) {
        this.actionEffect1HandlerHook.Original(rcx_actorId, rdx_data);
        var ability1 = Marshal.PtrToStructure<ActionEffect1>(rdx_data);
        this.HooksListener.HandleActionEffect1(rcx_actorId, ability1);
    }
}
