using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seed.localhost
{
    public partial class WeldInspectionDocument
    {
        public string InsertStatement
        {
            get
            {
                return string.Format(@"
                    INSERT INTO Weld_Inspection_Documents (Weld_Inspection_Document_ID, Weld_Inspection_ID, Document_Ref_Number, Document_Image,
                        Created_Date, Last_Updated, Modified_By_User_ID, Billable_Txn_Details_ID, Content_Type)
                    VALUES({0}, {1}, '{2}', 0x0, GetDate(), GetDate(), NULL, NULL, 'image/png')", WeldInspectionDocumentID, WeldInspectionID, DocumentRefNumber);
            }
        }


        public string BulkInsertLine
        {
            get
            {
                return string.Format(
                    "{0}|" +      //[Weld_Inspection_Document_ID]
                    "{1}|" +      //[Weld_Inspection_ID]
                    "{2}|" +      //[Document_Ref_Number]
                    "{3}|" +      //[Document_Image]
                    "{4}|" +      //[Created_Date]
                    "{5}|" +      //[Last_Updated]
                    "|" +         //[Modified_By_User_ID]
                    "|" +         //[Billable_Txn_Details_ID]
                    "{6}",        //[Content_Type]
                    WeldInspectionDocumentID, WeldInspectionID, DocumentRefNumber, DocumentImage, DateTime.Now.Formatted(),
                    DateTime.Now.Formatted(), ContentType);
            }
        }
    }
}
