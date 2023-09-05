using System;
using static PKHeX.Core.AbilityPermission;

namespace PKHeX.Core;

/// <summary>
/// Object that can be fed to a <see cref="IEncounterConvertible"/> converter to ensure that the resulting <see cref="PKM"/> meets rough specifications.
/// </summary>
public sealed record EncounterCriteria : IFixedNature, IFixedGender, IFixedAbilityNumber, IShinyPotential
{
    /// <summary>
    /// Default criteria with no restrictions (random) for all fields.
    /// </summary>
    public static readonly EncounterCriteria Unrestricted = new();

    /// <summary> End result's gender. </summary>
    /// <remarks> Leave as -1 to not restrict gender. </remarks>
    public byte Gender { get; init; } = FixedGenderUtil.GenderRandom;

    /// <summary> End result's ability numbers permitted. </summary>
    /// <remarks> Leave as <see cref="Any12H"/> to not restrict ability. </remarks>
    public AbilityPermission Ability { get; init; } = Any12H;

    /// <summary> End result's nature. </summary>
    /// <remarks> Leave as <see cref="Nature.Random"/> to not restrict nature. </remarks>
    public Nature Nature { get; init; } = Nature.Random;

    /// <summary> End result's shininess. </summary>
    /// <remarks> Leave as <see cref="Shiny.Random"/> to not restrict shininess. </remarks>
    public Shiny Shiny { get; init; }

    public int IV_HP  { get; init; } = RandomIV;
    public int IV_ATK { get; init; } = RandomIV;
    public int IV_DEF { get; init; } = RandomIV;
    public int IV_SPA { get; init; } = RandomIV;
    public int IV_SPD { get; init; } = RandomIV;
    public int IV_SPE { get; init; } = RandomIV;

    /// <summary>
    /// If the Encounter yields variable level ranges (e.g. RNG correlation), force the minimum level instead of yielding first match.
    /// </summary>
    public bool ForceMinLevelRange { get; set; }

    public sbyte TeraType { get; init; } = -1;

    // unused
    public int HPType { get; init; } = -1;

    private const int RandomIV = -1;

    /// <summary>
    /// Checks if the IVs are compatible with the encounter's defined IV restrictions.
    /// </summary>
    /// <param name="encounterIVs">Encounter template's IV restrictions. Speed is last!</param>
    /// <param name="generation">Destination generation</param>
    /// <returns>True if compatible, false if incompatible.</returns>
    public bool IsIVsCompatibleSpeedLast(Span<int> encounterIVs, int generation)
    {
        var IVs = encounterIVs;
        if (!ivCanMatch(IV_HP , IVs[0])) return false;
        if (!ivCanMatch(IV_ATK, IVs[1])) return false;
        if (!ivCanMatch(IV_DEF, IVs[2])) return false;
        if (!ivCanMatch(IV_SPA, IVs[3])) return false;
        if (!ivCanMatch(IV_SPD, IVs[4])) return false;
        if (!ivCanMatch(IV_SPE, IVs[5])) return false;

        return true;

        bool ivCanMatch(int requestedIV, int encounterIV)
        {
            if (requestedIV >= 30 && generation >= 6) // hyper training possible
                return true;
            return encounterIV == RandomIV || requestedIV == RandomIV || requestedIV == encounterIV;
        }
    }

    /// <inheritdoc cref="GetCriteria(IBattleTemplate, IPersonalInfo)"/>
    /// <param name="s">Template data (end result).</param>
    /// <param name="t">Personal table the end result will exist with.</param>
    public static EncounterCriteria GetCriteria(IBattleTemplate s, IPersonalTable t)
    {
        var pi = t.GetFormEntry(s.Species, s.Form);
        return GetCriteria(s, pi);
    }

