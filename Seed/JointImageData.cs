using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seed.localhost
{
    public partial class JointImageData
    {
        public string InsertStatement
        {
            get
            {
                return string.Format(@"
                    INSERT INTO Joint_Image_Data (Pipe_ID, Image, Last_Updated, Modified_By_User_ID, Client_Image_ID,
                        Reference_Number, Billable_Txn_Details_ID, Content_Type)
                    VALUES({0}, 0x0, GetDate(), NULL, '{1}', '{2}', NULL, 'image/png')", PipeID, JointImageDataID, ReferenceNumber);
            }
        }


        public string BulkInsertLine
        {
            get
            {
                //Joint_Image_Data_ID,Image,Pipe_ID,Thumbnail,Last_Updated,Modified_By_User_ID,Client_Image_ID,Reference_Number,Billable_Txn_Details_ID,Content_Type
                return string.Format("|00|{0}|00|{1}||{2}|{3}||{4}", PipeID, LastUpdated.Formatted(), JointImageDataID, ReferenceNumber, ContentType);
            }
        }
    }
}
