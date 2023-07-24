using CrimsonStainedLands.Extensions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrimsonStainedLands
{
    public class Dice: IComparable<Dice>
    {
        public int DiceSides;
        public int DiceCount;
        public int DiceBonus;

        public Dice(int Sides, int Count, int Bonus)
        {
            this.DiceSides = Sides;
            this.DiceCount = Count;
            this.DiceBonus = Bonus;
        }

        public Dice(Dice other)
        {
            this.DiceSides = other.DiceSides;
            this.DiceCount = other.DiceCount;
            this.DiceBonus = other.DiceBonus;
        }

        public bool HasValue { get { return DiceSides != 0 && DiceCount != 0; } }

        public int Roll()
        {
            //int sum = 0;

            ////switch (DiceCount)
            ////{
            ////    case 0: return 0;
            ////    case 1: return DiceSides;
            ////}

            //for (var idice = 0; idice < DiceCount; idice++)
            //    sum += utility.rand(1, DiceSides);
            if (DiceCount == 0)
                return DiceSides + DiceBonus;

            return Utility.Random(DiceSides, DiceSides * DiceCount) + DiceBonus;

            //return sum + DiceBonus;
        }

        public int Average => ( (DiceCount + DiceBonus) + (DiceCount * DiceSides + DiceBonus)) / 2;

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }
        public override bool Equals(object obj)
        {
            if(obj == null) return false;
            if(!(obj is Dice)) return false;
            Dice other = obj as Dice;
            return other.DiceSides == DiceSides && other.DiceCount == DiceCount && other.DiceBonus == DiceBonus;
        }

        public override string ToString()
        {
            return DiceSides + "d" + DiceCount + "+" + DiceBonus;
        }

        public int CompareTo(Dice other)
        {
            if(!(other is Dice)) return 1;
            if(other.Average<Average) return -1;
            if(other.Average==Average) return 0;
            else return 1;
        }
    }
}
