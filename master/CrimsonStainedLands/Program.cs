using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CrimsonStainedLands
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            Console.CancelKeyPress += Console_CancelKeyPress;
            
            Console.WriteLine(System.IO.Directory.GetCurrentDirectory());
            try
            {
                Game.Launch(Settings.Port);
                while (!Game.Instance.Info.Exiting)
                {
                    var log = Game.Instance.Info.RetrieveLog();
                    if (!string.IsNullOrEmpty(log))
                    {
                        Console.WriteLine(log.Trim());
                    }
                    System.Threading.Thread.Sleep(1);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            System.Environment.Exit(0);
        }
    }
}
