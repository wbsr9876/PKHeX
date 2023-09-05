using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics.CodeAnalysis;

namespace PKHeX.Core;

public sealed class EncounterGenerator3 : IEncounterGenerator
{
    public static readonly EncounterGenerator3 Instance = new();

    public IEnumerable<IEncounterable> GetPossible(PKM _, EvoCriteria[] chain, GameVersion game, EncounterTypeGroup groups)
    {
        var iterator = new EncounterPossible3(chain, groups, game);
        foreach (var enc in iterator)
            yield return enc;
    }

    public IEnumerable<IEncounterable> GetEncounters(PKM pk, LegalInfo info)
    {
        var chain = EncounterOrigin.GetOriginChain(pk, 3);
        return GetEncounters(pk, chain, info);
    }

    public IEnumerable<IEncounterable> GetEncounters(PKM pk, EvoCriteria[] chain, LegalInfo info)
    {
        if (chain.Length == 0)
            yield break;

        info.PIDIV = MethodFinder.Analyze(pk);
        IEncounterable? partial = null;

        foreach (var z in GetEncountersInner(pk, chain, info))
        {
            if (IsTypeCompatible(z, pk, info.PIDIV.Type))
                yield return z;
            else
                partial ??= z;
        }
        static bool IsTypeCompatible(IEncounterTemplate enc, PKM pk, PIDType type)
        {
            if (enc is IRandomCorrelation r)
                return r.IsCompatible(type, pk);
            return type == PIDType.None;
        }

        if (partial == null)
            yield break;

        info.PIDIVMatches = false;
        yield return partial;
    }

    private static IEnumerable<IEncounterable> GetEncountersInner(PKM pk, EvoCriteria[] chain, LegalInfo info)
    {
        var game = (GameVersion)pk.Version;
        var iterator = new EncounterEnumerator3(pk, chain, game);
        EncounterSlot3? deferSlot = null;
        List<Frame>? frames = null;
        foreach (var enc in iterator)
        {
            var e = enc.Encounter;
            if (e is not EncounterSlot3 s3)
            {
                yield return e;
                continue;
            }

            var wildFrames = frames ?? AnalyzeFrames(pk, info);
            var frame = wildFrames.Find(s => s.IsSlotCompatibile(s3, pk));
            if (frame != null)
                yield return s3;
            deferSlot ??= s3;
        }
        if (deferSlot != null)
            yield return deferSlot;
    }

    private static List<Frame> AnalyzeFrames(PKM pk, LegalInfo info)
    {
        return FrameFinder.GetFrames(info.PIDIV, pk).ToList();
    }

    private const int Generation = 3;
    private const EntityContext Context = EntityContext.Gen3;
    private const byte EggLevel = 5;

    private static EncounterEgg CreateEggEncounter(ushort species, byte form, GameVersion version)
    {
        if (FormInfo.IsBattleOnlyForm(species, form, Generation) || species is (int)Species.Castform)
            form = FormInfo.GetOutOfBattleForm(species, form, Generation);
        return new EncounterEgg(species, form, EggLevel, Generation, version, Context);
    }

    private static (ushort Species, byte Form) GetBaby(EvoCriteria lowest)
    {
        return EvolutionTree.Evolves3.GetBaseSpeciesForm(lowest.Species, lowest.Form);
    }

    public static bool TryGetEgg(ReadOnlySpan<EvoCriteria> chain, GameVersion version, [NotNullWhen(true)] out EncounterEgg? result)
    {
        result = null;
        var devolved = chain[^1];
        if (!devolved.InsideLevelRange(EggLevel))
            return false;

        // Ensure most devolved species is the same as the egg species.
        var (species, form) = GetBaby(devolved);
        if (species != devolved.Species && !Breeding.IsSplitBreedNotBabySpecies3(devolved.Species))
            return false; // not a split-breed.

        // Sanity Check 1
        if (!Breeding.CanHatchAsEgg(species))
            return false;
        // Sanity Check 2
        if (!Breeding.CanHatchAsEgg(species, form, Context))
            return false;
        // Sanity Check 3
        if (!PersonalTable.E.IsPresentInGame(species, form))
            return false;

        result = CreateEggEncounter(species, form, version);
        return true;
    }

    // Version is not updated when hatching an Egg in Gen3. Version is a clear indicator of the game it originated on.

    public static bool TryGetSplit(EncounterEgg other, ReadOnlySpan<EvoCriteria> chain, [NotNullWhen(true)] out EncounterEgg? result)
    {
        result = null;
        // Check for split-breed
        var devolved = chain[^1];
        if (other.Species == devolved.Species)
        {
            if (chain.Length < 2)
                return false; // no split-breed
            devolved = chain[^2];
        }
        if (!Breeding.IsSplitBreedNotBabySpecies3(devolved.Species))
            return false;

        result = other with { Species = devolved.Species, Form = devolved.Form };
        return true;
    }
}
