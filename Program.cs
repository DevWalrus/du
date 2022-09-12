// Author: Clinten Hopkins cmh3586


using System.Diagnostics;

namespace du
{
    public class DiskReader
    {
        static int folderCount;
        static int fileCount;
        static long byteCount;

        // private readonly object folderLock = new object();
        // private readonly object fileLock = new object();
        // private readonly object byteLock = new object();

        /// <summary>
        /// Initializes a new DiskReader object and sets every count to 0.
        /// </summary>
        public DiskReader()
        {
            folderCount = 0;
            fileCount = 0;
            byteCount = 0;
        }

        public void ResetCounters()
        {
            // lock (folderLock)
            // {
            //     folderCount = 0;
            // }
            //
            // lock (fileLock)
            // {
            //     fileCount = 0;
            // }
            //
            // lock (byteLock)
            // {
            //     byteCount = 0;
            // }
            Interlocked.Exchange(ref byteCount, 0);
            Interlocked.Exchange(ref fileCount, 0);
            Interlocked.Exchange(ref folderCount, 0);
        }

        // private void IncFolder()
        // {
        //     lock (folderLock)
        //     {
        //         folderCount++;
        //     }
        // }
        //
        // private void IncFile()
        // {
        //     lock (fileLock)
        //     {
        //         fileCount++;
        //     }
        // }
        //
        // private void AddByte(long size)
        // {
        //     lock (byteLock)
        //     {
        //         byteCount += size;
        //     }
        // }

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
            //IncFolder();
            Interlocked.Increment(ref folderCount);
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
                //IncFile();
                Interlocked.Increment(ref fileCount);
                //AddByte(fInfo.Length);
                Interlocked.Add(ref byteCount, fInfo.Length);
            }

            Parallel.ForEach(dirs, ParDU);

        }

        /// <summary>
        /// Look through a provided directory, <paramref name="dir"/>, and all children, to find the size of every file in the tree. The file count and
        /// sizes are then summed and stored in the <c>fileCount</c> and <c>byteCount</c> variable, respectively. The
        /// total folders traversed are also summed and stored in the <c>folderCount</c> variable.
        ///
        /// This process is done in happens in a sequential fashion, where every folder parsed on the same thread.
        /// The counterpart to this method is the <see cref="ParDU"/> method which handles each encountered folder on a
        /// new thread.
        /// </summary>
        /// <param name="dir">The parent directory to begin the tree search through</param>
        public void SeqDU(string dir)
        {
            //IncFolder();
            Interlocked.Increment(ref folderCount);
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
                //IncFile();
                Interlocked.Increment(ref fileCount);
                //AddByte(fInfo.Length);
                Interlocked.Add(ref byteCount, fInfo.Length);
            }

            foreach (var d in dirs)
            {
                SeqDU(d);
            }
        }

        public void Output(Stopwatch stopwatch, bool parallel)
        {
            Console.WriteLine("{0} Calculated in: {1:N7}s", parallel ? "Parallel" : "Sequential",
                stopwatch.Elapsed.TotalMilliseconds / 1000);
            Console.WriteLine("{0:N0} folders, {1:N0} files, {2:N0} bytes", folderCount, fileCount, byteCount);
        }

        /// <summary>
        /// 
        /// </summary>
        public void Error()
        {
            Console.WriteLine("Usage: du [-s] [-p] [-b] <path>\n" +
                              "Summarize disk usage of the set of FILES, recursively for directories.\n" +
                              "You MUST specify one of the parameters, -s, -p, or -b\n" +
                              "-s\tRun in single threaded mode\n" +
                              "-p\tRun in parallel mode (uses all available processors)\n" +
                              "-b\tRun in both parallel and single threaded mode.\n" +
                              "\tRuns parallel followed by sequential mode");
            Environment.Exit(1);
        }
    }

    public class du
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            var diskReader = new DiskReader();
            var stopwatch = new Stopwatch();

            if (args.Length < 2 || !new DirectoryInfo(args[1]).Exists)
            {
                Console.WriteLine(args.Length);
                Console.WriteLine(new DirectoryInfo(args[1]).Exists);
                diskReader.Error();
            }

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
                    diskReader.Error();
                    break;
            }
        }
    }
}