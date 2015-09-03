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
                    "{0}|" +    //[Weld_Part_Type_ID]
                    "{1}|" +    //[Weld_Part_ID]
                    "{2}|" +    //[Type]
                    "{3}|" +    //[Modified_By_User_ID]
                    "{4}",      //[Billable_Txn_Details_ID]
                    string.Empty, WeldPartID, Type, string.Empty, string.Empty);
            }
        }
    }
}
