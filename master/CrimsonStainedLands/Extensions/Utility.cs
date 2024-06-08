/***************************************************************************
*  Original Diku Mud copyright (C) 1990, 1991 by Sebastian Hammer,        *
*  Michael Seifert, Hans Henrik St{rfeldt, Tom Madsen, and Katja Nyboe.   *
*                                                                         *
*  Merc Diku Mud improvments copyright (C) 1992, 1993 by Michael          *
*  Chastain, Michael Quan, and Mitchell Tse.                              *
*                                                                         *
*  In order to use any part of this Merc Diku Mud, you must comply with   *
*  both the original Diku license in 'license.doc' as well the Merc       *
*  license in 'license.txt'.  In particular, you may not remove either of *
*  these copyright notices.                                               *
*                                                                         *
*  Thanks to abaddon for proof-reading our comm.c and pointing out bugs.  *
*  Any remaining bugs are, of course, our work, not his.  :)              *
*                                                                         *
*  Much time and thought has gone into this software and you are          *
*  benefitting.  We hope that you share your changes too.  What goes      *
*  around, comes around.                                                  *
***************************************************************************/

/***************************************************************************
*	ROM 2.4 is copyright 1993-1996 Russ Taylor			   *
*	ROM has been brought to you by the ROM consortium		   *
*	    Russ Taylor (rtaylor@pacinfo.com)				   *
*	    Gabrielle Taylor (gtaylor@pacinfo.com)			   *
*	    Brian Moore (rom@rom.efn.org)				   *
*	By using this code, you have agreed to follow the terms of the	   *
*	ROM license, in the file Tartarus/doc/rom.license                  *
***************************************************************************/

/***************************************************************************
*       Tartarus code is copyright (C) 1997-1998 by Daniel Graham          *
*	In using this code you agree to comply with the Tartarus license   *
*       found in the file /Tartarus/doc/tartarus.doc                       *
***************************************************************************/

using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using static CrimsonStainedLands.WizardNet;

namespace CrimsonStainedLands.Extensions
{
    public static class Utility
    {
        public static Random SystemRandomGenerator = new Random();

        public static bool CheckName(this string name, string nameList)
        {
            string temp_name = "";
            while (!string.IsNullOrEmpty(nameList))
            {
                nameList = OneArgument(nameList, ref temp_name, " ");
                if (StringPrefix(temp_name, name))
                {
                    return true;
                }
            }
            return false;
        }

        public static string WrapText(this string text, int width = 80, int firstlinelength = 80)
        {
            if (text.ISEMPTY()) return "";
            if (text.StartsWith(".") && text.Length > 1) return text.Substring(1);
            //return text;
            var newtext = new StringBuilder(text.Length);
            text = text.Replace('\n', ' ').Replace("\r", "").Replace('\t', ' ').Replace("  ", " ");
            //var splittext = text.Split(' ');
            //int length = 0;
            //foreach (var startword in splittext)
            //{
            //    var word = startword;
            //    if (length + word.Length > width)
            //    { 
            //        newtext.AppendLine();
            //        length = 0;
            //    }
            //    if (length > 0) word = " " + word;
            //    newtext.Append(word) ;
            //    length = length + word.Length;
            
            //}
            //return newtext.ToString();
            
            int index = text.IndexOf(' ');
            int lastindex = 0;
            var restwidth = width;
            //int endlineindex = -1;
            bool firstline = true;
            while(text.Length > 0)
            {
                if (firstline) width = firstlinelength;
                else width = restwidth;
                while(index > -1 && index <= width && index < text.Length)
                {
                    lastindex = index;
                    index = text.IndexOf(' ', index + 1);

                }

                if (index > 0 && index <= width)
                {
                    newtext.AppendLine(text.Substring(0, index).Trim());
                }
                else if(!text.ISEMPTY() && text.Length < width)
                {
                    newtext.AppendLine(text.Trim());
                    text = "";
                }
                else
                {
                    
                    newtext.AppendLine(text.Substring(0, lastindex).Trim());

                    if (text.Length > lastindex)
                        text = text.Substring(lastindex + 1);
                    else
                        text = "";
                    index = 0;
                    lastindex = 0;
                }
                //index = text.IndexOf(' ', index + 1);
            }
            if (!text.ISEMPTY() && text.Length < width)
            {
                newtext.AppendLine(text.Trim());
                text = "";
            }
            //newtext.AppendLine(text);
            return newtext.ToString().Trim();
            //while (index > -1 && endlineindex < text.Length)
            //{
            //    while (index >= 0 && index - lastindex <= width)
            //    {
            //        index = text.IndexOf(' ', index + 1);
            //        if (index > -1 && index - lastindex < width)
            //            endlineindex = index;
            //       // else if(index == -1)
            //       //     endlineindex = text.Length;
            //    }
            //    newtext.AppendLine(text.Substring(lastindex, endlineindex - lastindex).Trim());
            //    lastindex = endlineindex;
            //    if(index >= 0)
            //    index = endlineindex;
            //    else
            //    {
            //        newtext.AppendLine(text.Substring(endlineindex, text.Length - endlineindex).Trim());
            //    }
            //}
            //return newtext.ToString();
        }

