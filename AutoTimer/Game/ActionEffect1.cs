using System.Runtime.InteropServices;

namespace AutoTimer.Game; 

public enum AbilityDisplayType : byte {
    HideActionName = 0x00,
    ShowActionName = 0x01,
    ShowItemName = 0x02,
    MountName = 0x0D
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct AbilityHeader {
    public ulong AnimationTargetId;
    public uint ActionId;
    public uint Sequence;
    public float AnimationLockTime;
    public uint SomeTargetId;
    public ushort SourceSequence;
    public ushort Rotation;
    public ushort ActionAnimationId;
    public byte Variation;
    public AbilityDisplayType EffectDisplayType;
    public byte Unknown20;
    public byte EffectCount;
    public ushort Padding21;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct ActionEffect1 {
    public AbilityHeader Header;
    public uint Padding1;
    public ushort Padding2;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
    public uint[] Effects;

    public ushort Padding3;
    public uint Padding4;
    public ulong TargetId;
    public uint Padding5;
}