    /// <summary>
    /// Creates a new <see cref="EncounterCriteria"/> by loading parameters from the provided <see cref="IBattleTemplate"/>.
    /// </summary>
    /// <param name="s">Template data (end result).</param>
    /// <param name="pi">Personal info the end result will exist with.</param>
    /// <returns>Initialized criteria data to be passed to generators.</returns>
    public static EncounterCriteria GetCriteria(IBattleTemplate s, IPersonalInfo pi) => new()
    {
        Gender = (byte)s.Gender,
        IV_HP = s.IVs[0],
        IV_ATK = s.IVs[1],
        IV_DEF = s.IVs[2],
        IV_SPE = s.IVs[3],
        IV_SPA = s.IVs[4],
        IV_SPD = s.IVs[5],
        HPType = s.HiddenPowerType,

        Ability = GetAbilityPermissions(s.Ability, pi),
        Nature = NatureUtil.GetNature(s.Nature),
        Shiny = s.Shiny ? Shiny.Always : Shiny.Never,
        TeraType = (sbyte)s.TeraType,
    };

    private static AbilityPermission GetAbilityPermissions(int ability, IPersonalAbility pi)
    {
        var count = pi.AbilityCount;
        if (count < 2 || pi is not IPersonalAbility12 a)
            return Any12;
        var dual = GetAbilityValueDual(ability, a);
        if (count == 2 || pi is not IPersonalAbility12H h) // prior to gen5
            return dual;
        if (ability == h.AbilityH)
            return dual == Any12 ? Any12H : OnlyHidden;
        return dual;
    }

    private static AbilityPermission GetAbilityValueDual(int ability, IPersonalAbility12 a)
    {
        if (ability == a.Ability1)
            return ability != a.Ability2 ? OnlyFirst : Any12;
        return ability == a.Ability2 ? OnlySecond : Any12;
    }

    /// <summary>
    /// Gets the nature to generate, random if unspecified by the template or criteria.
    /// </summary>
    public Nature GetNature(Nature encValue)
    {
        if ((uint)encValue < 25)
            return encValue;
        return GetNature();
    }

    /// <summary>
    /// Gets the nature to generate, random if unspecified.
    /// </summary>
    public Nature GetNature()
    {
        if (Nature != Nature.Random)
            return Nature;
        return (Nature)Util.Rand.Next(25);
    }

    /// <summary>
    /// Gets the gender to generate, random if unspecified by the template or criteria.
    /// </summary>
    public int GetGender(int gender, IGenderDetail pkPersonalInfo)
    {
        if ((uint)gender < 3)
            return gender;
        return GetGender(pkPersonalInfo);
    }

    /// <summary>
    /// Gets the gender to generate, random if unspecified.
    /// </summary>
    public int GetGender(IGenderDetail pkPersonalInfo)
    {
        if (!pkPersonalInfo.IsDualGender)
            return pkPersonalInfo.FixedGender();
        if (pkPersonalInfo.Genderless)
            return 2;
        if (Gender is 0 or 1)
            return Gender;
        return pkPersonalInfo.RandomGender();
    }

    /// <summary>
    /// Gets a random ability index (0/1/2) to generate, based off an encounter's <see cref="num"/>.
    /// </summary>
    public int GetAbilityFromNumber(AbilityPermission num)
    {
        if (num.IsSingleValue(out int index)) // fixed number
            return index;

        bool canBeHidden = num.CanBeHidden();
        return GetAbilityIndexPreference(canBeHidden);
    }

    private int GetAbilityIndexPreference(bool canBeHidden = false) => Ability switch
    {
        OnlyFirst => 0,
        OnlySecond => 1,
        OnlyHidden or Any12H when canBeHidden => 2, // hidden allowed
        _ => Util.Rand.Next(2),
    };

    /// <summary>
    /// Applies random IVs without any correlation.
    /// </summary>
    /// <param name="pk">Entity to mutate.</param>
    public void SetRandomIVs(PKM pk)
    {
        pk.IV_HP = IV_HP != RandomIV ? IV_HP : Util.Rand.Next(32);
        pk.IV_ATK = IV_ATK != RandomIV ? IV_ATK : Util.Rand.Next(32);
        pk.IV_DEF = IV_DEF != RandomIV ? IV_DEF : Util.Rand.Next(32);
        pk.IV_SPA = IV_SPA != RandomIV ? IV_SPA : Util.Rand.Next(32);
        pk.IV_SPD = IV_SPD != RandomIV ? IV_SPD : Util.Rand.Next(32);
        pk.IV_SPE = IV_SPE != RandomIV ? IV_SPE : Util.Rand.Next(32);
    }

