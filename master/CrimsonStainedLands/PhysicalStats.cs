using CrimsonStainedLands.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CrimsonStainedLands
{
    public enum PhysicalStatTypes
    {
        Strength = 0,
        Wisdom = 1,
        Intelligence = 2,
        Dexterity = 3,
        Constitution = 4,
        Charisma = 5
    }

    public class PhysicalStats : IEnumerable<int>
    {
        public class StrengthApplyType
        {
            public int ToHit;
            public int ToDam;
            public int Carry;
            public int Wield;

            public StrengthApplyType(int tohit, int todam, int carry, int wield)
            {
                this.ToHit = tohit;
                this.ToDam = todam;
                this.Carry = carry;
                this.Wield = wield;
            }
        }

        public class IntelligenceApplyType
        {
            public int Learn;
            public IntelligenceApplyType(int learn)
            { this.Learn = learn; }
        };

        public class WisdomApplyType
        {
            public int Practice;

            public WisdomApplyType(int practice)
            {
                this.Practice = practice;
            }
        };

        public class DexterityApplyType
        {
            public int Defensive;
            public int Carry;
            public int ToHit;
            public int Armor;

            public DexterityApplyType(int defensive, int carry, int tohit, int armor)
            {
                this.Defensive = defensive;
                this.Carry = carry;
                this.ToHit = tohit;
                this.Armor = armor;

            }
        };

        public class ConstitutionApplyType
        {
            public int Hitpoints;
            public int Shock;

            public ConstitutionApplyType(int Hitpoints, int shock)
            {
                this.Hitpoints = Hitpoints;
                this.Shock = shock;
            }
        }


        public static StrengthApplyType[] StrengthApply =
        {
            new StrengthApplyType ( -5, -4,   0,  0 ),  /* 0  */
	        new StrengthApplyType ( -5, -4,   3,  1 ),  /* 1  */
	        new StrengthApplyType ( -3, -2,   3,  2 ),
            new StrengthApplyType ( -3, -1,  10,  3 ),  /* 3  */
	        new StrengthApplyType ( -2, -1,  25,  4 ),
            new StrengthApplyType ( -2, -1,  55,  5 ),  /* 5  */
	        new StrengthApplyType ( -1,  0,  80,  6 ),
            new StrengthApplyType ( -1,  0, 100,  7 ),
            new StrengthApplyType (  0,  0, 150,  8 ),
            new StrengthApplyType (  0,  0, 180,  9 ),
            new StrengthApplyType (  0,  0, 200, 10 ), /* 10  */
	        new StrengthApplyType (  0,  0, 215, 11 ),
            new StrengthApplyType (  0,  0, 230, 12 ),
            new StrengthApplyType (  0,  0, 230, 13 ), /* 13  */
	        new StrengthApplyType (  0,  1, 240, 14 ),
            new StrengthApplyType (  1,  1, 250, 15 ), /* 15  */
	        new StrengthApplyType (  1,  2, 265, 16 ),
            new StrengthApplyType (  2,  3, 280, 22 ),
            new StrengthApplyType (  2,  3, 300, 25 ), /* 18  */
	        new StrengthApplyType (  3,  4, 325, 30 ),
            new StrengthApplyType (  3,  5, 350, 35 ), /* 20  */
	        new StrengthApplyType (  4,  6, 400, 40 ),
            new StrengthApplyType (  4,  6, 450, 45 ),
            new StrengthApplyType (  5,  7, 500, 50 ),
            new StrengthApplyType (  5,  8, 550, 55 ),
            new StrengthApplyType (  6,  9, 600, 60 )  /* 25   */
        };

        public static IntelligenceApplyType[] IntelligenceApply =
        {
            new IntelligenceApplyType (  3 ),	/*  0 */
	        new IntelligenceApplyType (  5 ),	/*  1 */
	        new IntelligenceApplyType (  7 ),
            new IntelligenceApplyType (  8 ),	/*  3 */
	        new IntelligenceApplyType (  9 ),
            new IntelligenceApplyType ( 10 ),	/*  5 */
	        new IntelligenceApplyType ( 11 ),
            new IntelligenceApplyType ( 12 ),
            new IntelligenceApplyType ( 13 ),
            new IntelligenceApplyType ( 15 ),
            new IntelligenceApplyType ( 17 ),	/* 10 */
	        new IntelligenceApplyType ( 19 ),
            new IntelligenceApplyType ( 22 ),
            new IntelligenceApplyType ( 25 ),
            new IntelligenceApplyType ( 28 ),
            new IntelligenceApplyType ( 31 ),	/* 15 */
	        new IntelligenceApplyType ( 34 ),
            new IntelligenceApplyType ( 37 ),
            new IntelligenceApplyType ( 40 ),	/* 18 */
	        new IntelligenceApplyType ( 44 ),
            new IntelligenceApplyType ( 49 ),	/* 20 */
	        new IntelligenceApplyType ( 55 ),
            new IntelligenceApplyType ( 60 ),
            new IntelligenceApplyType ( 70 ),
            new IntelligenceApplyType ( 80 ),
            new IntelligenceApplyType ( 85 )	/* 25 */
        };



        public static WisdomApplyType[] WisdomApply =
        {
            new WisdomApplyType( 0 ),	/*  0 */
	        new WisdomApplyType( 0 ),	/*  1 */
	        new WisdomApplyType( 0 ),
            new WisdomApplyType( 0 ),	/*  3 */
	        new WisdomApplyType( 0 ),
            new WisdomApplyType( 1 ),	/*  5 */
	        new WisdomApplyType( 1 ),
            new WisdomApplyType( 1 ),
            new WisdomApplyType( 1 ),
            new WisdomApplyType( 1 ),
            new WisdomApplyType( 1 ),	/* 10 */
	        new WisdomApplyType( 1 ),
            new WisdomApplyType( 1 ),
            new WisdomApplyType( 1 ),
            new WisdomApplyType( 1 ),
            new WisdomApplyType( 2 ),	/* 15 */
	        new WisdomApplyType( 2 ),
            new WisdomApplyType( 2 ),
            new WisdomApplyType( 3 ),	/* 18 */
	        new WisdomApplyType( 3 ),
            new WisdomApplyType( 3 ),	/* 20 */
	        new WisdomApplyType( 3 ),
            new WisdomApplyType( 4 ),
            new WisdomApplyType( 4 ),
            new WisdomApplyType( 4 ),
            new WisdomApplyType( 5 )	/* 25 */
        };



        public static DexterityApplyType[] DexterityApply =
        {
            new DexterityApplyType(   60,0, 0, 0 ),   /* 0 */
	        new DexterityApplyType(   50,0, 0, 0 ),   /* 1 */
	        new DexterityApplyType(   50,0, 0, 0 ),
            new DexterityApplyType(   40,0, 0, 0 ),
            new DexterityApplyType(   30,0, 0, 0 ),
            new DexterityApplyType(   20,0, 0, 0 ),   /* 5 */
	        new DexterityApplyType(   10,0, 0, 0 ),
            new DexterityApplyType(    0,0, 0, 0 ),
            new DexterityApplyType(    0,0, 0, 0 ),
            new DexterityApplyType(    0,0, 0, 0 ),
            new DexterityApplyType(    0,0, 0, 0 ),   /* 10 */
	        new DexterityApplyType(    0,1, 0, 0 ),
            new DexterityApplyType(    0,1, 0, 0 ),
            new DexterityApplyType(    0,1, 0, 0 ),
            new DexterityApplyType(    0,2, 0, 0 ),
            new DexterityApplyType( - 10,2, 0, 0 ),   /* 15 */
	        new DexterityApplyType( - 15,2, 0, 0 ),
            new DexterityApplyType( - 20,3, 0, 0 ),
            new DexterityApplyType( - 30,3, 0, 0 ),
            new DexterityApplyType( - 40,4, 0, 0 ),
            new DexterityApplyType( - 50,4, 0, 0 ),   /* 20 */
	        new DexterityApplyType( - 60,5, 0, 0 ),
            new DexterityApplyType( - 75,6, 0, 0 ),
            new DexterityApplyType( - 90,7, 0, 0 ),
            new DexterityApplyType( -105,8, 0, 0 ),
            new DexterityApplyType( -120,9, 0, 0 )    /* 25 */
        };


        public static ConstitutionApplyType[] ConstitutionApply =
        {
            new ConstitutionApplyType ( -4, 20 ),   /*  0 */
	        new ConstitutionApplyType ( -3, 25 ),   /*  1 */
	        new ConstitutionApplyType ( -2, 30 ),
            new ConstitutionApplyType ( -2, 35 ),	  /*  3 */
	        new ConstitutionApplyType ( -1, 40 ),
            new ConstitutionApplyType ( -1, 45 ),   /*  5 */
	        new ConstitutionApplyType ( -1, 50 ),
            new ConstitutionApplyType (  0, 55 ),
            new ConstitutionApplyType (  0, 60 ),
            new ConstitutionApplyType (  0, 65 ),
            new ConstitutionApplyType (  0, 70 ),   /* 10 */
	        new ConstitutionApplyType (  0, 75 ),
            new ConstitutionApplyType (  0, 80 ),
            new ConstitutionApplyType (  0, 85 ),
            new ConstitutionApplyType (  0, 88 ),
            new ConstitutionApplyType (  1, 90 ),   /* 15 */
	        new ConstitutionApplyType (  2, 95 ),
            new ConstitutionApplyType (  2, 97 ),
            new ConstitutionApplyType (  3, 99 ),   /* 18 */
	        new ConstitutionApplyType (  3, 99 ),
            new ConstitutionApplyType (  4, 99 ),   /* 20 */
	        new ConstitutionApplyType (  4, 99 ),
            new ConstitutionApplyType (  5, 99 ),
            new ConstitutionApplyType (  6, 99 ),
            new ConstitutionApplyType (  7, 99 ),
            new ConstitutionApplyType (  8, 99 )    /* 25 */
        };


        public int Strength;
        public int Wisdom;
        public int Intelligence;
        public int Dexterity;
        public int Constitution;
        public int Charisma;
        public int this[PhysicalStatTypes index]
        {
            get
            {
                switch (index)
                {
                    case PhysicalStatTypes.Strength: return Strength;
                    case PhysicalStatTypes.Wisdom: return Wisdom;
                    case PhysicalStatTypes.Intelligence: return Intelligence;
                    case PhysicalStatTypes.Dexterity: return Dexterity;
                    case PhysicalStatTypes.Constitution: return Constitution;
                    case PhysicalStatTypes.Charisma: return Charisma;
                    default: throw new IndexOutOfRangeException();
                }
            }
            set
            {
                switch (index)
                {
                    case PhysicalStatTypes.Strength: Strength = value; break;
                    case PhysicalStatTypes.Wisdom: Wisdom = value; break;
                    case PhysicalStatTypes.Intelligence: Intelligence = value; break;
                    case PhysicalStatTypes.Dexterity: Dexterity = value; break;
                    case PhysicalStatTypes.Constitution: Constitution = value; break;
                    case PhysicalStatTypes.Charisma: Charisma = value; break;
                    default: throw new IndexOutOfRangeException();
                }
            }
        }

        public PhysicalStats(int Strength, int Wisdom, int Intelligence, int Dexterity, int Constitution, int Charisma)
        {
            this.Strength = Strength;
            this.Wisdom = Wisdom;
            this.Intelligence = Intelligence;
            this.Dexterity = Dexterity;
            this.Constitution = Constitution;
            this.Charisma = Charisma;
        }

        public PhysicalStats()
        {

        }

        public PhysicalStats(PhysicalStats other)
        {
            this.Strength = other.Strength;
            this.Wisdom = other.Wisdom;
            this.Intelligence = other.Intelligence;
            this.Dexterity = other.Dexterity;
            this.Constitution = other.Constitution;
            this.Charisma = other.Charisma;
        }

        public PhysicalStats(XElement element)
        {
            this.Strength = element.GetElementValueInt("Strength");
            this.Wisdom = element.GetElementValueInt("Wisdom");
            this.Intelligence = element.GetElementValueInt("Intelligence");
            this.Dexterity = element.GetElementValueInt("Dexterity");
            this.Constitution = element.GetElementValueInt("Constitution");
            this.Charisma = element.GetElementValueInt("Charisma");
        }

        public XElement Element(string Name)
        {
            return new XElement(Name,
                new XElement("Strength", Strength),
                new XElement("Wisdom", Wisdom),
                new XElement("Intelligence", Intelligence),
                new XElement("Dexterity", Dexterity),
                new XElement("Constitution", Constitution),
                new XElement("Charisma", Charisma));
        }

        public override string ToString()
        {
            return "Strength: " + Strength +
                ", Wisdom: " + Wisdom +
                ", Intelligence: " + Intelligence +
                ", Dexterity: " + Dexterity
                + ", Constitution: " + Constitution +
                ", Charisma: " + Charisma;
        }
        public override bool Equals(object obj)
        {
            return this.ToString() == obj.ToString();
        }

        public override int GetHashCode()
        {
            return this.ToString().GetHashCode();
        }

        public bool HasValue()
        {
            return !(Strength == 0 && Wisdom == 0 && Intelligence == 0 && Dexterity == 0 && Constitution == 0 && Charisma == 0);
        }
        public IEnumerator<int> GetEnumerator()
        {
            return new PhysicalStatEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new PhysicalStatEnumerator(this);
        }

        public int Count => 6;
        public int Length => 6;

        public class PhysicalStatEnumerator : IEnumerator<int>
        {
            private int _index;
            private PhysicalStats stats;

            public PhysicalStatEnumerator(PhysicalStats stats)
            {
                this.stats = stats;
            }
            private int GetCurrent()
            {
                switch (_index)
                {
                    case 0: return stats.Strength;
                    case 1: return stats.Wisdom;
                    case 2: return stats.Intelligence;
                    case 3: return stats.Dexterity;
                    case 4: return stats.Constitution;
                    case 5: return stats.Charisma;
                    default: throw new IndexOutOfRangeException();
                }
            }
            object IEnumerator.Current => (object)GetCurrent();

            int IEnumerator<int>.Current => GetCurrent();

            public void Dispose()
            {
                _index = -1;
            }

            public bool MoveNext()
            {
                if (_index < stats.Count)
                {
                    _index++;
                    return true;
                }
                else
                    return false;
            }

            public void Reset()
            {
                _index = 0;
            }
        }
    }
}