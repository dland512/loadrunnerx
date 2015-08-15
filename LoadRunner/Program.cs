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

namespace LoadRunner
{
    public class CommandLineArgs
    {
        public enum Operation { Full, Partial, Pipe, Weld, Map };

        [Option('u', "username", Required = true, HelpText = "Username for web service calls")]
        public string Username { get; set; }

        [Option('p', "password", Required = true, HelpText = "Password for web service calls")]
        public string Password { get; set; }

        [Option('j', "jobs", Required = true, HelpText = "Comma separated list of job IDs")]
        public string JobIDs { get; set; }

        [Option('o', "operations", Required = true, HelpText = "Comma separated list of operation to run. Options are Refresh, Pipe, Weld, Map")]
        public Operation Op { get; set; }

        [Option('n', "numops", Required = true, HelpText = "Number of operations each thread will perform")]
        public int NumOps { get; set; }

        [Option('t', "numthreads", Required = true, HelpText = "Number of threads")]
        public int? NumThreads { get; set; }

        [Option('s', "stagger", Required = true, HelpText = "Stagger users by min:max seconds")]
        public string StaggerUsers { get; set; }

        [Option('d', "downtime", Required = true, HelpText = "Seconds between requests min:max")]
        public string DownTimeSec { get; set; }
    }


    class Program
    {
        private static Random rand = new Random();
        private static CommandLineArgs parsedArgs;
        private static List<TimeSpan> requestTimes = new List<TimeSpan>();
        private static MainService service;
        private static List<long> vendorIDs = new List<long>();
        private static List<Job> jobs = new List<Job>();
        private static Dictionary<long, long> vendorToJob = new Dictionary<long, long>();
        private static Dictionary<long, int> jobNumPages = new Dictionary<long, int>();
        private static int staggerMin;
        private static int staggerMax;
        private static int downMin;
        private static int downMax;
        private static Dictionary<long, List<Task>> userTasks = new Dictionary<long, List<Task>>();
        

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

            List<Thread> threads = GetThreads();
            Console.WriteLine("have {0} threads that are being started", threads.Count);
            threads.ForEach(t => t.Start());
            threads.ForEach(t => t.Join());

            OutputResults();

            Console.WriteLine("done, press a key");
            Console.ReadKey();
        }


        private static void Init()
        {
            Console.WriteLine("intializing web services...");
            service = GetService();
            Console.WriteLine("    {0}", service.Url);

            List<long> jobIDs = new List<long>();

            foreach (string jid in parsedArgs.JobIDs.Split(','))
                jobIDs.Add(long.Parse(jid));

            Console.WriteLine("done");
            Console.WriteLine();

            //associate the vendors supplied to jobs supplied
            Console.WriteLine("getting jobs...");

            foreach (long jobID in jobIDs)
            {
                Job job = service.SelectJob(jobID);
                Console.WriteLine("   {0} ({1})", job.Name, job.JobID);
                jobs.Add(job);
            }

            Console.WriteLine();

            //get the number of 1000 pipe pages required for a full refresh for each job
            Console.WriteLine("getting pipe counts under jobs...");
            foreach (Job job in jobs)
            {
                int total;
                service.SelectPipes(string.Format("WHERE P.Job_ID = {0}", job.JobID), "P.Pipe_ID", "ASC", "0", "1", out total);
                Console.WriteLine("   {0} has {1} pipes", job.Name, total);
                jobNumPages[job.JobID] = (int)Math.Ceiling(total / 1000.0);
            }

            string[] staggerMinMax = parsedArgs.StaggerUsers.Split(':');
            staggerMin = int.Parse(staggerMinMax[0]);
            staggerMax = int.Parse(staggerMinMax[1]);

            string[] downMinMax = parsedArgs.DownTimeSec.Split(':');
            downMin = int.Parse(downMinMax[0]);
            downMax = int.Parse(downMinMax[1]);

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
                        threads.Add(new Thread(SetupFullRefresh));
                        break;

                    case CommandLineArgs.Operation.Partial:
                        threads.Add(new Thread(SetupPartialRefresh));
                        break;

                    case CommandLineArgs.Operation.Pipe:
                        //threads.AddRange(SetupPipeInserts());
                        break;

                    case CommandLineArgs.Operation.Weld:
                        //threads.AddRange(SetupWeldInserts());
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
            service.ServiceHeaderValue = new ServiceHeader() { Username = parsedArgs.Username, Password = parsedArgs.Password, ApiVersion = "release7" };
            service.Timeout = 100000000;
            return service;
        }


        private static void SetupFullRefresh()
        {
            int staggerTime = rand.Next(staggerMin * 1000, staggerMax * 1000);
            Console.WriteLine("waiting {0} milliseconds to start user on thread {1}", staggerTime, System.Threading.Thread.CurrentThread.ManagedThreadId);

            System.Threading.Thread.Sleep(staggerTime);

            Console.WriteLine("finished waiting to start user on thread {0}", System.Threading.Thread.CurrentThread.ManagedThreadId);

            for (int i = 0; i < parsedArgs.NumOps; i++)
                FullRefresh();
        }