        /// <summary>
        /// WrapText without removing existing formatting...
        /// </summary>
        /// <param name="text"></param>
        /// <param name="width"></param>
        /// <returns></returns>
        public static string FriendlyWrapText(this string text, int width = 80)
        {
            var newtext = new StringBuilder();
            var word = new StringBuilder();
            text = text.Replace("\r", "");
            text = text.Replace("\t", "   ");
            int linelength = 0;

            for(int i = 0; i < text.Length; i++)
            {
                if (linelength + word.Length > width)
                {
                    newtext.AppendLine();
                    newtext.Append(word.ToString());
                    linelength = word.Length;
                    word.Clear();
                }

                if (text[i] == '\n')
                {
                    linelength = 0;
                    newtext.Append(word.ToString());
                    word.Clear();
                    newtext.AppendLine();
                }
                else if (word.Length > width)
                {
                    newtext.AppendLine(word.ToString());
                    word.Clear();
                }
                else if (text[i] == ' ' || text[i] == '\t')
                {
                    newtext.Append(word.ToString());
                    newtext.Append(text[i]);
                    linelength += word.Length + 1;
                    word.Clear();
                }
                else
                {
                    word.Append(text[i]);
                }
            }
            newtext.Append(word.ToString());

            return newtext.ToString();
        }

        public static string TOSTRINGTRIM(this string args)
        {
            return (args ?? "").Trim();
        }

        public static string TOUPPERFIRST(this string args) => args.ISEMPTY() ? "" : (args.Length == 1 ? args.ToUpper() : args[0].ToString().ToUpper() + args.Substring(1));
        

        public static string ToDelimitedString<T>(this IEnumerable<T> list, string separator = " ")
        {
            return string.Join(separator, from single in list select single.ToString());
        }

        /// <summary>
        /// Return the first word or if quoted, multiple words
        /// Trims whitespace at the start of the string before returning an argument unless the whitespace is quoted
        /// </summary>
        /// <param name="ListOfArguments">A string which may contain multiple arguments</param>
        /// <param name="SingleArgument">Reference to a string to receive the first argument</param>
        /// <param name="Delimiter">Optionally specify a different delimiter</param>
        /// <returns>The remaining string after an argument is removed</returns>
        public static string OneArgumentOut(this string ListOfArguments, out string SingleArgument, string Delimiter = " ")
        {
            string oneargument = "";
            var returnvalue = OneArgument(ListOfArguments, ref oneargument, Delimiter);
            SingleArgument = oneargument;
            return returnvalue;
        }

        public static string OneArgument(this string ListOfArguments)
        {
            string onearg = "";
            return ListOfArguments.OneArgument(ref onearg);
        }

