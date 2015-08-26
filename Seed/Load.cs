using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seed.localhost
{
    public partial class Load
    {
        public string InsertStatement
        {
            get
            {
                string insert = @"
                    INSERT INTO Loads (
                        Load_ID,
                        Creation_Date,
                        Last_Updated,
                        Number,
                        Status,
                        Type,
                        Short_Date,
                        Total_Footage,
                        Vendor_ID,
                        Shipper,
                        Receiver,
                        Job_ID,
                        Modified_By_User_ID,
                        Billable_Txn_Details_ID,
                        Vessel,
                        Carrier,
                        Last_Location,
                        Origin_Address_1,
                        Origin_Address_2,
                        Origin_City,
                        Origin_State,
                        Origin_Zip,
                        Dest_Address_1,
                        Dest_Address_2,
                        Dest_City,
                        Dest_State,
                        Dest_Zip,
                        Customer,
                        Driver_Name,
                        Driver_Cell,
                        Latitude,
                        Longitude
                    )
                    VALUES(
                        " + LoadID + @",
                        '" + CreationDate + @"',
                        '" + LastUpdated + @"',
                        '" + Number + @"',
                        '" + Status + @"',
                        '" + Type + @"',
                        NULL,
                        NULL,
                        " + VendorID + @",
                        '" + Shipper + @"',
                        '" + Receiver + @"',
                        " + JobID + @",
                        1000,
                        NULL,
                        '" + Vessel + @"',
                        '" + Carrier + @"',
                        '" + LastLocation + @"',
                        '" + OriginAddress1 + @"',
                        '" + OriginAddress2 + @"',
                        '" + OriginCity + @"',
                        '" + OriginState + @"',
                        '" + OriginZip + @"',
                        '" + DestAddress1 + @"',
                        '" + DestAddress2 + @"',
                        '" + DestCity + @"',
                        '" + DestState + @"',
                        '" + DestZip + @"',
                        '" + Customer + @"',
                        '" + DriverName + @"',
                        '" + DriverCell + @"',
                        '" + Latitude + @"',
                        '" + Longitude + @"'
                    )";

                return insert;
            }
        }


        public string BulkInsertLine
        {
            get
            {
                return string.Format(
                    "{0}|" +      //Load_ID
                    "{1}|" +      //Creation_Date
                    "{2}|" +      //Last_Updated
                    "{3}|" +      //Number
                    "{4}|" +      //Status
                    "{5}|" +      //Type
                    "{6}|" +      //Short_Date
                    "|" +         //Total_Footage
                    "{7}|" +      //Vendor_ID
                    "{8}|" +      //Shipper
                    "{9}|" +      //Receiver
                    "{10}|" +     //Job_ID
                    "|" +         //Modified_By_User_ID
                    "|" +         //Billable_Txn_Details_ID
                    "{11}|" +     //Vessel
                    "{12}|" +     //Carrier
                    "{13}|" +     //Last_Location
                    "{14}|" +     //Origin_Address_1
                    "{15}|" +     //Origin_Address_2
                    "{16}|" +     //Origin_City
                    "{17}|" +     //Origin_State
                    "{18}|" +     //Origin_Zip
                    "{19}|" +     //Dest_Address_1
                    "{20}|" +     //Dest_Address_2
                    "{21}|" +     //Dest_City
                    "{22}|" +     //Dest_State
                    "{23}|" +     //Dest_Zip
                    "{24}|" +     //Customer
                    "{25}|" +     //Driver_Name
                    "{26}|" +     //Driver_Cell
                    "{27}|" +     //Latitude
                    "{28}",       //Longitude
                    LoadID,
                    CreationDate,
                    LastUpdated,
                    Number,
                    Status,
                    Type,
                    DateTime.Now.Formatted(),
                    VendorID,
                    Shipper,
                    Receiver,
                    JobID,
                    Vessel,
                    Carrier,
                    LastLocation,
                    OriginAddress1,
                    OriginAddress2,
                    OriginCity,
                    OriginState,
                    OriginZip,
                    DestAddress1,
                    DestAddress2,
                    DestCity,
                    DestState,
                    DestZip,
                    Customer,
                    DriverName,
                    DriverCell,
                    Latitude,
                    Longitude);
            }
        }
    }
}
