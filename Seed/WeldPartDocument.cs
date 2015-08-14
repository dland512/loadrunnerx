using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seed.localhost
{
    public partial class WeldPartDocument
    {
        public string InsertStatement
        {
            get
            {
                return string.Format(@"
                    INSERT INTO Weld_Part_Documents (Weld_Part_Document_ID, Weld_Part_ID, Document_Ref_Number, Document_Image, Created_Date, Last_Updated,
                        Modified_By_User_ID, Billable_Txn_Details_ID, Content_Type)
                    VALUES({0}, {1}, '{2}', 0x0, GetDate(), GetDate(), NULL, NULL, 'image/png')", WeldPartDocumentID, WeldPartID, DocumentRefNumber);
            }
        }


        public string BulkInsertLine
        {
            get
            {
                return string.Format(
                    "{0}|" +      //[Weld_Part_Document_ID]
                    "{1}|" +      //[Weld_Part_ID]
                    "{2}|" +      //[Document_Ref_Number]
                    "00|" +       //[Document_Image]
                    "{3}|" +      //[Created_Date]
                    "{4}|" +      //[Last_Updated]
                    "|" +         //[Modified_By_User_ID]
                    "|" +         //[Billable_Txn_Details_ID]
                    "image/png",  //[Content_Type]
                    WeldPartDocumentID, WeldPartID, DocumentRefNumber, DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"),
                    DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
            }
        }
    }
}