        /// <summary>
        /// Return the first word or if quoted, multiple words
        /// Trims whitespace at the start of the string before returning an argument unless the whitespace is quoted
        /// </summary>
        /// <param name="ListOfArguments">A string which may contain multiple arguments</param>
        /// <param name="SingleArgument">Reference to a string to receive the first argument</param>
        /// <param name="Delimiter">Optionally specify a different delimiter</param>
        /// <returns>The remaining string after an argument is removed</returns>
        public static string OneArgument(this string ListOfArguments, ref string SingleArgument, string Delimiter = " ")
        {
            if(ListOfArguments.ISEMPTY())
            {
                ListOfArguments = "";
                SingleArgument = "";
                return "";
            }
            ListOfArguments = ListOfArguments.TrimStart('\0', ' ', '\n', '\r', '\t');
            if (ListOfArguments.Length > 0)
            {
                if (ListOfArguments[0] == '\'' || ListOfArguments[0] == '"')
                {
                    //if (args.IndexOf(args[0], 1) >= 0)
                    //{
                        Delimiter = ListOfArguments[0].ToString();
                    //}
                    ListOfArguments = ListOfArguments.Substring(1);
                }
                int delimiterIndex = ListOfArguments.IndexOf(Delimiter);
                if (delimiterIndex >= 1)
                {
                    SingleArgument = ListOfArguments.Substring(0, delimiterIndex);
                    if (delimiterIndex < ListOfArguments.Length)
                    {
                        ListOfArguments = ListOfArguments.Substring(delimiterIndex + 1);
                        return ListOfArguments;
                    }
                    ListOfArguments = "";
                    return ListOfArguments;
                }
                SingleArgument = ListOfArguments;
                ListOfArguments = "";
                return ListOfArguments;
            }
            ListOfArguments = "";
            SingleArgument = "";
            return ListOfArguments;
        }

        public static int Percent(this long value, long max)
        {
            if (max == 0L)
            {
                return 0;
            }
            return (int)Math.Round(Math.Round((double)((((double)value) / ((double)max)) * 100.0), 0));
        }

        public static int Percent(this int value, int max)
        {
            if (max == 0L)
            {
                return 0;
            }
            return (int)Math.Round(Math.Round((double)((((double)value) / ((double)max)) * 100.0), 0));
        }

        
        [ThreadStatic]
        private static Random randomSeedGenerator;

        private static void initializeRandom()
        {
            if (randomSeedGenerator == null)
            {
                var cryptoResult = new byte[4];
                using (var seedProvider = new RNGCryptoServiceProvider())
                    seedProvider.GetBytes(cryptoResult);

                int seed = BitConverter.ToInt32(cryptoResult, 0);

                randomSeedGenerator = new Random(seed);
            }
        }
        /// <seealso cref="http://stackoverflow.com/questions/1399039/best-way-to-seed-random-in-singleton"/>
        public static int Random(this int inclusiveLowerBound, int inclusiveUpperBound)
        {
            initializeRandom();
            if(inclusiveLowerBound > inclusiveUpperBound)
            {
                var upper = inclusiveUpperBound;
                inclusiveUpperBound = inclusiveLowerBound;
                inclusiveLowerBound = upper;
            }

            if (inclusiveLowerBound == inclusiveUpperBound)
                return inclusiveLowerBound;
            // upper bound of Random.Next is exclusive
            int exclusiveUpperBound = inclusiveUpperBound + 1;
            return randomSeedGenerator.Next(inclusiveLowerBound, exclusiveUpperBound);
        }
        

        public static float Random(this float inclusiveLowerBound, float inclusiveUpperBound)
        {
            initializeRandom();

            if (inclusiveLowerBound > inclusiveUpperBound)
            {
                var upper = inclusiveUpperBound;
                inclusiveUpperBound = inclusiveLowerBound;
                inclusiveLowerBound = upper;
            }

            double val = (SystemRandomGenerator.NextDouble() * (inclusiveUpperBound - inclusiveLowerBound) + inclusiveLowerBound);
            
            return (float) val;
        }

