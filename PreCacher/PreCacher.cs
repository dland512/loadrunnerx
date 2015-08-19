using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PreCacher.localhost;

namespace PreCacher
{
    public class PreCacher
    {
        private MainService service;

        public PreCacher(MainService service)
        {
            this.service = service;
        }


        public void CacheData(long vendorID, long jobID, DateTime start, DateTime end)
        {
            Console.WriteLine("caching data for job {0}, times {1} - {2}", jobID, start.ToString("yyyy-MM-dd HH:mm:ss"), end.ToString("yyyy-MM-dd HH:mm:ss"));
            Stopwatch watch = new Stopwatch();
            watch.Start();

            //convert(dateTime,'2015-08-13 13:52:00',120)

            string where = string.Format("WHERE Vendor_ID = {0} AND Job_ID = {1} AND " +
                "Last_Updated BETWEEN convert(dateTime,'{2}',120) AND convert(dateTime,'{3}',120)",
                vendorID, jobID, start.ToString("yyyy-MM-dd HH:mm:ss"), end.ToString("yyyy-MM-dd HH:mm:ss"));

            Console.WriteLine(where);

            int total;
            service.SelectPipesWithoutImageDataWithWelds(where, "Pipe_ID", "ASC", "0", "0", out total);
            int numPages = (int)Math.Ceiling(total / 1000.0);

            for (int i = 0; i < numPages; i++)
            {
                Pipe[] pipes = service.SelectPipesWithoutImageDataWithWelds(where, "Pipe_ID", "ASC", i.ToString(), "1000", out total);
                Console.WriteLine("Retrieved {0} pipes for page {1} under job {2}: {3}", pipes.Length, i, jobID, watch.Elapsed);
            }

            watch.Stop();   
        }
    }
}
