using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace LoadRunner.localhost
{
    public partial class MainService
    {
        public bool AcceptCachedData = true;

        protected override WebRequest GetWebRequest(Uri uri)
        {
            WebRequest request = base.GetWebRequest(uri);

            if (!AcceptCachedData)
                request.Headers.Add("Cache-Control", "no-cache");
           
            return request;
        }
    }
}
