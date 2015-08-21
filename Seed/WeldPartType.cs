using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seed.localhost
{
    public partial class WeldPartType
    {
        public string BulkInsertLine
        {
            get
            {
                return string.Format(
                    "{0}|" +      //[Weld_Part_ID]
                    "{1}",        //[Type]
                    WeldPartID, Type);
            }
        }
    }
}
