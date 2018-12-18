using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace Ashura
{
    class Program
    {
        public static byte[] refdata;

        static void Main(string[] args)
        {
            var dir = @"c:\gens";
            var rf = @"c:\gens\Sonic the Hedgehog 2.ref";

            while (!Directory.Exists(dir))
            {
                Console.WriteLine($"{dir} does not exist.");
                Console.Write("Enter savestate directory: ");
                dir = Console.ReadLine();
            }
            while (!File.Exists(rf))
            {
                Console.WriteLine($"{rf} does not exist.");
                Console.Write($"Enter reference savestate path: ");
                rf = Console.ReadLine();
            }

            var i = new System.IO.FileSystemWatcher(dir, "*.gs*");
            refdata = File.ReadAllBytes(rf);
            Console.WriteLine("Waiting for changes.  Save a savestate to analyze ...");
            while (true)
            {
                var r = i.WaitForChanged(System.IO.WatcherChangeTypes.All);
                Check($"{dir}\\{r.Name}");
            }

        }

        public static void Analyze(byte[] data)
        {
            Console.Clear();

            var sprite = 0x11c78;   // Address of start of SST
            var pal = 0x11f7c;      // Address of start of palette line 1
            var end = sprite + 960;      // Address to end the analysis on

            int sprites = 0;
            int overflow = 0;

            bool done = false;  // Set when link field is zero (no more sprites)
            bool sst = true;    // Whether or not we're still in the SST
                
            int n = 0;          // How many bytes we've gone through
            int sstend = 0;     // Address detected of SST end

            for (int i = sprite; i < end; i++)
            {
                bool inpal = (i >= pal);
                byte b = data[i];

                if (done) sst = false;
                if (n % 8 == 0)
                {
                    if (data[i + 3] == 0)
                    {
                        /* Link flag = 0, last sprite */
                        done = true;
                    }
                    if (done && sst) sstend = i + 8;
                    if (!done) sprites++;
                }

                bool corrupt = (i < pal + 24) && data[i] != refdata[i];

                ConsoleColor color;
                if (inpal)
                    if (sst)
                        color = ConsoleColor.Red;
                    else
                        color = corrupt ? ConsoleColor.Magenta : ConsoleColor.Blue;
                else
                    color = sst ? ConsoleColor.Green : ConsoleColor.DarkBlue;

                if (inpal && sst) overflow++;

                char c = (b == 0) ? '-' : '+';

                Console.ForegroundColor = color;
                Console.Write(c);

                n++;
                if (n % 64 == 0) Console.WriteLine();

            }

            n = 0;
            for (int r = sstend - 32; r < sstend + 32; r++) {
                if (n % 16 == 0)
                {
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.Write($"{r:x8} - ");
                }
                bool inpal = (r >= pal);
                sst = (r <= sstend);

                bool corrupt = (r < pal + 24) && data[r] != refdata[r];

                ConsoleColor color;
                if (inpal)
                    if (sst)
                        color = ConsoleColor.Red;
                    else
                        color = corrupt ? ConsoleColor.Magenta : ConsoleColor.Blue;
                else
                    color = sst ? ConsoleColor.Green : ConsoleColor.DarkBlue;

                Console.ForegroundColor = color;
                Console.Write($" {data[r]:x2}");

                n++;
            }  

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine();
            Console.WriteLine($"Sprites: {sprites}");
            Console.WriteLine($"SAT ends: {sstend:x}");
            Console.WriteLine($"Overflow: {overflow}");

        }

        public static void Check(string x)
        {
            start:
            try
            {
                var b = File.ReadAllBytes(x);
                Analyze(b);
            }
            catch (Exception ex) when (ex.Message.Contains("rocess"))
            {
                /* Sometimes the emulator is still writing to the file when we try to read it.  Just try again
                 * until its not locked */
                Thread.Sleep(100);
                goto start;
            }            
        }


    }


}
