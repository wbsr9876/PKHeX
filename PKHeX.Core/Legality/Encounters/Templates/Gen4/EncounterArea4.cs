using System;
using static System.Buffers.Binary.BinaryPrimitives;

namespace PKHeX.Core;

/// <summary>
/// <see cref="GameVersion.Gen4"/> encounter area
/// </summary>
public sealed record EncounterArea4 : IEncounterArea<EncounterSlot4>, ISlotRNGType, IGroundTypeTile, IAreaLocation
{
    public EncounterSlot4[] Slots { get; }
    public GameVersion Version { get; }
    public SlotType Type { get; }
    public GroundTileAllowed GroundTile { get; }

    public readonly ushort Location;
    public readonly byte Rate;

    public bool IsMatchLocation(int location) => location == Location;

    public static EncounterArea4[] GetAreas(BinLinkerAccessor input, GameVersion game)
    {
        var result = new EncounterArea4[input.Length];
        for (int i = 0; i < result.Length; i++)
            result[i] = new EncounterArea4(input[i], game);
        return result;
    }

    private EncounterArea4(ReadOnlySpan<byte> data, GameVersion game)
    {
        Location = ReadUInt16LittleEndian(data);
        Type = (SlotType)data[2];
        Rate = data[3];
        Version = game;
        // although GroundTilePermission flags are 32bit, none have values > 16bit.
        GroundTile = (GroundTileAllowed)ReadUInt16LittleEndian(data[4..]);

        Slots = ReadRegularSlots(data);
    }

    private EncounterSlot4[] ReadRegularSlots(ReadOnlySpan<byte> data)
    {
        const int size = 10;
        int count = (data.Length - 6) / size;
        var slots = new EncounterSlot4[count];
        for (int i = 0; i < slots.Length; i++)
        {
            int offset = 6 + (size * i);
            var entry = data.Slice(offset, size);
            slots[i] = ReadRegularSlot(entry);
        }

        return slots;
    }

    private EncounterSlot4 ReadRegularSlot(ReadOnlySpan<byte> entry)
    {
        ushort species = ReadUInt16LittleEndian(entry);
        byte form = entry[2];
        byte slotNum = entry[3];
        byte min = entry[4];
        byte max = entry[5];
        byte mpi = entry[6];
        byte mpc = entry[7];
        byte sti = entry[8];
        byte stc = entry[9];
        return new EncounterSlot4(this, species, form, min, max, slotNum, mpi, mpc, sti, stc);
    }

    public bool IsMunchlaxTree(ITrainerID32 pk) => IsMunchlaxTree(pk, Location);

    private static bool IsMunchlaxTree(ITrainerID32 pk, ushort location)
    {
        // We didn't encode the honey tree index to the encounter slot resource.
        // Check if any of the slot's location doesn't match any of the groupC trees' area location ID.
        var trees = SAV4Sinnoh.CalculateMunchlaxTrees(pk.TID16, pk.SID16);
        return IsMunchlaxTree(trees, location);
    }

    private static bool IsMunchlaxTree(in MunchlaxTreeSet4 trees, ushort location)
    {
        return LocationID_HoneyTree[trees.Tree1] == location
            || LocationID_HoneyTree[trees.Tree2] == location
            || LocationID_HoneyTree[trees.Tree3] == location
            || LocationID_HoneyTree[trees.Tree4] == location;
    }

    private static ReadOnlySpan<byte> LocationID_HoneyTree => new byte[]
    {
        20, // 00 Route 205 Floaroma
        20, // 01 Route 205 Eterna
        21, // 02 Route 206
        22, // 03 Route 207
        23, // 04 Route 208
        24, // 05 Route 209
        25, // 06 Route 210 Solaceon
        25, // 07 Route 210 Celestic
        26, // 08 Route 211
        27, // 09 Route 212 Hearthome
        27, // 10 Route 212 Pastoria
        28, // 11 Route 213
        29, // 12 Route 214
        30, // 13 Route 215
        33, // 14 Route 218
        36, // 15 Route 221
        37, // 16 Route 222
        47, // 17 Valley Windworks
        48, // 18 Eterna Forest
        49, // 19 Fuego Ironworks
        58, // 20 Floaroma Meadow
    };
}
