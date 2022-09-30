// Author: Clinten Hopkins cmh3586


using System.Diagnostics;

namespace du
{
    /// <summary>
    /// A class of essentially helper functions for the <see cref="du"/> program.
    /// </summary>
    public class DiskReader
    {
        private static int _folderCount;
        private static int _fileCount;
        private static long _byteCount;

        /// <summary>
        /// Initializes a new DiskReader object and sets every count to 0.
        /// </summary>
        public DiskReader()
        {
            _folderCount = 0;
            _fileCount = 0;
            _byteCount = 0;
        }

        /// <summary>
        /// A simple method that sets all of the counters back to 0 to allow the user to run another pass of the reader.
        /// </summary>
        public void ResetCounters()
        {
            Interlocked.Exchange(ref _byteCount, 0);
            Interlocked.Exchange(ref _fileCount, 0);
            Interlocked.Exchange(ref _folderCount, 0);
        }

        /// <summary>
        /// Look through a provided directory, <paramref name="dir"/>, and all children, to find the size of every file in the tree. The file count and
        /// sizes are then summed and stored in the <c>fileCount</c> and <c>byteCount</c> variable, respectively. The
        /// total folders traversed are also summed and stored in the <c>folderCount</c> variable.
        ///
        /// This process is done in happens in a parallel fashion, where every folder is given a new thread. This means any given thread is
        /// going to be counting the sizes of the files in its folder and creating new threads to handle the children
        /// files. The counterpart to this method is the <see cref="SeqDU"/> method which handles all the folders on the same thread.
        /// </summary>
        /// <param name="dir">The parent directory to begin the tree search through</param>
        public void ParDU(string dir)
        {
            Interlocked.Increment(ref _folderCount);
            List<string> files = new List<string>();
            string[] dirs = Array.Empty<string>();
            try
            {
                files = new List<string>(Directory.GetFiles(dir));
                dirs = Directory.GetDirectories(dir);
            }
            catch
            {
                // Ignore these exceptions, throw all others
            }

            foreach (var fInfo in files.Select(f => new FileInfo(f)))
            {
                Interlocked.Increment(ref _fileCount);
                Interlocked.Add(ref _byteCount, fInfo.Length);
            }

            Parallel.ForEach(dirs, ParDU);

        }

        /// <summary>
        /// Look through a provided directory, <paramref name="dir"/>, and all children, to find the size of every file in the tree. The file count and
        /// sizes are then summed and stored in the <c>fileCount</c> and <c>byteCount</c> variable, respectively. The
        /// total folders traversed are also summed and stored in the <c>folderCount</c> variable.
        ///
        /// This process is done in happens in a sequential fashion, where every folder parsed recursively on the same
        /// thread. The counterpart to this method is the <see cref="ParDU"/> method which handles each encountered
        /// folder on a new thread.
        /// </summary>
        /// <param name="dir">The parent directory to begin the tree search through</param>
        public void SeqDU(string dir)
        {
            _folderCount++;
            List<string> files = new List<string>();
            
            string[] dirs = Array.Empty<string>();
            try
            {
                files = new List<string>(Directory.GetFiles(dir));
                dirs = Directory.GetDirectories(dir);
            }
            catch (UnauthorizedAccessException ignored)
            {
                // Ignore these exceptions, throw all others
            }

            foreach (var fInfo in files.Select(f => new FileInfo(f)))
            {
                _fileCount++;
                _byteCount += fInfo.Length;
            }

            foreach (var d in dirs)
            {
                SeqDU(d);
            }
        }

        /// <summary>
        /// Prints out the data from the most recent run(s) of the du program. <paramref name="stopwatch"/> is passed in
        /// to get the total run time of the program, the <paramref name="parallel"/> is passed in to denote what type
        /// of run occured. 
        /// </summary>
        /// <param name="stopwatch">The total run time of the program, this gets converted into a double number of
        /// seconds elapsed.</param>
        /// <param name="parallel">True if the preceding run was done in parallel, false otherwise</param>
        public void Output(Stopwatch stopwatch, bool parallel)
        {
            Console.WriteLine("{0} Calculated in: {1:N7}s", parallel ? "Parallel" : "Sequential",
                stopwatch.Elapsed.TotalMilliseconds / 1000);
            Console.WriteLine("{0:N0} folders, {1:N0} files, {2:N0} bytes", _folderCount, _fileCount, _byteCount);
        }

        /// <summary>
        /// Outputs a help message whenever a command line argument is incorrect or improperly formatted.
        /// </summary>
        public static void HelpMsg()
        {
            Console.WriteLine("Usage: du [-s] [-p] [-b] <path>\n" +
                              "Summarize disk usage of the set of FILES, recursively for directories.\n" +
                              "You MUST specify one of the parameters, -s, -p, or -b\n" +
                              "-s\tRun in single threaded mode\n" +
                              "-p\tRun in parallel mode (uses all available processors)\n" +
                              "-b\tRun in both parallel and single threaded mode.\n" +
                              "\tRuns parallel followed by sequential mode");
        }
    }

    /// <summary>
    /// The main program. The full description is in <see cref="du.Main"/>
    /// </summary>
    public class du
    {
        /// <summary>
        /// A program that take in a predefined number of <paramref name="args"/> as defined in the
        /// <see cref="DiskReader.HelpMsg"/>. The runs are timed and then output in a formatted message; see:
        /// <see cref="DiskReader.Output"/>. The program has 4 defined exit codes:
        /// <list type="table">
        ///     <listheader>
        ///         <term>Exit Code</term>
        ///         <description>Meaning</description>
        ///     </listheader>
        ///     <item>
        ///         <term>0</term>
        ///         <description>Success</description>
        ///     </item>
        ///     <item>
        ///         <term>1</term>
        ///         <description>Incorrect number of args provided</description>
        ///     </item>
        ///     <item>
        ///         <term>2</term>
        ///         <description>Directory is not available to the program</description>
        ///     </item>
        ///     <item>
        ///         <term>3</term>
        ///         <description>First arg is not defined</description>
        ///     </item>
        /// </list>
        /// </summary>
        /// <param name="args">Arguments as they are defined in <see cref="DiskReader.HelpMsg"/></param>
        public static void Main(string[] args)
        {
            var diskReader = new DiskReader();
            var stopwatch = new Stopwatch();

            if (args.Length < 2)
            {
                DiskReader.HelpMsg();
                Environment.Exit(1);
            }

            if (!new DirectoryInfo(args[1]).Exists)
            {
                DiskReader.HelpMsg();
                Environment.Exit(2);
            }

            Console.WriteLine("Directory: '{0}':", new DirectoryInfo(args[1]).FullName);
    
            switch (args[0])
            {
                case "-p":
                    stopwatch.Start();
                    diskReader.ParDU(args[1]);
                    stopwatch.Stop();
                    diskReader.Output(stopwatch, true);
                    break;
                case "-s":
                    stopwatch.Start();
                    diskReader.SeqDU(args[1]);
                    stopwatch.Stop();
                    diskReader.Output(stopwatch, false);
                    break;
                case "-b":
                    stopwatch.Start();
                    diskReader.ParDU(args[1]);
                    stopwatch.Stop();
                    diskReader.Output(stopwatch, true);

                    stopwatch.Reset();
                    diskReader.ResetCounters();

                    stopwatch.Start();
                    diskReader.SeqDU(args[1]);
                    stopwatch.Stop();
                    diskReader.Output(stopwatch, false);
                    break;
                default:
                    DiskReader.HelpMsg();
                    Environment.Exit(3);
                    break;
            }
        }
    }
}