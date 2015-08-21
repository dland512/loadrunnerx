using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;
using PreCacher.localhost;

namespace PreCacher
{
    class Program
    {
        private MainService service;

        public class CommandLineArgs
        {
            [Option('u', "username", Required = true, HelpText = "Username for web service calls")]
            public string Username { get; set; }

            [Option('p', "password", Required = true, HelpText = "Password for web service calls")]
            public string Password { get; set; }

            [Option('v', "vendorid", Required = true, HelpText = "Vendor ID")]
            public long VendorID { get; set; }

            [Option('j', "jobid", Required = true, HelpText = "Job ID")]
            public long JobID { get; set; }

            [Option('r', "repeat", Required = true, HelpText = "How often (in minutes) to cache data")]
            public int RepeatMin { get; set; }

            [Option('t', "topofhour", Required = false, HelpText = "Wait until the top of the hour to start caching")]
            public bool TopOfHour { get; set; }
        }


        static void Main(string[] args)
        {
            CommandLineArgs parsedArgs = new CommandLineArgs();
            bool ok = CommandLine.Parser.Default.ParseArguments(args, parsedArgs);

            if (!ok)
            {
                HelpText help = HelpText.AutoBuild(parsedArgs);
                throw new Exception(string.Format("Invalid command line args: {0}", help.ToString()));
            }

            if (parsedArgs.TopOfHour)
            {
                //wait until the top of the hour to start
                TimeSpan ts = TimeUntilNextHour();
                Console.WriteLine("waiting for " + ts);
                Thread.Sleep(ts);
            }

            MainService service = GetService(parsedArgs.Username, parsedArgs.Password);
            PreCacher cacher = new PreCacher(service);
            
            while (true)
            {
                //cache the last hour of changes
                DateTime now = DateTime.Now;
                DateTime start = GetDateTimeToHour(now.AddHours(-1));
                DateTime end = GetDateTimeToHour(now);
                cacher.CacheData(parsedArgs.VendorID, parsedArgs.JobID, start, end);

                //wait until the top of the next hour
                Thread.Sleep(TimeUntilNextHour());
                //Thread.Sleep(parsedArgs.RepeatMin * 60 * 1000);
            }
        }


        private static MainService GetService(string username, string password)
        {
            MainService service = new MainService();
            service.Url = ConfigurationManager.AppSettings["ServiceUrl"];
            service.ServiceHeaderValue = new ServiceHeader() { Username = username, Password = password, ApiVersion = "release7" };
            service.Timeout = 100000000;
            return service;
        }


        private static DateTime GetDateTimeToHour(DateTime dt)
        {
            return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, 0, 0);
        }


        private static TimeSpan TimeUntilNextHour()
        {
            DateTime now = DateTime.Now;
            DateTime nextHour = GetDateTimeToHour(now.AddHours(1));
            return nextHour.Subtract(now);
        }
    }
}
