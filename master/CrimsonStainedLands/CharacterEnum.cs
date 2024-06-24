using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrimsonStainedLands
{
    public enum ActType
    {
        ToRoom = 0,
        ToRoomNotVictim = 1,
        ToVictim = 2,
        ToChar = 3,
        ToAll = 4,
        ToGroupInRoom = 5,
        GlobalNotVictim = 6
    }

    public enum WearSlotIDs
    {
        None,
        LeftFinger,
        RightFinger,
        About,
        Neck1,
        Neck2,
        Head,
        Chest,
        Legs,
        Feet,
        LeftWrist,
        RightWrist,
        Wield,
        DualWield,
        Held,
        Shield,
        Floating,
        Tattoo,
        Hands,
        Arms,
        Waist
    }

    public enum Positions
    {
        Dead = 0,
        Mortal = 1,
        Incapacitated,
        Stunned,
        Sleeping,
        Resting,
        Sitting,
        Fighting,
        Standing,

    }

    public enum ActFlags
    {
        GuildMaster = 1,
        NoAlign = 2,
        Color = 3,
        DamageOnType,
        SkipAutoDeletePrompt,
        AutoAssist,
        AutoExit,
        AutoLoot,
        AutoSac,
        AutoGold,
        AutoSplit,
        Evaluation,
        Betrayer,
        HeroImm,
        HolyLight,
        CanLoot,
        NoSummon,
        NoFollow,
        NoTransfer,
        PlayerDenied,
        Frozen,
        AssistPlayer,
        AssistAll,
        AssistVnum,
        AssistAlign,
        AssistRace,
        Sentinel,           /*Staysinoneroom*/
        Scavenger,          /*Picksupobjects*/
        CabalMob,          /*Attacksnon-cabalPC's*/
        Aggressive,         /*AttacksPC's*/
        StayArea,          /*Won'tleavearea*/
        Wimpy,
        Pet,                /*Autosetforpets*/
        Trainer,              /*CantrainPC's*/

        NoTrack,
        Undead,
        Cleric,
        Mage,
        Thief,
        Warrior,
        NoPurge,
        Outdoors,
        Indoors,
        IsHealer,
        Banker,
        Brief,
        NPC,
        NoWander,

        Changer,
        Healer,

        Shopkeeper,

        AreaAttack,
        WizInvis,
        Fade,
        AssistPlayers,
        AssistGuard,
        Rescue,
        UpdateAlways,

        Backstab,
        Bash,
        Berserk,
        Disarm,
        Dodge,
        Fast,
        Kick,
        DirtKick,
        Parry,
        Tail,
        Trip,
        Crush,
        PartingBlow,

        Train = Trainer,
        Gain = Trainer,
        Practice = GuildMaster,
        ColorOn = Color,
        NewbieChannel = 71,
        NoDuels = 72,
        AFK = 73,
    }

    public enum Sexes
    {
        None = 0,
        Male,
        Female,
        Either = None
    }

    public enum CharacterSize
    {
        Tiny = 0,
        Small,
        Medium,
        Large,
        Huge,
        Giant
    }

    public enum ImmuneStatus
    {
        Normal,
        Immune,
        Resistant,
        Vulnerable
    }

    public class WearSlot
    {
        public WearSlotIDs id;
        public WearFlags flag;
        public string slot;
        internal string wearString;
        internal string wearStringOthers;
    }
}
