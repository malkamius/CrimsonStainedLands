﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using CrimsonStainedLands.Extensions;
using CrimsonStainedLands.World;

namespace CrimsonStainedLands
{
    public enum AffectRemoveReason
    {
        Cleansed,
        Moved,
        Died,
        ChangedPosition,
        WoreOff,
        Other,
        Stripped,
        Combat
    }

    public enum ApplyTypes
    {
        None,
        Armor,
        Strength,
        Dexterity,
        Intelligence,
        Wisdom,
        Constitution,
        Height,
        Weight,
        Mana,
        Hitroll,
        DamageRoll,
        Saves,
        SavingParalysis,
        SavingRod,
        SavingPetrification,
        Breath,
        Hitpoints,
        SavingSpell,
        SavingBreath,
        Move,
        Sex,
        Charisma,
        Age,
        Str = Strength,
        Wis = Wisdom,
        Int = Intelligence,
        Dex = Dexterity,
        Con = Constitution,
        Chr = Charisma,
        Damroll = DamageRoll,
        Hp = Hitpoints,
        AC = Armor,
        SavingPetri = SavingPetrification
    }

    public enum AffectWhere
    {
        ToAffects = 1,
        ToObject,
        ToImmune,
        ToResist,
        ToVulnerabilities,
        ToWeapon,
        ToForm,
        ToSkill,
        ToDamageNoun,
    }

    public enum AffectTypes
    {
        None = 0,
        Spell = 1,
        Skill = 2,
        Power,
        Malady,
        Commune,
        Invis,
        Song,
        DispelAtDeath,
        Strippable,
        GreaterEnliven
    }

    public enum AffectFlags
    {
        Blind,
        Invisible,
        DetectEvil,
        DetectInvis,
        DetectMagic,
        DetectHidden,
        AcuteVision,
        DetectGood,
        DetectIllusion,
        Sanctuary,
        AnthemOfResistance,
        FaerieFire,
        Infrared,
        Curse,
        Poison,
        ProtectionEvil,
        ProtectionGood,
        Sneak,
        Hide,
        Sleep,
        Charm,
        Flying,
        PassDoor,
        Haste,
        Calm,
        Plague,
        Weaken,
        DarkVision,
        NightVision,
        Berserk,
        Swim,
        Regeneration,
        Slow,
        Camouflage,
        Burrow,
        Rabies,
        FastRunning,
        PlayDead,
        Bloodthirst,
        WaterBreathing,
        Retract,
        Deafen,
        EnhancedFastHealing,
        
        GrandNocturne,
        SuddenDeath,
        Distracted,
        Silenced,
        BindHands,
        BindLegs,
        Greased,
        Smelly,
        ArcaneVision,
        Lightning,
        Shield,
        Watershield,
        Airshield,
        Fireshield,
        Lightningshield,
        Frostshield,
        Earthshield,
        Immolation,
        BestialFury,
        SkinOfTheDisplacer,
        ZigZagFeint,
        Sated,
        Quenched,
        Haven,
        Ghost,
        KnowAlignment,
        Protection,
        DuelChallenge,
        DuelChallenged,
        DuelStarting,
        DuelInProgress,
        DuelCancelling,
        ApplyingFirstAid,
        FirstAidBeingApplied,
        ProtectEvil = ProtectionEvil,
        ProtectGood = ProtectionGood,
    }

    public enum Frequency
    {
        Violence = 1,
        Tick = 2,
    }

    public class AffectData
    {
        public enum StripAndSaveFlags
        {
            DoNotSave,
            RemoveOnMove,
            RemoveOnPositionChange,
            RemoveOnCombat,
            PersistThroughDeath,
        }

        public string ownerName = "";
        public string name = "";
        public string displayName = "";
        public int wait;
        public AffectWhere where = AffectWhere.ToAffects;
        public List<WeaponDamageTypes> DamageTypes = new List<WeaponDamageTypes>();
        public int level;
        public int duration;
        public Frequency frequency = Frequency.Tick;
        public ApplyTypes location;
        public List<AffectFlags> flags = new List<AffectFlags>();
        public int modifier;
        public bool hidden;
        public AffectTypes affectType = AffectTypes.Spell;

        public SkillSpell skillSpell;
        public string endMessage = "";
        public string endMessageToRoom = "";
        public string beginMessage = "";
        public string beginMessageToRoom = "";
        public string tickProgram = "";
        public string endProgram = "";

        public XElement ExtraState = new XElement("ExtraState");

        public List<StripAndSaveFlags> RemoveAndSaveFlags = new List<StripAndSaveFlags>();

        public AffectData()
        {

        }

        public AffectData(AffectData toCopy)
        {
            ownerName = toCopy.ownerName;
            name = toCopy.name;
            displayName = toCopy.displayName;
            wait = toCopy.wait;
            where = toCopy.where;
            level = toCopy.level;
            duration = toCopy.duration;
            frequency = toCopy.frequency;
            location = toCopy.location;
            flags.AddRange(toCopy.flags);
            modifier = toCopy.modifier;
            hidden = toCopy.hidden;
            affectType = toCopy.affectType;
            endMessage = toCopy.endMessage;
            endMessageToRoom = toCopy.endMessageToRoom;
            beginMessage = toCopy.beginMessage;
            beginMessageToRoom = toCopy.beginMessageToRoom;
            skillSpell = toCopy.skillSpell;
            DamageTypes.AddRange(toCopy.DamageTypes);
            tickProgram = toCopy.tickProgram;
            endProgram = toCopy.endProgram;
            RemoveAndSaveFlags.AddRange(toCopy.RemoveAndSaveFlags);
            ExtraState = new XElement("ExtraState", toCopy.ExtraState.Elements());
        }

