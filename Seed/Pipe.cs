using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seed.localhost
{
    public partial class Pipe
    {
        public string InsertStatement
        {
            get
            {
                string insert = @"
                    INSERT INTO Pipes (
                        Pipe_ID,
                        Barcode,
                        Creation_Date,
                        Image,
                        Last_Updated,
                        Issue,
                        Length,
                        Status,
                        Type,
                        Wall,
                        Thumbnail,
                        Coating,
                        Diameter,
                        Vendor_ID,
                        Class,
                        Heat,
                        Owner,
                        Latitude,
                        Longitude,
                        Current_Location,
                        Grade,
                        Number,
                        Manufacturer,
                        Cut,
                        Verify,
                        Modified_By_User_ID,
                        Alert,
                        Job_ID,
                        Parent_Pipe_ID,
                        Mill_Length,
                        Station,
                        Billable_Txn_Details_ID,
                        Bend,
                        Standard_Change,
                        Inspection_Passed)
                    VALUES(
                        " + PipeID + @",
                        ,Barcode
                        ,CreationDate
                        '',
                        ,LastUpdated
                        " + BoolToSqlValue(Issue) + @",
                        ,Length
                        ,Status
                        ,Type
                        ,Wall
                        '',
                        ,Coating
                        ,Diameter
                        " + VendorID + @",
                        ,Class
                        ,Heat
                        ,Owner
                        ,Latitude
                        ,Longitude
                        ,CurrentLocation
                        ,Grade
                        ,Number
                        ,Manufacturer
                        " + BoolToSqlValue(Cut) + @",
                        " + BoolToSqlValue(Verify) + @",
                        1000,
                        ,Alert
                        " + JobID + @",
                        NULL,
                        ,MillLength
                        ,Station
                        NULL,
                        " + BoolToSqlValue(Bend) + @",
                        1,
                        " + BoolToSqlValue(InspectionPassed) + @"
                    )";

                return insert;
            }
        }

        public string BulkInsertLine
        {
            get
            {
                return string.Format("{0}|{1}|{2}|{3}|{4}|{5}|{6}|{7}|{8}|{9}|{10}|{11}|{12}|{13}|{14}|{15}|{16}|{17}|{18}|" +
                    "{19}|{20}|{21}|{22}|{23}|{24}|{25}|{26}|{27}|{28}|{29}|{30}|{31}|{32}|{33}|{34}",
                    PipeID, Barcode, CreationDate, "00", LastUpdated, BoolToSqlValue(Issue), Length, Status, Type, Wall,
                    "00", Coating, Diameter, VendorID, Class, Heat, Owner, Latitude, Longitude, CurrentLocation, Grade,
                    Number, Manufacturer, BoolToSqlValue(Cut), BoolToSqlValue(Verify), 1000, Alert, JobID, string.Empty,
                    MillLength, Station, string.Empty, BoolToSqlValue(Bend), 1, BoolToSqlValue(InspectionPassed));
            }
        }

        public string BoolToSqlValue(bool? b)
        {
            if (b == null)
                return "NULL";
            else
                return b.Value ? "1" : "0";
        }
    }
}
