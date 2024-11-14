using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrimsonStainedLands
{
    internal static class BonusInfo
    {
        public static float LearningBonus => 2;
        public static float ExperienceBonus => 1;

        public static DateTime LearningBonusEnds => DateTime.Now.AddYears(1);
        public static DateTime ExperienceBonusEnds => DateTime.Now.AddYears(1);

        public static void DoBonus(Character player, string arguments)
        {
            player.send("The following bonus information is available:\r\n");
            if(ExperienceBonus > 1)
            {
                player.send("\\&FF8000Experience bonus: \\G{0}% - Ends {1}\\x\r\n", (ExperienceBonus - 1) * 100, ExperienceBonusEnds.ToString());
            }
            else
                player.send("\\&FF8000Experience bonus: \\Gnone\\x\r\n");

            if (LearningBonus > 1)
            {
                player.send("\\&FF8000Learning bonus: \\G{0}% - Ends {1}\\x\r\n", (LearningBonus - 1) * 100, LearningBonusEnds.ToString());
            }
            else
                player.send("\\&FF8000Learning bonus: \\Gnone\\x\r\n");
            player.send("\r\n");
        }
    }
}