        public AffectData(XElement affectElement)
        {
            ownerName = affectElement.GetAttributeValue("ownerName");
            name = affectElement.GetAttributeValue("name");
            displayName = affectElement.GetAttributeValue("displayName");
            wait = affectElement.GetAttributeValueInt("wait");
            Utility.GetEnumValue<AffectWhere>(affectElement.GetAttributeValue("where"), ref where);
            level = affectElement.GetAttributeValueInt("level");
            duration = affectElement.GetAttributeValueInt("duration");
            Utility.GetEnumValue<Frequency>(affectElement.GetAttributeValue("frequency"), ref frequency, Frequency.Tick);
            Utility.GetEnumValue<ApplyTypes>(affectElement.GetAttributeValue("location"), ref location);
            flags.AddRange(Utility.LoadFlagList<AffectFlags>(affectElement.GetAttributeValue("flags")));
            modifier = affectElement.GetAttributeValueInt("modifier");
            hidden = affectElement.GetAttributeValue("hidden").StringPrefix("true");
            Utility.GetEnumValue<AffectTypes>(affectElement.GetAttributeValue("affectType"), ref affectType);
            endMessage = affectElement.GetAttributeValue("endMessage");
            endMessageToRoom = affectElement.GetAttributeValue("endMessageToRoom");
            beginMessage = affectElement.GetAttributeValue("beginMessage");
            beginMessageToRoom = affectElement.GetAttributeValue("beginMessageToRoom");

            DamageTypes.AddRange(Utility.LoadFlagList<WeaponDamageTypes>(affectElement.GetAttributeValue("DamageTypes")));
            skillSpell = SkillSpell.SkillLookup(affectElement.GetAttributeValue("skillSpell"));
            tickProgram = affectElement.GetAttributeValue("TickProgram");
            endProgram = affectElement.GetAttributeValue("EndProgram");
            RemoveAndSaveFlags.AddRange(Utility.LoadFlagList<StripAndSaveFlags>(affectElement.GetAttributeValue("RemoveAndSaveFlags")));
            ExtraState = new XElement("ExtraState", (affectElement.GetElement("ExtraState") ?? new XElement("ExtraState")).Elements());
        }

        public XElement Element
        {
            get
            {
                return new XElement("Affect",
                    !ownerName.ISEMPTY() ? new XAttribute("ownerName", ownerName.TOSTRINGTRIM()) : null,
                    !name.ISEMPTY() ? new XAttribute("name", name.TOSTRINGTRIM()) : null,
                    !displayName.ISEMPTY() ? new XAttribute("displayName", displayName.TOSTRINGTRIM()) : null,
                    wait != 0 ? new XAttribute("wait", wait) : null,
                    new XAttribute("where", where.ToString()),
                    new XAttribute("level", level),
                    new XAttribute("duration", duration),
                    frequency != Frequency.Tick ? new XAttribute("frequency", frequency.ToString()) : null,
                    new XAttribute("location", location.ToString()),
                    flags.Any() ? new XAttribute("flags", string.Join(" ", from flag in flags select flag.ToString())) : null,
                    DamageTypes.Any() ? new XAttribute("DamageTypes", string.Join(" ", from dt in DamageTypes select dt.ToString())) : null,
                    new XAttribute("modifier", modifier),
                    new XAttribute("hidden", hidden.ToString()),
                    new XAttribute("affectType", affectType.ToString()),
                    !endMessage.ISEMPTY() ? new XAttribute("endMessage", endMessage.TOSTRINGTRIM()) : null,
                    !endMessageToRoom.ISEMPTY() ? new XAttribute("endMessageToRoom", endMessageToRoom.TOSTRINGTRIM()) : null,
                    !beginMessage.ISEMPTY() ? new XAttribute("beginMessage", beginMessage.TOSTRINGTRIM()) : null,
                    !beginMessageToRoom.ISEMPTY() ? new XAttribute("beginMessageToRoom", beginMessageToRoom.TOSTRINGTRIM()) : null,
                    skillSpell != null ? new XAttribute("skillSpell", skillSpell != null ? skillSpell.internalName : "") : null,
                    !tickProgram.ISEMPTY() ? new XAttribute("TickProgram", tickProgram.TOSTRINGTRIM()) : null,
                    !endProgram.ISEMPTY() ? new XAttribute("EndProgram", endProgram.TOSTRINGTRIM()) : null,
                    RemoveAndSaveFlags.Any() ? new XAttribute("RemoveAndSaveFlags", string.Join(" ", from removeflag in RemoveAndSaveFlags select removeflag.ToString())) : null,
                    ExtraState
                    );
            }
        }

        public AffectRemoveReason RemovedReason { get; internal set; }

        public Character GetOwner() => Character.GetCharacterWorld(null, ownerName, false, false, true);
    }
}