    /// <summary>
    /// Applies random IVs with a minimum and maximum (bitshifted >> 1)
    /// </summary>
    /// <param name="pk">Entity to mutate.</param>
    /// <param name="minIV">Minimum IV from GO</param>
    /// <param name="maxIV">Maximum IV from GO</param>
    public void SetRandomIVsGO(PKM pk, int minIV = 0, int maxIV = 15)
    {
        var bareMin = (minIV << 1) | 1;
        var rnd = Util.Rand;
        pk.IV_HP =
              IV_HP  != RandomIV && IV_HP  >= bareMin ? IV_HP  | 1
            : (rnd.Next(minIV, maxIV + 1) << 1) | 1; // hp
        pk.IV_ATK = pk.IV_SPA =
              IV_ATK != RandomIV && IV_ATK >= bareMin ? IV_ATK | 1
            : IV_SPA != RandomIV && IV_SPA >= bareMin ? IV_SPA | 1
            : (rnd.Next(minIV, maxIV + 1) << 1) | 1; // attack
        pk.IV_DEF = pk.IV_SPD =
              IV_DEF != RandomIV && IV_DEF >= bareMin ? IV_DEF | 1
            : IV_SPD != RandomIV && IV_SPD >= bareMin ? IV_SPD | 1
            : (rnd.Next(minIV, maxIV + 1) << 1) | 1; // defense
        pk.IV_SPE =
              IV_SPE != RandomIV ? IV_SPE
            : rnd.Next(32); // speed
    }

    public void SetRandomIVs(PKM pk, int flawless)
    {
        Span<int> ivs = stackalloc[] { IV_HP, IV_ATK, IV_DEF, IV_SPE, IV_SPA, IV_SPD };
        flawless -= ivs.Count(31);
        int remain = ivs.Count(RandomIV);
        if (flawless > remain)
        {
            // Overwrite specified IVs until we have enough remaining slots.
            while (flawless > remain)
            {
                int index = Util.Rand.Next(6);
                if (ivs[index] is RandomIV or 31)
                    continue;
                ivs[index] = RandomIV;
                remain++;
            }
        }

        // Sprinkle in remaining flawless IVs
        while (flawless > 0)
        {
            int index = Util.Rand.Next(6);
            if (ivs[index] != RandomIV)
                continue;
            ivs[index] = 31;
            flawless--;
        }
        // Fill in the rest
        for (int i = 0; i < ivs.Length; i++)
        {
            if (ivs[i] == RandomIV)
                ivs[i] = Util.Rand.Next(32);
        }
        // Done.
        pk.SetIVs(ivs);
    }

    /// <summary>
    /// Applies random IVs without any correlation.
    /// </summary>
    /// <param name="pk">Entity to mutate.</param>
    /// <param name="template">Template to populate from</param>
    public void SetRandomIVs(PKM pk, IndividualValueSet template)
    {
        if (!template.IsSpecified)
        {
            SetRandomIVs(pk);
            return;
        }

        pk.IV_HP = Get(template.HP, IV_HP);
        pk.IV_ATK = Get(template.ATK, IV_ATK);
        pk.IV_DEF = Get(template.DEF, IV_DEF);
        pk.IV_SPE = Get(template.SPE, IV_SPE);
        pk.IV_SPA = Get(template.SPA, IV_SPA);
        pk.IV_SPD = Get(template.SPD, IV_SPD);

        static int Get(sbyte template, int request)
        {
            if (template != -1)
                return template;
            if (request != RandomIV)
                return request;
            return Util.Rand.Next(32);
        }
    }
}
