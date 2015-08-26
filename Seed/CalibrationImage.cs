using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seed.localhost
{
    public partial class CalibrationImage
    {
        public string BulkInsertLine
        {
            get
            {
                return string.Format(
                    "{0}|" +  //[Calibration_Image_ID]
                    "{1}|" +  //[Image]
                    "{2}|" +  //[Created_Date]
                    "{3}",    //[Last_Updated]
                    CalibrationImageID, "00", CreationDate.Formatted(), LastUpdated.Formatted());
            }
        }
    }
}
