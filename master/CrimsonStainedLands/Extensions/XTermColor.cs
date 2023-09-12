using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CrimsonStainedLands.Extensions
{
    public static class XTermColor
    {
        public const string Clear = "\x01B[0m";

        public const string Underline = "\x01B[4m";
        public const string Reverse = "\x01B[7m";
        public const string Flash = "\x01B[5m";

        public static int NextColorMarkerIndex(this string text, int startIndex, out char escapeChar)
        {
            var slashIndex = text.IndexOf('\\', startIndex);
            var curlyIndex = text.IndexOf('{', startIndex);

            if (curlyIndex >= 0 && (slashIndex == -1 || slashIndex > curlyIndex))
            {
                escapeChar = '{';
                return curlyIndex;
            }
            else if (slashIndex >= 0)
            {
                escapeChar = '\\';
                return slashIndex;
            }
            else
            {
                escapeChar = '\0';
                return -1;
            }
        }

        public static string ColorStringRGBColor(this string text, bool StripColor = false, bool Support256 = false, bool SupportRGB = false, bool MXP = false)
        {
            StringBuilder ResultBuilder = new StringBuilder();
            char EscapeChar = '\0';
            int LastIndex = 0;
            int ColorCodeOffset = 0;
            var @base = 38;
            if (MXP)
                ResultBuilder.Append("\x01B[7z");
            for (int ColorMarkerIndex = text.NextColorMarkerIndex(LastIndex, out EscapeChar); ColorMarkerIndex > -1; ColorMarkerIndex = text.NextColorMarkerIndex(LastIndex, out EscapeChar))
            {
                ColorCodeOffset = 0;
                if (ColorMarkerIndex > 0)
                {
                    if (LastIndex < ColorMarkerIndex)
                        ResultBuilder.Append(text.Substring(LastIndex, ColorMarkerIndex - LastIndex));
                }

                if (text.Length > (ColorMarkerIndex + 1))
                {
                    bool Bold;
                    var color = -1;

                    char ColorCodeCharacter = text[ColorMarkerIndex + 1];
                    if (ColorCodeCharacter == char.ToUpper(ColorCodeCharacter))
                    {
                        Bold = true;
                    }
                    else
                    {
                        Bold = false;
                    }
                    if (ColorCodeCharacter == '!' && !StripColor)
                    {
                        ResultBuilder.Append(Reverse);
                    }
                    else if ((ColorCodeCharacter == '*' || ColorCodeCharacter == 'f') && !StripColor)
                    {
                        ResultBuilder.Append(Flash);
                    }
                    else if (ColorCodeCharacter == '#')
                    {
                        @base = 48;
                    }
                    else if (ColorCodeCharacter == '@' && !StripColor)
                    {
                        ResultBuilder.Append(Underline);
                    }

                    switch ((char.ToLower(ColorCodeCharacter)))
                    {
                        case '\\':
                            ResultBuilder.Append(@"\");
                            color = -1;
                            break;
                        case '{':
                            ResultBuilder.Append(@"{");
                            color = -1;
                            break;
                        case 'n':
                            ResultBuilder.Append("\n");
                            color = -1;
                            break;
                        case 't':
                            ResultBuilder.Append("\t");
                            color = -1;
                            break;
                        case 'd':
                            color = 0;
                            break;
                        case 'r':
                            color = 1;
                            break;
                        case 'g':
                            color = 2;
                            break;
                        case 'y':
                            color = 3;
                            break;
                        case 'b':
                            color = 4;
                            break;
                        case 'm':
                            color = 5;
                            break;
                        case 'c':
                            color = 6;
                            break;
                        case 'w':
                            color = 7;
                            break;

                        case 'x':
                            ResultBuilder.Append("\x001b[0m");
                            ResultBuilder.Append("\u001b[4z\x01B[3z\x01B[7z");
                            color = -1;
                            break;
                        case 'e':
                            {
                                string number = "";
                                bool ended = false;
                                while (text.Length > ColorMarkerIndex + number.Length + 2 &&
                                    (((ColorCodeCharacter = text[ColorMarkerIndex + number.Length + 2]) >= '0' && ColorCodeCharacter <= '9')
                                    || ColorCodeCharacter == ';') &&
                                    number.Length < 3)
                                {
                                    ColorCodeOffset++;
                                    if (ColorCodeCharacter == ';')
                                    {
                                        ended = true;
                                        break;
                                    }
                                    number = number + ColorCodeCharacter;

                                }
                                int.TryParse(number, out color);
                                if (!ended && text.Length > ColorMarkerIndex + number.Length + 2 &&
                                   ((ColorCodeCharacter = text[ColorMarkerIndex + number.Length + 2]) == ';') &&
                                   number.Length <= 3)
                                    ColorCodeOffset++;
                                break;
                            }
                        case '&':
                            {
                                string number = "";
                                bool ended = false;
                                while (text.Length > ColorMarkerIndex + number.Length + 2 &&
                                    (((ColorCodeCharacter = text[ColorMarkerIndex + number.Length + 2]) >= '0' && ColorCodeCharacter <= '9')
                                    || (ColorCodeCharacter >= 'A' && ColorCodeCharacter <= 'F')
                                    || ColorCodeCharacter == ';') &&
                                    number.Length < 6)
                                {
                                    ColorCodeOffset++;
                                    if (ColorCodeCharacter == ';')
                                    {
                                        ended = true;
                                        break;
                                    }
                                    number = number + ColorCodeCharacter;

                                }
                                int.TryParse(number, System.Globalization.NumberStyles.HexNumber, CultureInfo.InvariantCulture, out color);
                                if (SupportRGB)
                                {
                                    ResultBuilder.Append(string.Format("\x001b[{0};2;{1:00};{2:00};{3:00}m", @base, color >> 16 & 0xFF, color >> 8 & 0xFF, color & 0xFF));
                                }
                                color = -1;
                                if (!ended && text.Length > ColorMarkerIndex + number.Length + 2 &&
                                   ((ColorCodeCharacter = text[ColorMarkerIndex + number.Length + 2]) == ';') &&
                                   number.Length <= 6)
                                    ColorCodeOffset++;
                                break;
                            }
                        //case '<':
                        //    if (MXP)
                        //    {
                        //        ResultBuilder.Append("\x1b[4z<");
                        //        var regex = new Regex(@"(<([a-zA-Z].*?)\b[^>]*>)(.*?)^(<\/\1>)");
                        //    }
                        //    else
                        //    {

                        //        //    var index = text.IndexOf('>', ColorMarkerIndex);
                        //        //    if (index > -1)
                        //        //        ColorCodeOffset = index - ColorMarkerIndex;
                        //        //                                ResultBuilder.Append("\x01B[7z");
                        //        var regex = new Regex(@"<([a-zA-Z].*?)\b[^>]*>(.*?)\\?<\/\1>");
                        //        var match = regex.Match(text, ColorMarkerIndex);
                        //        ColorCodeOffset = match.Length;
                        //        ResultBuilder.Append(match.Groups[2].Value);
                        //    }
                        //    break;
                        default:
                            break;
                    }

                    if (color >= 0 && !StripColor)
                    {
                        if (Support256)
                        {
                            if (color < 8 && Bold)
                                color = 8 + color;
                            ResultBuilder.Append(string.Format("\x001b[{0};5;{1:00}m", @base, color));
                        }
                        else if (color < 8)
                        {
                            if (@base == 38) @base = 30;
                            if (@base == 48) @base = 40;
                            ResultBuilder.Append("\x001b[" + (Bold ? "1;" : "") + string.Format("{0:00}m", @base + color));
                        }
                        color = 0;
                        @base = 38;
                    }
                }
                if (text.Length > (ColorMarkerIndex + 2))
                {
                    LastIndex = ColorMarkerIndex + 2 + ColorCodeOffset;
                }
                else
                {
                    LastIndex = text.Length;
                }
            }
            if (LastIndex < text.Length)
                ResultBuilder.Append(text.Substring(LastIndex));

            return ResultBuilder.ToString();
        }

        public static string EscapeColor(this string text)
        {
            if (text == null) return "";
            //var regex = new Regex(@"<([a-zA-Z].*?)\b[^>]*>(.*?)<\/\1>");
            //Match match;
            //while ((match = regex.Match(text, 0)) != null && match.Length > 0)
            //{
            //    text = text.Substring(0, match.Index) + match.Groups[2].Value + (text.Length > match.Index + match.Length ? text.Substring(match.Index + match.Length) : "");
            //}

            return text.Replace(@"\", @"\\").Replace("{", "{{");
        }

        
    }
}
