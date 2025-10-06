namespace CrimsonStainedLands
{
    public abstract class NamedFlag
    {
        public string Name { get; set; }

        public NamedFlag()
        {
        }

        public override string ToString()
        {
            return Name;
        }

        public static IEnumerable<string> GetNames()
        {
            return new List<string>();
        }

        public static IEnumerable<NamedFlag> GetValues()
        {
            return new List<NamedFlag>();
        }
    }

    public partial class ActFlags : NamedFlag
    {

        public static readonly ActFlags GuildMaster = new();
        public static readonly ActFlags NoAlign = new();
        public static readonly ActFlags Color = new();
        public static readonly ActFlags DamageOnType = new();
        public static readonly ActFlags SkipAutoDeletePrompt = new();
        public static readonly ActFlags AutoAssist = new();
        public static readonly ActFlags AutoExit = new();
        public static readonly ActFlags AutoLoot = new();
        public static readonly ActFlags AutoSac = new();
        public static readonly ActFlags AutoGold = new();
        public static readonly ActFlags AutoSplit = new();
        public static readonly ActFlags Evaluation = new();
        public static readonly ActFlags Betrayer = new();
        public static readonly ActFlags HeroImm = new();
        public static readonly ActFlags HolyLight = new();
        public static readonly ActFlags CanLoot = new();
        public static readonly ActFlags NoSummon = new();
        public static readonly ActFlags NoFollow = new();
        public static readonly ActFlags NoTransfer = new();
        public static readonly ActFlags PlayerDenied = new();
        public static readonly ActFlags Frozen = new();
        public static readonly ActFlags AssistPlayer = new();
        public static readonly ActFlags AssistAll = new();
        public static readonly ActFlags AssistVnum = new();
        public static readonly ActFlags AssistAlign = new();
        public static readonly ActFlags AssistRace = new();
        public static readonly ActFlags Sentinel = new();
        public static readonly ActFlags Scavenger = new();
        public static readonly ActFlags CabalMob = new();
        public static readonly ActFlags Aggressive = new();
        public static readonly ActFlags StayArea = new();
        public static readonly ActFlags Wimpy = new();
        public static readonly ActFlags Pet = new();
        public static readonly ActFlags Trainer = new();
        public static readonly ActFlags NoTrack = new();
        public static readonly ActFlags Undead = new();
        public static readonly ActFlags Cleric = new();
        public static readonly ActFlags Mage = new();
        public static readonly ActFlags Thief = new();
        public static readonly ActFlags Warrior = new();
        public static readonly ActFlags NoPurge = new();
        public static readonly ActFlags Outdoors = new();
        public static readonly ActFlags Indoors = new();
        public static readonly ActFlags IsHealer = new();
        public static readonly ActFlags Banker = new();
        public static readonly ActFlags Brief = new();
        public static readonly ActFlags NPC = new();
        public static readonly ActFlags NoWander = new();
        public static readonly ActFlags Changer = new();
        public static readonly ActFlags Healer = new();
        public static readonly ActFlags Shopkeeper = new();
        public static readonly ActFlags AreaAttack = new();
        public static readonly ActFlags WizInvis = new();
        public static readonly ActFlags Fade = new();
        public static readonly ActFlags AssistPlayers = new();
        public static readonly ActFlags AssistGuard = new();
        public static readonly ActFlags Rescue = new();
        public static readonly ActFlags UpdateAlways = new();
        public static readonly ActFlags Backstab = new();
        public static readonly ActFlags Bash = new();
        public static readonly ActFlags Berserk = new();
        public static readonly ActFlags Disarm = new();
        public static readonly ActFlags Dodge = new();
        public static readonly ActFlags Fast = new();
        public static readonly ActFlags Kick = new();
        public static readonly ActFlags DirtKick = new();
        public static readonly ActFlags Parry = new();
        public static readonly ActFlags Tail = new();
        public static readonly ActFlags Trip = new();
        public static readonly ActFlags Crush = new();
        public static readonly ActFlags PartingBlow = new();
        public static readonly ActFlags Train = Trainer;
        public static readonly ActFlags Gain = Trainer;
        public static readonly ActFlags Practice = GuildMaster;
        public static readonly ActFlags ColorOn = Color;
        public static readonly ActFlags NewbieChannel = new();
        public static readonly ActFlags NoDuels = new();
        public static readonly ActFlags AFK = new();
        public static readonly ActFlags OOCChannel = new();

        // Use a dictionary for cached lookup by name
        public static readonly Dictionary<string, ActFlags> FlagsByName = new()
        {
        };

        static ActFlags()
        {
            typeof(ActFlags).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                .Where(f => f.FieldType == typeof(ActFlags))
                .ToList()
                .ForEach(field =>
                {
                    var flag = (NamedFlag)field.GetValue(null);
                    if (flag.Name == null)
                    {
                        flag.Name = field.Name;
                    }
                    FlagsByName[field.Name] = (ActFlags)flag;
                });
        }

        public static ActFlags GetFlag(string name)
        {
            return FlagsByName.TryGetValue(name, out var flag) ? flag : null;
        }

        public new static IEnumerable<string> GetNames()
        {
            return FlagsByName.Keys.ToArray();
        }

        public new static IEnumerable<NamedFlag> GetValues()
        {
            return FlagsByName.Values.ToArray();
        }
    }
}