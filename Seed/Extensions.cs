using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seed
{
    public static class Extensions
    {
        public static string Formatted(this DateTime dt)
        {
            return dt.ToString("yyyy/MM/dd HH:mm:ss");
        }
    }
}
