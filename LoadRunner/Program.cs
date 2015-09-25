using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.DataVisualization.Charting;
using CommandLine;
using CommandLine.Text;
using LoadRunner.localhost;
using System.Drawing;
using System.Threading;
using System.Configuration;
using System.IO;

namespace LoadRunner
{
    public class CommandLineArgs
    {
        public enum Operation { Full, Partial, PipeUpdate, PipeUpdateRefresh, WeldUpdate, WeldUpdateRefresh };

        [Option('u', "username", Required = true, HelpText = "Username for web service calls")]
        public string Username { get; set; }

        [Option('p', "password", Required = true, HelpText = "Password for web service calls")]
        public string Password { get; set; }

        [Option('j', "job", Required = true, HelpText = "Job ID")]
        public long JobID { get; set; }

        [Option('o', "operation", Required = true, HelpText = "Operation to run. Options are Refresh, Pipe, Weld, Map")]
        public Operation Op { get; set; }

        [Option('n', "numops", Required = true, HelpText = "Number of operations each thread will perform")]
        public int NumOps { get; set; }

        [Option('t', "numthreads", Required = true, HelpText = "Number of threads")]
        public int? NumThreads { get; set; }

        [Option('s', "stagger", Required = true, HelpText = "Stagger users by min:max seconds")]
        public string StaggerUsers { get; set; }

        [Option('d', "downtime", Required = true, HelpText = "Seconds between requests min:max")]
        public string DownTimeSec { get; set; }

        [Option('a', "appprocesstime", Required = false, HelpText = "Time (in milliseconds) that it will take the app to process a single record of data pulled down")]
        public int AppProcessingTime { get; set; }

        [Option('r', "refreshdate", Required = false, HelpText = "Partial refresh date/time (e.g. 2015-08-14 11:21:00)")]
        public string PartialRefreshDate { get; set; }
    }


    class Program
    {
        private static Random rand = new Random();
        private static CommandLineArgs parsedArgs;
        private static List<TimeSpan> requestTimes = new List<TimeSpan>();
        private static MainService service;
        private static Job job;
        private static Dictionary<long, int> jobNumPages = new Dictionary<long, int>();
        private static int staggerMin;
        private static int staggerMax;
        private static int downMin;
        private static int downMax;
        private static Dictionary<long, List<Task>> userTasks = new Dictionary<long, List<Task>>();
        private static List<Pipe> pipesToUpdate = new List<Pipe>();
        private static List<Weld> weldsToUpdate = new List<Weld>();
        static LocalDataStoreSlot lastUpdatedSlot = Thread.AllocateNamedDataSlot("LastUpdated");
        static Stopwatch overallWatch = new Stopwatch();
        

        static void Main(string[] args)
        {
            parsedArgs = new CommandLineArgs();
            bool ok = CommandLine.Parser.Default.ParseArguments(args, parsedArgs);

            if (!ok)
            {
                HelpText help = HelpText.AutoBuild(parsedArgs);
                throw new Exception(string.Format("Invalid command line args: {0}", help.ToString()));
            }

            Init();

            overallWatch.Start();

            List<Thread> threads = GetThreads();
            Console.WriteLine("have {0} threads that are being started", threads.Count);
            threads.ForEach(t => t.Start());
            threads.ForEach(t => t.Join());

            overallWatch.Stop();

            OutputResults();

            Console.WriteLine("done, press a key");
            Console.ReadKey();
        }


