// Author: Clinten Hopkins cmh3586


using System.Diagnostics;

namespace du
{
    public class DiskReader
    {
        
        public int FolderCount { get; private set; }
        public int FileCount { get; private set; }
        public long ByteCount { get; private set; }
        
        private readonly object folderLock = new object();
        private readonly object fileLock = new object();
        private readonly object byteLock = new object();

        private void ResetCounters()
        {
            lock (folderLock)
            {
                FolderCount = 0;
            }
            
            lock (fileLock)
            {
                FileCount = 0;
            }
            
            lock (byteLock)
            {
                ByteCount = 0;
            }
        }
        
        private void IncFolder()
        {
            lock (folderLock)
            {
                FolderCount++;
            }
        }
        
        private void IncFile()
        {
            lock (fileLock)
            {
                FileCount++;
            }
        }
        
        private void AddByte(long size)
        {
            lock (byteLock)
            {
                ByteCount += size;
            }
        }

        public void ParDU(string dir)
        {
            IncFolder();
            List<string> files = new List<string>();
            string[] dirs = Array.Empty<string>();
            try
            {
                files = new List<string>(Directory.GetFiles(dir));
                dirs = Directory.GetDirectories(dir);
            }catch{}

            foreach (var fInfo in files.Select(f => new FileInfo(f)))
            {
                IncFile();
                AddByte(fInfo.Length);
            }
            
            Parallel.ForEach(dirs, ParDU);

        }
        
        public void SeqDU(string dir)
        {
            IncFolder();
            List<string> files = new List<string>();

            string[] dirs = Array.Empty<string>();
            try
            {
                files = new List<string>(Directory.GetFiles(dir));
                dirs = Directory.GetDirectories(dir);
            }catch{}
            foreach (var fInfo in files.Select(f => new FileInfo(f)))
            {
                IncFile();
                AddByte(fInfo.Length);
            }
            
            foreach (var d in dirs)
            {
                SeqDU(d);
            }
        }

        private void Output(Stopwatch stopwatch, bool parallel)
        {
            Console.WriteLine("{0} Calculated in: {1:N7}s", parallel ? "Parallel" : "Sequential", stopwatch.Elapsed.TotalMilliseconds/1000);
            Console.WriteLine("{0:N0} folders, {1:N0} files, {2:N0} bytes", FolderCount, FileCount, ByteCount);
        }

        public void Error()
        {
            Console.WriteLine("Usage: du [-s] [-p] [-b] <path>\n" +
                              "Summarize disk usage of the set of FILES, recursively for directories.\n"+
                              "You MUST specify one of the parameters, -s, -p, or -b\n"+
                              "-s\tRun in single threaded mode\n"+
                              "-p\tRun in parallel mode (uses all available processors)\n"+
                              "-b\tRun in both parallel and single threaded mode.\n"+
                              "\tRuns parallel followed by sequential mode");
            Environment.Exit(1);
        }
        
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