        private static void SetupPartialRefresh()
        {
            int staggerTime = rand.Next(staggerMin * 1000, staggerMax * 1000);
            Console.WriteLine("waiting {0} milliseconds to start user on thread {1}", staggerTime, System.Threading.Thread.CurrentThread.ManagedThreadId);

            System.Threading.Thread.Sleep(staggerTime);

            Console.WriteLine("finished waiting to start user on thread {0}", System.Threading.Thread.CurrentThread.ManagedThreadId);

            for (int i = 0; i < parsedArgs.NumOps; i++)
                PartialRefresh();
        }


        private static List<Task> SetupPipeInserts()
        {
            List<Task> tasks = new List<Task>();

            for (int i = 0; i < parsedArgs.NumThreads; i++)
                tasks.Add(new Task(() => PipeInserts()));

            return tasks;
        }


        private static List<Task> SetupWeldInserts()
        {
            List<Task> tasks = new List<Task>();

            for (int i = 0; i < parsedArgs.NumThreads; i++)
                tasks.Add(new Task(() => WeldInserts()));

            return tasks;
        }


        private static Job GetRandomJob()
        {
            return jobs[rand.Next(0, jobs.Count)];
        }


        private static void FullRefresh()
        {
            Console.WriteLine("Full refresh");

            int total;
            Job job = GetRandomJob();
            long vendorID = job.VendorID;
            long numPages = jobNumPages[job.JobID];

            for (int i = 0; i < numPages; i++)
            {
                Stopwatch watch = new Stopwatch();
                watch.Start();

                string where = string.Format("WHERE Vendor_ID = {0} AND Job_ID = {1}", vendorID, job.JobID);
                string sortField = "Pipe_ID";
                string sortDir = "ASC";
                string pageSize = "1000";

                Console.WriteLine("Full refresh: getting page {0} for Job {1}", i, job.JobID);
                Pipe[] pipes = service.SelectPipesWithoutImageDataWithWelds(where, sortField, sortDir, i.ToString(), pageSize, out total);

                watch.Stop();
                requestTimes.Add(watch.Elapsed);

                Console.WriteLine("Retrieved {0} pipes for page {1} under job {2}: {3}", pipes.Length, i, job.JobID, watch.Elapsed);

                DownTime();
            }
        }


        private static void PartialRefresh()
        {
            int total;
            Job job = GetRandomJob();
            long vendorID = job.VendorID;
            long numPages = jobNumPages[job.JobID];

            Stopwatch watch = new Stopwatch();
            watch.Start();

            string dateTime = "2015-08-14 11:21:00";
            string where = string.Format("where Last_Updated >= convert(dateTime,'{0}',120) and Vendor_ID = {1} and Job_ID = {2}", dateTime, vendorID, job.JobID);
            string sortField = "Pipe_ID";
            string sortDir = "ASC";
            string page = "0";
            string pageSize = "1000000";

            Console.WriteLine("running partial refresh for Job {0}", job.Name);
            Pipe[] pipes = service.SelectPipesWithoutImageDataWithWelds(where, sortField, sortDir, page, pageSize, out total);

            watch.Stop();
            requestTimes.Add(watch.Elapsed);

            Console.WriteLine("partial refresh retrieved {0} pipes for job {1}: {2}", pipes.Length, job.JobID, watch.Elapsed);

            DownTime();
        }


        private static void PipeInserts()
        {
            Console.WriteLine("Pipe inserts");

            Stopwatch watch = new Stopwatch();
            watch.Start();

            Console.WriteLine(service.Url);
            service.InsertAndUpdatePipeWithMultipleBarcodes(GeneratePipe());

            watch.Stop();
            requestTimes.Add(watch.Elapsed);

            DownTime();
        }


        private static void WeldInserts()
        {
            Console.WriteLine("Weld inserts");

            Stopwatch watch = new Stopwatch();
            watch.Start();

            Console.WriteLine(service.Url);
            service.InsertWeld(GenerateWeld());

            watch.Stop();
            requestTimes.Add(watch.Elapsed);

            DownTime();
        }


        private static void DownTime()
        {
            //wait for some amount of time before doing another request
            int downTime = rand.Next(downMin * 1000, downMax * 1000);
            Console.WriteLine("going down for {0} milliseconds", downTime);
            System.Threading.Thread.Sleep(downTime);
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

            Job job = GetRandomJob();

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

            Console.WriteLine("total time: {0}, {1}", fullTime, fullTime.TotalMilliseconds);
            Console.WriteLine("min request time: {0}", minTime);
            Console.WriteLine("max request time: {0}", maxTime);
            Console.WriteLine("average request time: {0}", avgTime);

            SaveChart(requestTimes.Select(t => t.TotalSeconds).ToList());
        }


        private static void SaveChart(List<double> requestTimes)
        {
            int min = (int)requestTimes.Min();
            int max = (int)requestTimes.Max();

            DataSet dataSet = new DataSet();
            DataTable dt = new DataTable();
            dt.Columns.Add("Seconds", typeof(double));
            dt.Columns.Add("RequestCount", typeof(int));

            Dictionary<int, int> timeTable = new Dictionary<int, int>();

            List<DataRow> rows = new List<DataRow>();

            foreach (double time in requestTimes)
            {
                DataRow row = rows.FirstOrDefault(r => ((double)r[0]) == time);

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
            chart.Titles.Add("some really long title\nand more info\nand even more info");
            chart.Titles[0].Font = ca.AxisX.TitleFont;
            
            chart.DataBind();
            
            chart.SaveImage(@"chart.png", ChartImageFormat.Png);
        }
    }
}