        private static void Init()
        {
            Console.WriteLine("intializing web services...");
            service = GetService();
            Console.WriteLine("    {0}", service.Url);

            Console.WriteLine("done");
            Console.WriteLine();

            //associate the vendors supplied to jobs supplied
            Console.WriteLine("getting jobs...");

            job = service.SelectJob(parsedArgs.JobID);
            Console.WriteLine("   {0} ({1})", job.Name, job.JobID);

            Console.WriteLine();

            //get the number of 1000 pipe pages required for a full refresh for each job
            Console.WriteLine("getting pipe counts under job...");

            int total;
            Pipe[] pipes = service.SelectPipes(string.Format("WHERE P.Vendor_ID = {0} AND P.Job_ID = {1}", job.VendorID, job.JobID),
                "P.Pipe_ID", "ASC", "0", "1000", out total);
            Console.WriteLine("   {0} has {1} pipes", job.Name, total);
            jobNumPages[job.JobID] = (int)Math.Ceiling(total / 1000.0);
            job.PipeCount = total;

            string[] staggerMinMax = parsedArgs.StaggerUsers.Split(':');
            staggerMin = int.Parse(staggerMinMax[0]);
            staggerMax = int.Parse(staggerMinMax[1]);

            string[] downMinMax = parsedArgs.DownTimeSec.Split(':');
            downMin = int.Parse(downMinMax[0]);
            downMax = int.Parse(downMinMax[1]);

            if (parsedArgs.Op == CommandLineArgs.Operation.PipeUpdate || parsedArgs.Op == CommandLineArgs.Operation.PipeUpdateRefresh)
            {
                //get 5000 random pipes to be used for updates
                pipesToUpdate = service.SelectPipes(string.Format("WHERE P.Vendor_ID = {0} AND P.Job_ID = {1}", job.VendorID, job.JobID),
                    "P.Barcode", "ASC", "0", "5000", out total).ToList();
            }

            if (parsedArgs.Op == CommandLineArgs.Operation.WeldUpdate || parsedArgs.Op == CommandLineArgs.Operation.WeldUpdateRefresh)
            {
                //get 5000 random welds to be used for updates
                weldsToUpdate = service.SelectWelds(string.Format("WHERE P1.Job_ID = {0}", job.JobID), "W.Weld_Barcode", "ASC", "0", "5000", out total).ToList();
            }

            Console.WriteLine();
            Console.WriteLine("starting tasks...");
            Console.WriteLine("   users: {0}", parsedArgs.NumThreads);
            Console.WriteLine("   operation: {0}", parsedArgs.Op);
            Console.WriteLine("   number of operations per user: {0}", parsedArgs.NumOps);
            Console.WriteLine("   start time staggering: {0}", parsedArgs.StaggerUsers);
            Console.WriteLine("   downtime between requests: {0}", parsedArgs.DownTimeSec);
            Console.WriteLine();

            Console.WriteLine();
        }


        private static List<Thread> GetThreads()
        {
            List<Thread> threads = new List<Thread>();

            for (int i = 0; i < parsedArgs.NumThreads; i++)
            {
                switch (parsedArgs.Op)
                {
                    case CommandLineArgs.Operation.Full:
                        threads.Add(new Thread(() => RunThreadOperations(FullRefresh)));
                        break;

                    case CommandLineArgs.Operation.Partial:
                        threads.Add(new Thread(() => RunThreadOperations(PartialRefresh)));
                        break;

                    case CommandLineArgs.Operation.PipeUpdate:
                        threads.Add(new Thread(() => RunThreadOperations(PipeUpdate)));
                        break;

                    case CommandLineArgs.Operation.PipeUpdateRefresh:
                        threads.Add(new Thread(() => RunThreadOperations(PipeUpdateAndRefresh)));
                        break;

                    case CommandLineArgs.Operation.WeldUpdate:
                        threads.Add(new Thread(() => RunThreadOperations(WeldUpdate)));
                        break;

                    case CommandLineArgs.Operation.WeldUpdateRefresh:
                        threads.Add(new Thread(() => RunThreadOperations(WeldUpdateAndRefresh)));
                        break;
                }
            }

            return threads;
        }


        private static void StartTasks(List<Task> tasks)
        {
            foreach (Task task in tasks)
                task.Start();
        }


