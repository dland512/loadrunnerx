using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seed.localhost
{
    public partial class LoadImage
    {
        public string InsertStatement
        {
            get
            {
                return string.Format(@"
                    INSERT INTO Load_Images (Load_Image_ID, Reference_Number, Image, Load_ID, Image_Type, Modified_By_User_ID,
                        Billable_Txn_Details_ID, Last_Updated, Content_Type)
                    VALUES({0}, '{1}', 0x0, {2}, '{3}', NULL, NULL, GetDate(), 'image/png')", LoadImageID, ReferenceNumber, LoadID,
                                                                                          Type == LoadImageType.Identification ? "i" : "s");
            }
        }


        public string BulkInsertLine
        {
            get
            {
                return string.Format(
                    "{0}|" +     //Load_Image_ID
                    "{1}|" +     //Reference_Number
                    "00|" +      //Image
                    "{2}|" +     //Load_ID
                    "{3}|" +     //Image_Type
                    "|" +        //Modified_By_User_ID
                    "|" +        //Billable_Txn_Details_ID
                    "{4}|" +     //Last_Updated
                    "image/png", //Content_Type
                    LoadImageID,
                    ReferenceNumber,
                    LoadID,
                    Type == LoadImageType.Identification ? "i" : "s",
                    LastUpdated.ToString("yyyy/MM/dd HH:mm:ss"));
            }
        }
    }
}
