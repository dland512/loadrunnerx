using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seed.localhost
{
    public partial class ReferenceDocument
    {
        public string BulkInsertLine
        {
            get
            {
                return string.Format(
                    "{0}|" + //[Reference_Document_ID]
                    "{1}|" + //[Reference_Number]
                    "{2}|" + //[Document]
                    "{3}|" + //[Date_Created]
                    "{4}|" + //[Last_Updated]
                    "{5}|" + //[Content_Type]
                    "{6}",   //[Job_ID]
                    ReferenceDocumentID, ReferenceNumber, "00", DateCreated.Formatted(), LastUpdated.Formatted(), ContentType, JobID);
            }
        }
    }
}