        private static MainService GetService()
        {
            MainService service = new MainService();
            service.Url = ConfigurationManager.AppSettings["ServiceUrl"];
            service.ServiceHeaderValue = new ServiceHeader() { Username = parsedArgs.Username, Password = parsedArgs.Password, ApiVersion = "release8" };
            service.Timeout = 100000000;
            return service;
        }


        private delegate void RunOperation();


        private static void RunThreadOperations(RunOperation Operation)
        {
            int staggerTime = rand.Next(staggerMin * 1000, staggerMax * 1000);
            Log(string.Format("waiting {0} milliseconds to start user on thread {1}", staggerTime, System.Threading.Thread.CurrentThread.ManagedThreadId));
            System.Threading.Thread.Sleep(staggerTime);
            Log(string.Format("finished waiting to start user on thread {0}", System.Threading.Thread.CurrentThread.ManagedThreadId));

            for (int i = 0; i < parsedArgs.NumOps; i++)
            {
                Operation();
                DownTime();
                Console.WriteLine(System.Environment.NewLine);
            }
        }


        private static void FullRefresh()
        {
            Console.WriteLine("Full refresh");

            int total;
            long vendorID = job.VendorID;
            long numPages = jobNumPages[job.JobID];

            for (int i = 0; i < numPages; i++)
            {
                Stopwatch watch1 = new Stopwatch();
                Stopwatch watch2 = new Stopwatch();
                
                string where = string.Format("WHERE Vendor_ID = {0} AND Job_ID = {1}", vendorID, job.JobID);
                string sortField = "Pipe_ID";
                string sortDir = "ASC";
                string pageSize = "1000";

                Console.WriteLine("Full refresh: getting page {0} for Job {1}", i, job.JobID);
                service.AcceptCachedData = false;

                watch1.Start();
                Pipe[] pipes = service.SelectPipesWithoutImageDataWithWelds(where, sortField, sortDir, i.ToString(), pageSize, out total);
                watch1.Stop();
                Console.WriteLine("Retrieved {0} pipes for page {1} under job {2}: {3}", pipes.Length, i, job.JobID, watch1.Elapsed);

                ProcessDataInApp(pipes.Length);

                watch2.Start();
                Weld[] welds = service.SelectWeldsWithChildren(job.JobID, null, i, int.Parse(pageSize));
                watch2.Stop();
                Console.WriteLine("Retrieved {0} welds for page {1} under job {2}: {3}", welds.Length, i, job.JobID, watch2.Elapsed);

                ProcessDataInApp(welds.Length);

                TimeSpan totalTime = watch1.Elapsed.Add(watch2.Elapsed);
                requestTimes.Add(totalTime);

                Console.WriteLine("Total time for full refresh: {0}", totalTime);
            }
        }



        /// <summary>
        /// First refresh is based on the PartialRefreshDate command line parameter, subsequent refreshes are based on when the
        /// previous refresh was done.
        /// </summary>
        private static void PartialRefresh()
        {
            DateTime since;
            DateTime? lastUpdated = (DateTime?)Thread.GetData(lastUpdatedSlot);

            //get pipes and welds since the last request was made. If no requests have been made, use datetime passed in on commmand line
            if (lastUpdated != null)
                since = lastUpdated.Value;
            else
                since = DateTime.Parse(parsedArgs.PartialRefreshDate);

            Thread.SetData(lastUpdatedSlot, DateTime.Now);

            Log(string.Format("Running partial refresh for everthing after {0} ({1} since the last refresh)", since, DateTime.Now.Subtract(since)));

            int pageSize = 1000;
            int numReceived = 0;
            int page = 0;

            Stopwatch watch = new Stopwatch();
            watch.Start();

            /*
             * Get pipes using partial refresh
             */
            do
            {
                string where = GetPartialRefreshWhere(job.VendorID, job.JobID, since);
                int total;

                Pipe[] pipes = service.SelectPipesWithoutImageDataWithWelds(where, "Pipe_ID", "ASC", page.ToString(), pageSize.ToString(), out total);

                numReceived = pipes.Length;
                Log(string.Format("partial pipe refresh on page {0} returned {1} pipes", page, numReceived));
                page++;

                //wait while the app processes the data
                ProcessDataInApp(numReceived);
            }
            while (numReceived >= pageSize);

            pageSize = 1000;
            numReceived = 0;
            page = 0;

            /*
             * Get welds using partial refresh
             */
            do
            {
                Weld[] welds = service.SelectWeldsWithChildren(job.JobID, since, page, pageSize);

                numReceived = welds.Length;
                Log(string.Format("partial weld refresh on page {0} returned {1} welds", page, numReceived));
                page++;

                //wait while the app processes the data
                ProcessDataInApp(numReceived);
            }
            while (numReceived >= pageSize);

            Log(string.Format("Partial refresh done: {0}", watch.Elapsed));

            watch.Stop();
            requestTimes.Add(watch.Elapsed);
        }


