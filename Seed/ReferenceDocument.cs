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
                    "{3}|" + //[Modified_By_User_ID]
                    "{4}|" + //[Date_Created]
                    "{5}|" + //[Last_Updated]
                    "{6}|" + //[Content_Type]
                    "{7}",   //[Job_ID]
                    ReferenceDocumentID, ReferenceNumber, Document, "", DateCreated.Formatted(), LastUpdated.Formatted(), ContentType, JobID);
            }
        }
    }
}