        public static T SelectRandom<T>(this IEnumerable<T> list)
        {
            initializeRandom();
            return list != null ? list.OrderBy(x => randomSeedGenerator.Next()).FirstOrDefault() : default(T);
        }

        /// <summary>
        /// Returns whether the wholestring starts with partstring ignoring case
        /// </summary>
        /// <param name="wholeString"></param>
        /// <param name="partString"></param>
        /// <param name="acceptEmptyStrings"></param>
        /// <returns></returns>
        public static bool StringPrefix(this string wholeString, string partString, bool acceptEmptyStrings = false)
        {
            if (!acceptEmptyStrings && (string.IsNullOrEmpty(wholeString) || string.IsNullOrEmpty(partString)))
                return false;
            if (wholeString.Length < partString.Length)
            {
                return false;
            }
            return ((wholeString.Length >= partString.Length) && wholeString.ToLower().StartsWith(partString.ToLower()));
        }


        public static bool StringCmp(this string firststring, string secondstring)
        {
            return equals(firststring, secondstring);
        }

        public static bool equals(this string firststring, string secondstring)
        {
            return firststring.ISEMPTY()? false : firststring.Equals(secondstring, StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool ISSET<T>(this List<T> list, T flag)
        {
            return list.Contains(flag);
        }

        public static bool ISSET<T>(this IEnumerable<T> list, T flag)
        {
            return list.Contains(flag);
        }

        public static bool ISSET<T>(this IEnumerable<T> list, params T[] flags) => list.Any(item => flags.Contains(item));

        /// <summary>
        /// Remove a flag from a list if it is set
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="flag"></param>
        /// <returns></returns>
        public static bool REMOVEFLAG<T>(this List<T> list, T flag)
        {
            if (list.Contains(flag))
            {
                list.Remove(flag);
                return true;
            }
            else
                return false;
        }

        public static bool SETBIT<T>(this List<T> list, T flag)
        {
            return ADDFLAG(list, flag);
        }

        public static void SETBITS<T>(this List<T> list, params T[] flags)
        {
            foreach (var flag in flags)
                ADDFLAG(list, flag);
        }

        public static void SETBITS<T>(this List<T> list, IEnumerable<T> flags)
        {
            foreach(var flag in flags)
                ADDFLAG(list, flag);
        }

        public static void ADDFLAGS<T>(this List<T> list, IEnumerable<T> flags)
        {
            foreach (var flag in flags)
                ADDFLAG(list, flag);
        }
        /// <summary>
        /// Add a flag to a list if it is not already set
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="flag"></param>
        /// <returns></returns>
        public static bool ADDFLAG<T>(this List<T> list, T flag)
        {
            if (!list.Contains(flag))
            {
                list.Add(flag);
                return true;
            }
            else
                return false;
        }

        public static bool REMOVEFLAG<T>(this HashSet<T> list, T flag)
        {
            if (list.Contains(flag))
            {
                list.Remove(flag);
                return true;
            }
            else
                return false;
        }

        public static bool SETBIT<T>(this HashSet<T> list, T flag)
        {
            return ADDFLAG(list, flag);
        }

        public static void SETBITS<T>(this HashSet<T> list, IEnumerable<T> flags)
        {
            foreach (var flag in flags)
                ADDFLAG(list, flag);
        }

        public static void ADDFLAGS<T>(this HashSet<T> list, IEnumerable<T> flags)
        {
            foreach (var flag in flags)
                ADDFLAG(list, flag);
        }
        /// <summary>
        /// Add a flag to a list if it is not already set
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="flag"></param>
        /// <returns></returns>
        public static bool ADDFLAG<T>(this HashSet<T> list, T flag)
        {
            //if (!list.Contains(flag))
            //{
                return list.Add(flag);
            //    return true;
            //}
            //else
            //    return false;
        }

        public static bool ISEMPTY(this string checkstring) => string.IsNullOrEmpty(checkstring);

        public static IEnumerable<T> GetEnumValues<T>()
        {
            return Enum.GetValues(typeof(T)).OfType<T>();
        }

        public static bool GetEnumValue<T>(string search, ref T value, T defaultValue = default)
        {
            var names = Enum.GetNames(typeof(T));
            var values = Enum.GetValues(typeof(T));

            if (search.StringCmp("none"))
                return false;

            for (int i = 0; i < names.Length; i++)
                if (names[i].ToLower() == search.ToLower() || names[i].Replace("_", "").ToLower() == search.Replace("_", "").ToLower())
                {
                    value = (T)values.GetValue(i);
                    return true;
                }

            if (!string.IsNullOrEmpty(search) && search.ToLower() != "none")
                Game.bug("EnumValue not found: {0} {1}", typeof(T).Name, search);
            value = defaultValue;
            return false;
        }

        public static bool GetEnumValues<T>(string search, ref List<T> value, bool clear = true, char delimiter = ' ')
        {
            if(value == null)
                value = new List<T>();
            if (search.ISEMPTY()) return false;
            var names = Enum.GetNames(typeof(T));
            var values = Enum.GetValues(typeof(T));
            var flags = search.Split(delimiter);
            
            

            if (clear) value.Clear();

            if (search.StringCmp("none"))
                return false;

            bool found = false;

            foreach (var flag in flags.Distinct())
            {
                found = false;
                for (int i = 0; i < names.Length; i++)
                    if (names[i].ToLower() == flag.ToLower() || names[i].Replace("_", "").ToLower() == flag.Replace("_", "").ToLower())
                    {
                        value.SETBIT((T)values.GetValue(i));
                        found = true;
                    }

                if (!found && !string.IsNullOrEmpty(flag))
                    Game.log("Flag " + flag + " not found. " + typeof(T).Name.ToString());
            }
            return found;
        }

        public static bool GetEnumValues<T>(string search, ref HashSet<T> value, bool clear = true, char delimiter = ' ')
        {
            if (value == null)
                value = new HashSet<T>();
            if (search.ISEMPTY()) return false;
            var names = Enum.GetNames(typeof(T));
            var values = Enum.GetValues(typeof(T));
            var flags = search.Split(delimiter);



            if (clear) value.Clear();

            if (search.StringCmp("none"))
                return false;

            bool found = false;

            foreach (var flag in flags.Distinct())
            {
                found = false;
                for (int i = 0; i < names.Length; i++)
                    if (names[i].ToLower() == flag.ToLower() || names[i].Replace("_", "").ToLower() == flag.Replace("_", "").ToLower())
                    {
                        value.SETBIT((T)values.GetValue(i));
                        found = true;
                    }

                if (!found && !string.IsNullOrEmpty(flag))
                    Game.log("Flag " + flag + " not found. " + typeof(T).Name.ToString());
            }
            return found;
        }

        public static bool GetEnumValueStrPrefixOut<T>(string search, out T value)
        {
            bool result;
            T outvalue = default(T);

            result = GetEnumValueStrPrefix(search, ref outvalue);
            value = outvalue;
            return result;
            
        }

        public static bool GetEnumValueStrPrefix<T>(string search, ref T value)
        {
            var names = Enum.GetNames(typeof(T));
            var values = Enum.GetValues(typeof(T));
            
            if (search.StringCmp("none"))
                return false;
            for (int i = 0; i < names.Length; i++)
                if (names[i].ToLower().StringPrefix(search) || names[i].ToLower().StringPrefix(search.Replace("_", "")))
                {
                    value = (T)values.GetValue(i);
                    return true;
                }

            return false;
        }

        public static bool HasElement(this XElement element, string subElementName)
        {
            subElementName = subElementName.ToLower();
            return element != null && (element.Element(subElementName) != null || (from newelement in element.Elements() where newelement.Name.ToString().ToLower() == subElementName select newelement).FirstOrDefault() != null);
        }

        public static XElement GetElement(this XElement element, string subElementName)
        {
            if (element != null && element.Element(subElementName) != null)
                return element.Element(subElementName);
            subElementName = subElementName.ToLower();
            return (from newelement in element.Elements() where newelement.Name.ToString().ToLower() == subElementName select newelement).FirstOrDefault();
        }

        public static string GetElementValue(this XElement element, string subElementName, string defaultValue = "")
        {
            if (element.HasElement(subElementName))
                return element.GetElement(subElementName).Value;
            else
                return defaultValue;
        }

        public static int GetElementValueInt(this XElement element, string subElementName, int defaultValue = 0)
        {
            int result = defaultValue;
            if (element.HasElement(subElementName))
            {
                if (int.TryParse(element.GetElement(subElementName).Value, out result))
                    return result;
            }

            return defaultValue;
        }

        public static float GetElementValueFloat(this XElement element, string subElementName, float defaultValue = 0)
        {
            float result = defaultValue;
            if (element.HasElement(subElementName))
            {
                if (float.TryParse(element.GetElement(subElementName).Value, out result))
                    return result;
            }

            return defaultValue;
        }

        public static long GetElementValueLong(this XElement element, string subElementName, long defaultValue = 0)
        {
            long result = defaultValue;
            if (element.HasElement(subElementName))
            {
                if (long.TryParse(element.GetElement(subElementName).Value, out result))
                    return result;
            }

            return defaultValue;
        }
        public static bool HasAttribute(this XElement element, string attributeName)
        {
            attributeName = attributeName.ToLower();
            return element != null && (element.Attribute(attributeName) != null || (from newAttribute in element.Attributes() where newAttribute.Name.ToString().ToLower() == attributeName select newAttribute).FirstOrDefault() != null);
        }

        public static XAttribute GetAttribute(this XElement element, string attributeName)
        {
            if (element != null && element.Attribute(attributeName) != null)
                return element.Attribute(attributeName);
            attributeName = attributeName.ToLower();
            return (from newAttribute in element.Attributes() where newAttribute.Name.ToString().ToLower() == attributeName select newAttribute).FirstOrDefault();
        }

        public static string GetAttributeValue(this XElement element, string attributeName, string defaultValue = "")
        {
            if (element.HasAttribute(attributeName))
            {
                return element.GetAttribute(attributeName).Value;
            }

            return defaultValue;
        }
        public static int GetAttributeValueInt(this XElement element, string attributeName, int defaultValue = 0)
        {
            int result = 0;
            if (element.HasAttribute(attributeName))
            {
                if (int.TryParse(element.GetAttribute(attributeName).Value, out result))
                    return result;
            }

            return defaultValue;
        }

        public static long GetAttributeValueLong(this XElement element, string attributeName, long defaultValue = 0)
        {
            long result = 0;
            if (element.HasAttribute(attributeName))
            {
                if (long.TryParse(element.GetAttribute(attributeName).Value, out result))
                    return result;
            }
            return defaultValue;
        }

        public static bool IsName(this string namelist, string thename, bool disallowPrefix = false)
        {
            string part = "";
            string originalString = "";
            string list = "";
            string name = "";
            
            if (string.IsNullOrEmpty(thename) || string.IsNullOrEmpty(namelist))
                return false;

            originalString = thename;

            do
            {
                thename = OneArgument(thename, ref part);
                if (string.IsNullOrEmpty(part))
                    return false;

                list = namelist;
                bool found = false;
                do
                {
                    list = list.OneArgument(ref name);
                    if (string.IsNullOrEmpty(name))
                        return false;

                    if (disallowPrefix == false)
                    {
                        //if (originalString.strPrefix(name) || name.strPrefix(part))
                        if (name.StringPrefix(part))
                        {
                            found = true;
                            break;
                        }
                            //return true;
                        //else if (part.StringPrefix(name))
                        //    break;
                    }


                    if (originalString.StringCmp(name))
                    {
                        return true;
                    }
                    if (part.StringCmp(name))
                    {
                        found = true;
                        break;
                    }
                } while (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(list));

                if (!found) return false;
            } while (!string.IsNullOrEmpty(part) && !string.IsNullOrEmpty(thename));


            return true;
        }

        public static IEnumerable<T> LoadFlagList<T>(string flagList, char delimiter = ' ')
        {
            if (string.IsNullOrEmpty(flagList))
                yield break;
            var flags = flagList.Split(delimiter);
            T flagValue = default(T);
            foreach (var flag in flags.Distinct())
                if (Utility.GetEnumValue<T>(flag, ref flagValue))
                    yield return flagValue;
                else if(flag != "none")
                    Game.log("LoadFlagList - Flag " + typeof(T).Name + " " + flag + " not found.");
        }

        /// <summary>
        /// Random number between 1 and 128, used mostly to compare skill %s to a chance to fail
        /// </summary>
        /// <returns></returns>
        public static int NumberPercent()
        {
            return Random(1, 128); // not exactly how rom does it...  they roll the dice till it is below 100 and add 1
        }

        /// <summary>
        /// First argument minimum, Middle argument arbitrary number, Last Argument maximum
        /// </summary>
        /// <param name="a">Minimum</param>
        /// <param name="b">Value to min/max</param>
        /// <param name="c">Maximum</param>
        /// <returns></returns>
        public static int URANGE(int a, int b, int c) => ((b) < (a) ? (a) : ((b) > (c) ? (c) : (b)));

        /// <summary>
        /// First argument minimum, Middle argument arbitrary number, Last Argument maximum
        /// </summary>
        /// <param name="a">Minimum</param>
        /// <param name="b">Value to min/max</param>
        /// <param name="c">Maximum</param>
        /// <returns></returns>
        public static float URANGE(float a, float b, float c) => ((b) < (a) ? (a) : ((b) > (c) ? (c) : (b)));
        
        public static int number_argument(this string args, ref string arg)
        {
            int index = 0;
            int result = 0;

            index = args.IndexOf('.');
            //if(index == -1) index = args.IndexOf(' ');
            if (index > 0)
            {
                int.TryParse(args.Substring(0, index), out result);
                if (args.Length > index + 1)
                {
                    arg = args.Substring(index + 1);
                }
                else
                    arg = "";
                return result;
            }
            arg = args;
            return 0;
        }

        public static int dice(int dicesides, int dicecount, int dicebonus = 0)
        {
            return Random(dicecount + dicebonus, dicecount * dicesides + dicebonus);//  new Dice(dicesides, dicecount, dicebonus).Roll();
        }

        /// <summary>
        /// Simple linear interpolation.
        /// </summary>
        public static int interpolate(int level, int value_00, int value_32)
        {
            return value_00 + level * (value_32 - value_00) / 32;
        }

        public static string ToStringFormatted(this XElement xml)
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.Encoding = Encoding.UTF8;
            settings.NewLineOnAttributes = true;
            settings.NewLineHandling = NewLineHandling.None;
            //StringBuilder result = new StringBuilder();
            using (var ms = new MemoryStream())
            {
                using (XmlWriter writer = XmlWriter.Create(ms, settings))
                {
                    xml.WriteTo(writer);
                }
                return Encoding.UTF8.GetString(ms.ToArray());
            }
            //return result.ToString();
        } // ToStringFormatted

        public static void AddRange<T>(this HashSet<T> list, IEnumerable<T> values)
        {
            foreach(var val in values)
            {
                list.Add(val);
            }
        }

        public static T[] ToArrayLocked<T>(this IEnumerable<T> enumerable)
        {
            lock(enumerable)
                return enumerable.ToArray();
        }

        //public static void RemoveAll<T>(this HashSet<T> list, Func<T, bool> predicate)
        //{
        //    foreach (var val in list)
        //    {
        //        if (predicate(val)) list.Remove(val);
        //    }
        //}
    }
}