        private static string GetPartialRefreshWhere(long vendorID, long jobID, DateTime since)
        {
            return string.Format("where Last_Updated >= convert(dateTime,'{0}',120) and Vendor_ID = {1} and Job_ID = {2}",
                since.ToString("yyyy-MM-dd HH:mm:ss"), vendorID, jobID);
        }


        private static DateTime GetDateTimeToHour(DateTime dt)
        {
            return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, 0, 0);
        }


        private static void PipeInserts()
        {
            Console.WriteLine("Pipe inserts");

            Stopwatch watch = new Stopwatch();
            watch.Start();

            service.InsertAndUpdatePipeWithMultipleBarcodes(GeneratePipe());

            watch.Stop();
            requestTimes.Add(watch.Elapsed);
        }


        private static void PipeUpdate()
        {
            Console.WriteLine("Pipe update");
            Pipe pipe = pipesToUpdate[rand.Next(0, pipesToUpdate.Count)];
            pipe.Image = string.Empty;
            pipe.Thumbnail = string.Empty;
            pipe.Alert = "new alert!";

            Stopwatch watch = new Stopwatch();
            watch.Start();

            service.UpdatePipe(pipe);

            watch.Stop();
            requestTimes.Add(watch.Elapsed);
        }


        private static void PipeUpdateAndRefresh()
        {
            Console.WriteLine("Pipe update followed by a refresh");
            PipeUpdate();
            Thread.Sleep(2000);
            PartialRefresh();
        }


        private static void WeldUpdate()
        {
            Weld weld = weldsToUpdate[rand.Next(0, weldsToUpdate.Count)];

            Stopwatch watch = new Stopwatch();
            watch.Start();

            weld.Station = "playstation";
            weld.LastUpdated = DateTime.Now;
            service.UpdateWeld(weld);

            watch.Stop();
            requestTimes.Add(watch.Elapsed);
        }


        private static void WeldUpdateAndRefresh()
        {
            Console.WriteLine("Weld update followed by a refresh");
            WeldUpdate();
            Thread.Sleep(2000);
            PartialRefresh();
        }


        private static void WeldInserts()
        {
            Console.WriteLine("Weld inserts");

            Stopwatch watch = new Stopwatch();
            watch.Start();

            service.InsertWeld(GenerateWeld());

            watch.Stop();
            requestTimes.Add(watch.Elapsed);
        }


        private static void DownTime()
        {
            //wait for some amount of time before doing another request
            int downTime = rand.Next(downMin * 1000, downMax * 1000);
            Console.WriteLine("going down for {0} milliseconds", downTime);
            System.Threading.Thread.Sleep(downTime);
        }


        private static void ProcessDataInApp(int numRecs)
        {
            int totalTime = parsedArgs.AppProcessingTime * numRecs;
            Console.WriteLine("app will process {0} records for {1} milliseconds each, total time: {2} seconds", numRecs, parsedArgs.AppProcessingTime, totalTime/1000.0);
            System.Threading.Thread.Sleep(totalTime);
        }


