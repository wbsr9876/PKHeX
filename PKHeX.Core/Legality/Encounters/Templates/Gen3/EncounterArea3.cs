using System;
using static System.Buffers.Binary.BinaryPrimitives;

namespace PKHeX.Core;

/// <summary>
/// <see cref="GameVersion.Gen3"/> encounter area
/// </summary>
public sealed record EncounterArea3 : IEncounterArea<EncounterSlot3>, ISlotRNGType, IAreaLocation
{
    public EncounterSlot3[] Slots { get; }
    public GameVersion Version { get; }
    public SlotType Type { get; }

    public readonly byte Rate;
    public readonly byte Location;

    public bool IsMatchLocation(int location) => location == Location;

    public static EncounterArea3[] GetAreas(BinLinkerAccessor input, GameVersion game)
    {
        var result = new EncounterArea3[input.Length];
        for (int i = 0; i < result.Length; i++)
            result[i] = new EncounterArea3(input[i], game);
        return result;
    }

    public static EncounterArea3[] GetAreasSwarm(BinLinkerAccessor input, GameVersion game)
    {
        var result = new EncounterArea3[input.Length];
        for (int i = 0; i < result.Length; i++)
            result[i] = new EncounterArea3(input[i], game, SlotType.Swarm | SlotType.Grass);
        return result;
    }

    private EncounterArea3(ReadOnlySpan<byte> data, GameVersion game)
    {
        Location = data[0];
        Type = (SlotType)data[2];
        Rate = data[3];
        Version = game;

        Slots = ReadRegularSlots(data);
    }

    private EncounterArea3(ReadOnlySpan<byte> data, GameVersion game, SlotType type)
    {
        Location = data[0];
        Type = type;
        Rate = data[3];
        Version = game;

        Slots = ReadSwarmSlots(data);
    }

    private EncounterSlot3[] ReadRegularSlots(ReadOnlySpan<byte> data)
    {
        const int size = 10;
        int count = (data.Length - 4) / size;
        var slots = new EncounterSlot3[count];
        for (int i = 0; i < slots.Length; i++)
        {
            int offset = 4 + (size * i);
            var entry = data.Slice(offset, size);
            slots[i] = ReadRegularSlot(entry);
        }

        return slots;
    }

    private EncounterSlot3 ReadRegularSlot(ReadOnlySpan<byte> entry)
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
        return new EncounterSlot3(this, species, form, min, max, slotNum, mpi, mpc, sti, stc);
    }

    private EncounterSlot3[] ReadSwarmSlots(ReadOnlySpan<byte> data)
    {
        const int size = 14;
        int count = (data.Length - 4) / size;
        var slots = new EncounterSlot3[count];
        for (int i = 0; i < slots.Length; i++)
        {
            int offset = 4 + (size * i);
            var entry = data.Slice(offset, size);
            slots[i] = ReadSwarmSlot(entry);
        }

        return slots;
    }

    private EncounterSlot3Swarm ReadSwarmSlot(ReadOnlySpan<byte> entry)
    {
        ushort species = ReadUInt16LittleEndian(entry);
        // form always 0
        byte slotNum = entry[3];
        byte min = entry[4];
        byte max = entry[5];

        var moves = new Moveset(
            ReadUInt16LittleEndian(entry[06..]),
            ReadUInt16LittleEndian(entry[08..]),
            ReadUInt16LittleEndian(entry[10..]),
            ReadUInt16LittleEndian(entry[12..])
        );

        return new EncounterSlot3Swarm(this, species, min, max, slotNum, moves);
    }
}
