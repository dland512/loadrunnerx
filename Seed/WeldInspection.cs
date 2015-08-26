using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seed.localhost
{
    public partial class WeldInspection
    {
        public string InsertStatement
        {
            get
            {
                string insert = @"
                    INSERT INTO Weld_Inspections (
                        Weld_Inspection_ID,
                        Weld_ID,
                        Inspector_ID,
                        Date_Performed,
                        Last_Updated,
                        Date_Created,
                        Inspection_Type,
                        Modified_By_User_ID,
                        Billable_Txn_Details_ID,
                        Delayed,
                        Passed,
                        Spread,
                        Rig,
                        Crew
                    )
                    VALUES(
                        " + WeldInspectionID + @",
                        " + WeldID + @",
                        NULL,
                        '" + DatePerformed + @"',
                        '" + LastUpdated + @"',
                        '" + DateCreated + @"',
                        '" + InspectionType + @"',
                        1000,
                        NULL,
                        0,
                        1,
                        '" + Spread + @"',
                        '" + Rig + @"',
                        '" + Crew + @"'
                    )";

                return insert;
            }
        }


        public string BulkInsertLine
        {
            get
            {
                return string.Format(
                    "{0}|" +     //Weld_Inspection_ID
                    "{1}|" +     //Weld_ID
                    "|" +        //Inspector_ID
                    "{2}|" +     //Date_Performed
                    "{3}|" +     //Last_Updated
                    "{4}|" +     //Date_Created
                    "nde|" +     //Inspection_Type
                    "|" +        //Modified_By_User_ID
                    "|" +        //Billable_Txn_Details_ID
                    "{5}|" +     //Delayed
                    "{6}|" +     //Passed
                    "{7}|" +     //Spread
                    "{8}|" +     //Rig
                    "{9}",       //Crew
                    WeldInspectionID, WeldID, DateTime.Now.Formatted(), DateTime.Now.Formatted(),
                    DateTime.Now.Formatted(), 0, 1, Spread, Rig, Crew);
            }
        }
    }
}