        private static Int64 NextInt64(Random rand)
        {
            byte[] buffer = new byte[sizeof(Int64)];
            rand.NextBytes(buffer);
            return BitConverter.ToInt64(buffer, 0);
        }


        private static Pipe GeneratePipe()
        {
            Random rand = new Random();
            long pipeID = NextInt64(rand);

            Pipe pipe = new Pipe()
            {
                PipeID = pipeID,
                Barcode = pipeID + "bc",
                Number = pipeID + "nm",
                VendorID = job.VendorID,
                JobID = job.JobID,
                Coating = "ARO-L",
                Diameter = "10",
                CurrentLocation = "Austin, TX",
                Latitude = "30.25",
                Longitude = "97.75",
                Length = "60",
                Status = "IN",
                Type = "HFW",
                Wall = "0.365",
                Heat = "ZM1610",
                Grade = "X52",
                Manufacturer = "SeAH",
                Image = string.Empty,
                Thumbnail = string.Empty,
                Issue = true,
                Class = "class",
                Owner = "owner",
                Alert = "alert",
                Cut = false,
                Bend = false,
                Verify = false,
                MillLength = "1",
                Station = "Station X",
                LastUpdated = DateTime.Now.ToString(),
                NoteCount = 0,
                InspectionPassed = true
            };

            pipe.AdditionalBarcodes = new string[]
            {
                pipe.Barcode + "_1",
                pipe.Barcode + "_2",
                pipe.Barcode + "_3",
                pipe.Barcode + "_4",
                pipe.Barcode + "_5",
                pipe.Barcode + "_6"
            };

            return pipe;
        }


        private static Weld GenerateWeld()
        {
            Random rand = new Random();
            long weldID = NextInt64(rand);

            Weld weld = new Weld()
            {
                WeldID = weldID,
                Pipe1ID = 20130503065448923,
                Pipe2ID = 20130503065448925,
                WeldBarcode = weldID + "_bc",
                WeldNumber = weldID + "_nm",
                WpsNumber = weldID + "wps",
                RepairWpsNumber = weldID + "_rwps",
                Longitude = "30.25",
                Latitude = "97.75",
                Station = "Station W",
                CurrentLocation = "Springfield, OR",
                CreationDate = DateTime.Now,
                LastUpdated = DateTime.Now,
                Status = WeldStatus.Active,
                Repaired = false,
                Type = "Fillet",
                ParentWeldID = null,
                WeldCoating = "FBE",
            };

            return weld;
        }


        private static void OutputResults()
        {
            TimeSpan fullTime = TimeSpan.Zero;
            double avgTime = 0;
            double minTime = double.MaxValue;
            double maxTime = 0;

            foreach (TimeSpan time in requestTimes)
            {
                fullTime = fullTime.Add(time);
                avgTime += time.TotalMilliseconds;

                if (time.TotalMilliseconds < minTime)
                    minTime = time.TotalMilliseconds;

                if (time.TotalMilliseconds > maxTime)
                    maxTime = time.TotalMilliseconds;

                //Console.WriteLine("request time: {0}", time.TotalMilliseconds);
            }

            avgTime /= requestTimes.Count;

            Console.WriteLine("total test time: {0}", overallWatch.Elapsed);
            Console.WriteLine("total time waiting for data: {0}", fullTime.ToString("g"));
            Console.WriteLine("min request time: {0}", FormatTime(minTime));
            Console.WriteLine("max request time: {0}", FormatTime(maxTime));
            Console.WriteLine("average request time: {0}", FormatTime(avgTime));

            SaveChart(requestTimes.Select(t => t.TotalMilliseconds).ToList(), fullTime, minTime, maxTime, avgTime);
        }


