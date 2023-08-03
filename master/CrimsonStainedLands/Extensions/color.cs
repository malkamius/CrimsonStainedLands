using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CrimsonStainedLands.Extensions
{
    public static class color
    {
        public const string Clear = "\x01B[0m";
        public const string Red = "[0;31m";
        public const string Green = "[0;32m";
        public const string Yellow = "[0;33m";
        public const string Blue = "[0;34m";
        public const string Magenta = "[0;35m";
        public const string Cyan = "[0;36m";
        public const string White = "[0;37m";
        public const string DarkGrey = "[1;30m";
        public const string RedBold = "[1;31m";
        public const string GreenBold = "[1;32m";
        public const string YellowBold = "[1;33m";
        public const string BlueBold = "[1;34m";
        public const string MagentaBold = "[1;35m";
        public const string CyanBold = "[1;36m";
        public const string WhiteBold = "[1;37m";
        public const string Underline = "\x01B[4m";
        public const string Reverse = "\x01B[7m";
        public const string Flash = "\x01B[5m";

        public static string colorString(this string text, Character ch = null)
        {
            string newString = "";
            bool colorOn = true;
            if (ch != null && !ch.Flags.Contains(ActFlags.Color))
                colorOn = false;

            var @base = 30;
            for (int iCh = text.IndexOf(@"\"); iCh > -1; iCh = text.IndexOf(@"\"))
            {
                if (iCh > 0)
                {
                    newString = newString + text.Substring(0, iCh);
                }
                if (text.Length > (iCh + 1))
                {
                    int Bold;
                    var color = 0;

                    char cCode = text[iCh + 1];
                    if (cCode == char.ToUpper(cCode))
                    {
                        Bold = 1;
                    }
                    else
                    {
                        Bold = 0;
                    }
                    if (cCode == '!' && colorOn)
                    {
                        newString = newString + Reverse;
                    }
                    else if ((cCode == '*' || cCode == 'f') && colorOn)
                    {
                        newString = newString + Flash;
                    }
                    else if (cCode == '#')
                    {
                        //newString = newString + "\x001b[1;41m";
                        @base = 40;
                    }
                    else if (cCode == '@' && colorOn)
                    {
                        newString = newString + Underline;
                    }

                    switch (((char)(char.ToLower(cCode) - '\\')))
                    {
                        case '\0':
                            newString = newString + @"\";
                            color = 0;
                            break;

                        case '\x0006':
                            //newString = newString + "\x001b[" + Bold + ";34m";
                            color = 4;
                            break;

                        case '\a':
                            //newString = newString + "\x001b[" + Bold + ";36m";
                            color = 6;
                            break;

                        case '\v':
                            //newString = newString + "\x001b[" + Bold + ";32m";
                            color = 2;
                            break;

                        case '\x0011':
                            //newString = newString + "\x001b[" + Bold + ";35m";
                            color = 5;
                            break;

                        case '\x0012':
                            newString = newString + "\r\n\r";
                            color = 0;
                            break;
                        case '\x0018':
                            newString = newString + "\t";
                            color = 0;
                            break;
                        case '\x0016':
                            //newString = newString + "\x001b[" + Bold + ";31m";
                            color = 1;
                            break;

                        case '\x001b':
                            //newString = newString + "\x001b[" + Bold + ";37m";
                            color = 7;
                            break;

                        case '\x001c':
                            newString = newString + "\x001b[0m";
                            color = 0;
                            break;

                        case '\x001d':
                            //newString = newString + "\x001b[" + Bold + ";33m";
                            color = 3;
                            break;
                    }

                    if (color != 0 && colorOn)
                    {
                        newString = newString + "\x001b[" + Bold + string.Format(";{0:00}m", @base + color);
                        color = 0;
                        @base = 30;
                    }
                }
                if (text.Length > (iCh + 2))
                {
                    text = text.Substring(iCh + 2);
                }
                else
                {
                    text = "";
                }
            }
            return (newString + text);
        }

        public static string escapeColor(this string text)
        {
            return text.Replace(@"\", @"\\");
        }

    }
}
