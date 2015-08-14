using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seed.localhost
{
    public partial class WeldPart
    {
        public string InsertStatement
        {
            get
            {
                string insert = @"";
//                    INSERT INTO Weld_Parts (
//                        Weld_Part_ID              "{0}" +      //
//                        Weld_ID              "{0}" +      //
//                        Welder_ID              "{0}" +      //
//                        Position              "{0}" +      //
//                        Machine_ID              "{0}" +      //
//                        Machine_Bug_ID_1              "{0}" +      //
//                        Calibration_Ref_Num_1              "{0}" +      //
//                        Created_Date              "{0}" +      //
//                        Last_Updated              "{0}" +      //
//                        Modified_By_User_ID              "{0}" +      //
//                        Billable_Txn_Details_ID              "{0}" +      //
//                        Welder_2_ID              "{0}" +      //
//                        Position_2              "{0}" +      //
//                        Inspection_Passed              "{0}" +      //
//                        Machine_Bug_ID_2              "{0}" +      //
//                        Calibration_Image_1_ID              "{0}" +      //
//                        Calibration_Image_2_ID              "{0}" +      //
//                        Calibration_Ref_Num_2              "{0}" +      //
//                        Visual_Inspector_ID)
//                    VALUES(
//                        " + WeldPartID + @"              "{0}" +      //
//                        " + WeldID + @"              "{0}" +      //
//                        NULL              "{0}" +      //
//                        '" + Position + @"'              "{0}" +      //
//                        '" + MachineID + @"'              "{0}" +      //
//                        '" + MachineBugID1 + @"'              "{0}" +      //
//                        '" + CalibrationRefNum1 + @"'              "{0}" +      //
//                        '" + CreatedDate + @"'              "{0}" +      //
//                        '" + LastUpdated + @"'              "{0}" +      //
//                        1000              "{0}" +      //
//                        NULL              "{0}" +      //
//                        NULL              "{0}" +      //
//                        '" + Position2 + @"'              "{0}" +      //
//                        1              "{0}" +      //
//                        NULL              "{0}" +      //
//                        NULL              "{0}" +      //
//                        NULL              "{0}" +      //
//                        NULL              "{0}" +      //
//                        NULL
//                    )";

                return insert;
            }
        }


        public string BulkInsertLine
        {
            get
            {
                return string.Format(
                    "{0}|" +     //Weld_Part_ID
                    "{1}|" +     //Weld_ID
                    "|" +        //Welder_ID
                    "{2}|" +     //Position
                    "{3}|" +     //Machine_ID
                    "{4}|" +     //Machine_Bug_ID_1
                    "{5}|" +     //Calibration_Ref_Num_1
                    "{6}|" +     //Created_Date
                    "{7}|" +     //Last_Updated
                    "|" +        //Modified_By_User_ID
                    "|" +        //Billable_Txn_Details_ID
                    "|" +        //Welder_2_ID
                    "{8}|" +     //Position_2
                    "|" +        //Inspection_Passed
                    "{9}" +      //Machine_Bug_ID_2
                    "|" +        //Calibration_Image_1_ID
                    "|" +        //Calibration_Image_2_ID
                    "{10}|" +    //Calibration_Ref_Num_2
                    "|",         //Visual_Inspector_ID
                    WeldPartID, WeldID, Position, MachineID, MachineBugID1, CalibrationRefNum1,
                    DateTime.Now.ToString("yyyy/MM/dd MM:hh:ss"), DateTime.Now.ToString("yyyy/MM/dd MM:hh:ss"),
                    Position2, MachineBugID2, CalibrationRefNum2);
            }
        }
    }
}
