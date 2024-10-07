using CrimsonStainedLands.Extensions;
using CrimsonStainedLands.World;
using System.Collections.Generic;
using System.Linq;

namespace CrimsonStainedLands
{
    internal class DamageMessageText
    {
        public static List<DamageMessageText> DamageMessageTexts = new List<DamageMessageText>();
        
        public static DamageMessageText GetWeaponDamageMessageText(WeaponDamageTypes damagetype, float damage)
        {
            var dammsgs = DamageMessageTexts.OrderBy(wmd => wmd.LessThanOrEqualTo).Where(wmd => damage <= wmd.LessThanOrEqualTo);
            var dammsg = dammsgs.FirstOrDefault(wmd => wmd.DamageType == damagetype) ?? dammsgs.FirstOrDefault(wmd => wmd.DamageType == WeaponDamageTypes.None);
            return dammsg;
        }
     
        static DamageMessageText()
        {
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.None, 0, "miss", "misses"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.None, 1, "barely scratch", "barely scratches"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.None, 2, "scratch", "scratches"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.None, 4, "graze", "grazes"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.None, 7, "hit", "hits"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.None, 11, "injure", "injures"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.None, 15, "wound", "wounds"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.None, 20, "maul", "mauls"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.None, 25, "decimate", "decimates"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.None, 30, "devastate", "devastates"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.None, 37, "maim", "maims"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.None, 45, "MUTILATE", "MUTILATES"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.None, 55, "EVISCERATE", "EVISCERATES"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.None, 65, "DISMEMBER", "DISMEMBERS"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.None, 85, "MASSACRE", "MASSACRES"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.None, 100, "MANGLE", "MANGLES"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.None, 135, "*** DEMOLISH ***", "*** DEMOLISHES ***"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.None, 160, "*** DEVASTATE ***", "*** DEVASTATES ***"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.None, 250, "=== OBLITERATE ===", "=== OBLITERATES ==="));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.None, 330, ">>> ANNIHILATE <<<", ">>> ANNIHILATES <<<"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.None, 380, "<<< ERADICATE >>>", "<<< ERADICATES >>>"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.None, float.MaxValue, "do UNSPEAKABLE things to", "does UNSPEAKABLE things to"));

            // Adding messages for WeaponDamageTypes.Lightning
            // Lightning Damage Messages
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.Lightning, 0, "spark", "sparks"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.Lightning, 1, "tingle", "tingles"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.Lightning, 2, "zap", "zaps"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.Lightning, 4, "jolt", "jolts"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.Lightning, 7, "shock", "shocks"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.Lightning, 11, "electrify", "electrifies"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.Lightning, 15, "galvanize", "galvanizes"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.Lightning, 20, "discharge", "discharges"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.Lightning, 25, "arc", "arcs"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.Lightning, 30, "surge", "surges"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.Lightning, 37, "ionize", "ionizes"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.Lightning, 45, "electrocute", "electrocutes"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.Lightning, 55, "thunder strike", "thunder strikes"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.Lightning, 65, "overload", "overloads"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.Lightning, 85, "blast", "blasts"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.Lightning, 100, "VOLTAIC BURST", "VOLTAIC BURSTS"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.Lightning, 135, "*** PLASMA STORM ***", "*** PLASMA STORMS ***"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.Lightning, 160, "*** FULMINATE ***", "*** FULMINATES ***"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.Lightning, 250, "=== THUNDEROUS WRATH ===", "=== THUNDEROUS WRATHS ==="));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.Lightning, 330, ">>> IONIC CATACLYSM <<<", ">>> IONIC CATACLYSMS <<<"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.Lightning, 380, "<<< ELECTROMAGNETIC ANNIHILATION >>>", "<<< ELECTROMAGNETIC ANNIHILATES >>>"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.Lightning, float.MaxValue, "do UNSPEAKABLE things to", "does UNSPEAKABLE things to"));

            // Cold Damage Messages
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.Cold, 0, "chill", "chills"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.Cold, 1, "numb", "numbs"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.Cold, 2, "frost", "frosts"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.Cold, 4, "freeze", "freezes"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.Cold, 7, "glaciate", "glaciates"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.Cold, 11, "crystallize", "crystallizes"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.Cold, 15, "deep freeze", "deep freezes"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.Cold, 20, "flash freeze", "flash freezes"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.Cold, 25, "encase", "encases"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.Cold, 30, "hibernate", "hibernates"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.Cold, 37, "petrify", "petrifies"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.Cold, 45, "shatter", "shatters"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.Cold, 55, "frostbite", "frostbites"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.Cold, 65, "hypothermia", "hypothermias"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.Cold, 85, "avalanche", "avalanches"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.Cold, 100, "arctic blast", "arctic blasts"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.Cold, 135, "*** GLACIAL EPOCH ***", "*** GLACIAL EPOCHS ***"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.Cold, 160, "*** PERMAFROST ***", "*** PERMAFROSTS ***"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.Cold, 250, "=== ABSOLUTE ZERO ===", "=== ABSOLUTE ZEROS ==="));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.Cold, 330, ">>> CRYOGENIC OBLIVION <<<", ">>> CRYOGENIC OBLIVIONS <<<"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.Cold, 380, "<<< ICE AGE APOCALYPSE >>>", "<<< ICE AGE APOCALYPSES >>>"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.Cold, float.MaxValue, "do UNSPEAKABLE things to", "does UNSPEAKABLE things to"));

            // Fire Damage Messages
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.Fire, 0, "warm", "warms"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.Fire, 1, "singe", "singes"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.Fire, 2, "spark", "sparks"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.Fire, 4, "ignite", "ignites"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.Fire, 7, "kindle", "kindles"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.Fire, 11, "scorch", "scorches"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.Fire, 15, "sear", "sears"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.Fire, 20, "char", "chars"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.Fire, 25, "cauterize", "cauterizes"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.Fire, 30, "incinerate", "incinerates"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.Fire, 37, "immolate", "immolates"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.Fire, 45, "blaze", "blazes"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.Fire, 55, "torch", "torches"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.Fire, 65, "inferno", "infernos"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.Fire, 85, "conflagerate", "conflagerates"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.Fire, 100, "pyroclasm", "pyroclasms"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.Fire, 135, "*** FIRESTORM ***", "*** FIRESTORMS ***"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.Fire, 160, "*** HELLFIRE ***", "*** HELLFIRES ***"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.Fire, 250, "=== SOLAR FLARE ===", "=== SOLAR FLARES ==="));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.Fire, 330, ">>> SUPERNOVA <<<", ">>> SUPERNOVAS <<<"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.Fire, 380, "<<< COSMIC INCINERATION >>>", "<<< COSMIC INCINERATIONS >>>"));
            DamageMessageTexts.Add(new DamageMessageText(WeaponDamageTypes.Fire, float.MaxValue, "do UNSPEAKABLE things to", "does UNSPEAKABLE things to"));

        }
        public DamageMessageText(WeaponDamageTypes type, float damageLessThanOrEqualTo, string message, string PresentSimpleMessage)
        {
            this.DamageType = type;
            this.LessThanOrEqualTo = damageLessThanOrEqualTo;
            this.Message = message;
            this.PresentSimpleMessage = PresentSimpleMessage;
        }

        public WeaponDamageTypes DamageType { get; }
        public float LessThanOrEqualTo { get; }
        public string Message { get; }
        public string PresentSimpleMessage { get; }

        public string ToString(Character to, bool PresentSimpleMessage)
        {
            var messagefrom = this;
            if(!to.Flags.ISSET(ActFlags.DamageOnType) && DamageType != WeaponDamageTypes.None)
            {
                messagefrom = GetWeaponDamageMessageText(WeaponDamageTypes.None, this.LessThanOrEqualTo) ?? this;
            }
            return PresentSimpleMessage? messagefrom.PresentSimpleMessage : messagefrom.Message;
        }
    }
}