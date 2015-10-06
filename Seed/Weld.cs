using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seed.localhost
{
    public partial class Weld
    {
        public string InsertStatement
        {
            get
            {
                string insert = @"
                    INSERT INTO Welds (
                        Weld_ID,
                        Weld_Number,
                        Pipe1_ID,
                        Pipe2_ID,
                        Longitude,
                        Latitude,
                        Last_Updated,
                        Deleted,
                        Current_Location,
                        Weld_Barcode,
                        Weld_Coating,
                        Creation_Date,
                        Welder_First_Name,
                        Welder_Last_Name,
                        Wps_Number,
                        Modified_By_User_ID,
                        Station,
                        Billable_Txn_Details_ID,
                        Status,
                        Repaired,
                        Type,
                        Parent_Weld_ID,
                        Repair_Wps_Number)
                    VALUES(
                        " + WeldID + @",
                        '" + WeldNumber + @"',
                        " + Pipe1ID + @",
                        " + Pipe2ID + @",
                        '" + Longitude + @"',
                        '" + Latitude + @"',
                        '" + LastUpdated + @"',
                        0,
                        '" + CurrentLocation + @"',
                        '" + WeldBarcode + @"',
                        '" + WeldCoating + @"',
                        '" + CreationDate + @"',
                        '" + WelderFirstName + @"',
                        '" + WelderLastName + @"',
                        '" + WpsNumber + @"',
                        1000,
                        '" + Station + @"',
                        NULL,
                        '" + Status + @"',
                        0,
                        '" + Type + @"',
                        NULL,
                        '" + RepairWpsNumber + @"'
                    )";

                return insert;
            }
        }

        public string BulkInsertLine
        {
            get
            {
                return string.Format(
                    "{0}|" +    //Weld_ID
                    "{1}|" +    //Weld_Number
                    "{2}|" +    //Pipe1_ID
                    "{3}|" +    //Pipe2_ID
                    "{4}|" +    //Longitude
                    "{5}|" +    //Latitude
                    "{6}|" +    //Last_Updated
                    "{7}|" +    //Deleted
                    "{8}|" +    //Current_Location
                    "{9}|" +    //Weld_Barcode
                    "{10}|" +   //Weld_Coating
                    "{11}|" +   //Creation_Date
                    "{12}|" +   //Welder_First_Name
                    "{13}|" +   //Welder_Last_Name
                    "{14}|" +   //Wps_Number
                    "|" +       //Modified_By_User_ID
                    "{15}|" +   //Station
                    "|" +       //Billable_Txn_Details_ID
                    "{16}|" +   //Status
                    "{17}|" +   //Repaired
                    "{18}|" +   //Type
                    "|" +       //Parent_Weld_ID
                    "{19}|",    //Repair_Wps_Number
                    WeldID, WeldNumber, Pipe1ID, Pipe2ID, Longitude, Latitude, LastUpdated.Formatted(), 0, CurrentLocation,
                    WeldBarcode, WeldCoating, CreationDate.Formatted(), WelderFirstName, WelderLastName, WpsNumber, Station,
                    Status, Repaired, Type, RepairWpsNumber);
            }
        }
    }
}