        private static void SaveChart(List<double> requestTimes, TimeSpan totalTime, double minTime, double maxTime, double avgTime)
        {
            DataSet dataSet = new DataSet();
            DataTable dt = new DataTable();
            dt.Columns.Add("Seconds", typeof(int));
            dt.Columns.Add("RequestCount", typeof(int));

            Dictionary<int, int> timeTable = new Dictionary<int, int>();

            List<DataRow> rows = new List<DataRow>();
            List<int> intTimes = requestTimes.OrderBy(t => t).Select(t => (int)Math.Round(t/1000)).ToList();

            foreach (int time in intTimes)
            {
                DataRow row = rows.FirstOrDefault(r => ((int)r[0]) == time);

                if (row == null)
                {
                    row = dt.NewRow();
                    row[0] = time;
                    row[1] = 0;
                    rows.Add(row);
                }

                row[1] = ((int)row[1]) + 1;
            }

            rows.ForEach(r => dt.Rows.Add(r));

            dataSet.Tables.Add(dt);

            Chart chart = new Chart();
            chart.DataSource = dataSet.Tables[0];
            chart.Width = 900;
            chart.Height = 500;

            Series series = new Series();
            series.Name = "Serie1";
            series.Color = Color.FromArgb(220, 0, 27);
            series.ChartType = SeriesChartType.Column;
            series.ShadowOffset = 0;
            series.IsValueShownAsLabel = true;
            series.XValueMember = "Seconds";
            series.YValueMembers = "RequestCount";
            series.Font = new Font(series.Font.FontFamily, 10);
            chart.Series.Add(series);
            
            ChartArea ca = new ChartArea();
            ca.Name = "ChartArea1";
            ca.BackColor = Color.White;
            ca.BorderWidth = 0;
            ca.AxisX = new Axis();
            ca.AxisY = new Axis();
            ca.AxisX.Title = "Time (seconds)";
            Font f = ca.AxisX.TitleFont;
            ca.AxisX.TitleFont = new Font(f.FontFamily, 12, f.Style);
            ca.AxisY.Title = "Request count";
            ca.AxisY.TitleFont = ca.AxisX.TitleFont;
            ca.AxisX.MajorGrid.LineColor = Color.LightGray;
            ca.AxisY.MajorGrid.LineColor = ca.AxisX.MajorGrid.LineColor;
            chart.ChartAreas.Add(ca);

            chart.Titles.Add("Requests times");
            chart.Titles[0].Font = ca.AxisX.TitleFont;
            chart.Titles.Add(GetChartDescriptionString(totalTime, minTime, maxTime, avgTime));
            chart.Titles[1].Font = new Font(chart.Titles[1].Font.FontFamily, 10);
            
            chart.DataBind();
            
            int i = 0;
            string fileName = "";

            //loop until you find a free file name (in case multiple instances are running at the same time)
            do
            {
                fileName = string.Format("chart-{0}.png", i++);
            }
            while(File.Exists(fileName));

            chart.SaveImage(fileName, ChartImageFormat.Png);
        }


        private static string FormatTime(double milliseconds)
        {
            return new TimeSpan(0, 0, 0, 0, (int)milliseconds).ToString(@"hh\:mm\:ss\.ff");
        }

        private static string GetChartDescriptionString(TimeSpan totalTime, double minTime, double maxTime, double avgTime)
        {
            return string.Format("job: {0}, num pipes: {1},   threads: {2},   op: {3},   op count: {4},   stagger: {5},   downtime: {6}\n" +
                "num requests: {7},   total time: {8},   min request: {9},   max request: {10},   avg request: {11}", job.Name, job.PipeCount,
                parsedArgs.NumThreads, parsedArgs.Op, parsedArgs.NumOps, parsedArgs.StaggerUsers, parsedArgs.DownTimeSec, requestTimes.Count,
                totalTime.ToString(@"hh\:mm\:ss\.ff"), FormatTime(minTime), FormatTime(maxTime), FormatTime(avgTime));
        }


        private static void Log(string msg)
        {
            Console.WriteLine("{0}: {1}", DateTime.Now, msg);
        }
    }
}
