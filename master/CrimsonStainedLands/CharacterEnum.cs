using CrimsonStainedLands.World;
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
