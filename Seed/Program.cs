using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;
using Seed.localhost;

namespace Seed
{
    class Program
    {
        public class CommandLineArgs
        {
            public enum Operation { Refresh, Pipe, Weld, Map };

            [Option('d', "database", Required = true, HelpText = "Name of the database.")]
            public string DatabaseName { get; set; }

            [Option('v', "vendor", Required = true, HelpText = "The vendor ID.")]
            public long VendorID { get; set; }

            [Option('j', "job", Required = true, HelpText = "The Job ID.")]
            public long JobID { get; set; }

            [Option('p', "pipes", Required = true, HelpText = "Number of pipes")]
            public int NumPipes { get; set; }

            [Option('w', "welds", Required = true, HelpText = "Number of welds")]
            public int NumWelds { get; set; }

            [Option('l', "loads", Required = true, HelpText = "Number of loads")]
            public int NumLoads { get; set; }
        }

        private static CommandLineArgs parsedArgs;
        private static long nextID = 0;
        private static Random rand = new Random();
        private const int NUM_WELD_PASSES = 3;
        private const int NUM_WELD_INSPECTIONS = 5;
        private const int NUM_PIPES_PER_LOAD = 50;

        private const string PIPE_FILE = "pipes.txt";
        private const string PIPE_BARCODE_FILE = "barcodes.txt";
        private const string PIPE_STENCIL_FILE = "pipestencils.txt";
        private const string PIPE_IMAGE_FILE = "pipeimages.txt";
        private const string WELD_FILE = "welds.txt";
        private const string WELD_PASS_FILE = "passes.txt";
        private const string WELD_PASS_DOC_FILE = "passdocs.txt";
        private const string WELD_PASS_TYPE_FILE = "passtypes.txt";
        private const string WELD_INSP_FILE = "inspections.txt";
        private const string WELD_INSP_DOC_FILE = "inspdocs.txt";
        private const string LOAD_FILE = "loads.txt";
        private const string LOAD_DOC_FILE = "loaddocs.txt";
        private const string PLM_FILE = "plm.txt";
        private const string REF_DOC_FILE = "refdocs.txt";
        private const string CAL_IMAGE_FILE = "calimages.txt";

        private static List<CalibrationImage> calibrationImages = new List<CalibrationImage>();

        static void Main(string[] args)
        {
            parsedArgs = new CommandLineArgs();
            bool ok = CommandLine.Parser.Default.ParseArguments(args, parsedArgs);

            if (!ok)
            {
                HelpText help = HelpText.AutoBuild(parsedArgs);
                throw new Exception(string.Format("Invalid command line args: {0}", help.ToString()));
            }

            List<Pipe> pipes = CreatePipes();
            List<long> pipeIDs = pipes.Select(p => p.PipeID).ToList();
            CreateCalibrationImages();
            List<Weld> welds = CreateWelds(pipeIDs);
            List<Load> loads = CreateLoads(pipeIDs);
            List<ReferenceDocument> refDocs = CreateReferenceDocuments(pipes, welds, loads);
            OutputInsertStatements(pipes, welds, loads, refDocs);
            OutputSqlScript();

            Console.WriteLine("done, press a key");
            Console.ReadKey();
        }


        private static long GetNextID()
        {
            if (nextID == 0)
                nextID = GetRandomLong();

            return nextID++;
        }


        private static List<Pipe> CreatePipes()
        {
            List<Pipe> pipes = new List<Pipe>();

            for (int i = 0; i < parsedArgs.NumPipes; i++)
            {
                Pipe pipe = GeneratePipe();
                pipes.Add(pipe);
            }

            return pipes;
        }


        private static void CreateCalibrationImages()
        {
            for (int i = 0; i < 10; i++)
                calibrationImages.Add(GenerateCalibrationImage());
        }


        private static List<Weld> CreateWelds(List<long> pipeIDs)
        {
            List<Weld> welds = new List<Weld>();

            using (StreamWriter sw = File.CreateText(WELD_FILE))
            {
                int p = 0;

                for (int i = 0; i < parsedArgs.NumWelds; i++)
                {
                    long pipe1ID = pipeIDs[p];
                    p = (p + 1) % pipeIDs.Count;
                    long pipe2ID = pipeIDs[p];
                    Weld weld = GenerateWeld(pipe1ID, pipe2ID);
                    welds.Add(weld);
                }
            }

            foreach (Weld weld in welds)
            {
                List<WeldPart> parts = new List<WeldPart>();

                for (int j = 0; j < NUM_WELD_PASSES; j++)
                {
                    WeldPart part = GenerateWeldPart(weld.WeldID);
                    parts.Add(part);
                }

                weld.WeldParts = parts.ToArray();
            }

            foreach (Weld weld in welds)
            {
                List<WeldInspection> inspections = new List<WeldInspection>();

                for (int j = 0; j < NUM_WELD_INSPECTIONS; j++)
                {
                    WeldInspection insp = GenerateWeldInspection(weld.WeldID);
                    inspections.Add(insp);
                }

                weld.WeldInspections = inspections.ToArray();
            }

            return welds;
        }


        private static List<Load> CreateLoads(List<long> pipeIDs)
        {
            List<Load> loads = new List<Load>();

            for (int i = 0; i < parsedArgs.NumLoads; i++)
            {
                Load load = GenerateLoad();
                loads.Add(load);
            }

            return loads;
        }


        private static List<ReferenceDocument> CreateReferenceDocuments(List<Pipe> pipes, List<Weld> welds, List<Load> loads)
        {
            List<ReferenceDocument> refDocs = new List<ReferenceDocument>();

            foreach (Weld weld in welds)
            {
                foreach(WeldPart part in weld.WeldParts)
                {
                    foreach(WeldPartDocument doc in part.WeldPartDocuments)
                        if (!string.IsNullOrEmpty(doc.DocumentRefNumber) && rand.Next() % 2 == 0)
                            refDocs.Add(GenerateReferenceDocument(doc.DocumentRefNumber));

                    if (!string.IsNullOrEmpty(part.CalibrationRefNum1) && rand.Next() % 2 == 0)
                    {
                        refDocs.Add(GenerateReferenceDocument(part.CalibrationRefNum1));
                    }

                    if (!string.IsNullOrEmpty(part.CalibrationRefNum2) && rand.Next() % 2 == 0)
                    {
                        refDocs.Add(GenerateReferenceDocument(part.CalibrationRefNum2));
                    }
                }

                foreach (WeldInspection insp in weld.WeldInspections)
                {
                    foreach (WeldInspectionDocument doc in insp.WeldInspectionDocuments)
                    {
                        if (!string.IsNullOrEmpty(doc.DocumentRefNumber) && rand.Next() % 2 == 0)
                            refDocs.Add(GenerateReferenceDocument(doc.DocumentRefNumber));
                    }
                }
            }

            return refDocs;
        }


        private static Int64 GetRandomLong()
        {
            byte[] buffer = new byte[sizeof(Int64)];
            rand.NextBytes(buffer);
            return Math.Abs(BitConverter.ToInt64(buffer, 0));
        }


        private static Pipe GeneratePipe()
        {
            long pipeID = GetNextID();

            Pipe pipe = new Pipe()
            {
                PipeID = pipeID,
                Barcode = pipeID + "bc",
                Number = pipeID + "nm",
                VendorID = parsedArgs.VendorID,
                JobID = parsedArgs.JobID,
                Coating = "ARO-L",
                Diameter = "10",
                CurrentLocation = "Austin, TX",
                Latitude = "30.25",
                Longitude = "97.75",
                Length = "60",
                Status = "IN",
                Type = "HFW",
                Wall = "0.365",
                Heat = "ZM1610",
                Grade = "X52",
                Manufacturer = "SeAH",
                Image = string.Empty,
                Thumbnail = string.Empty,
                Issue = true,
                Class = "class",
                Owner = "owner",
                Alert = "alert",
                Cut = false,
                Bend = false,
                Verify = false,
                MillLength = "1",
                Station = "Station X",
                LastUpdated = DateTime.Now.ToString(),
                NoteCount = 0,
                InspectionPassed = true,
                ImageData = new JointImageData[0]
            };

            pipe.AdditionalBarcodes = new string[]
            {
                pipe.Barcode + "_1",
                pipe.Barcode + "_2",
                pipe.Barcode + "_3",
                pipe.Barcode + "_4",
                pipe.Barcode + "_5",
                pipe.Barcode + "_6"
            };

            //every pipe has 1 stencil image
            pipe.StencilData = new JointStencilData[] { GenerateJointStencilImage(pipe) };

            //roughly 1 in every 30 pipes has a damage image
            if (rand.Next(0, 30) == 0)
                pipe.ImageData = new JointImageData[] { GenerateJointImage(pipe) };

            return pipe;
        }


        private static JointStencilData GenerateJointStencilImage(Pipe pipe)
        {
            return new JointStencilData()
            {
                JointStencilDataID = Guid.NewGuid(),
                PipeID = pipe.PipeID,
                ContentType = "image/png",
                Image = string.Empty,
                JobID = pipe.JobID,
                ReferenceNumber = "somerefnum",
                LastUpdated = DateTime.Now
            };
        }


        private static JointImageData GenerateJointImage(Pipe pipe)
        {
            return new JointImageData()
            {
                JointImageDataID = Guid.NewGuid(),
                PipeID = pipe.PipeID,
                ContentType = "image/png",
                Image = string.Empty,
                JobID = pipe.JobID,
                ReferenceNumber = "somerefnum",
                LastUpdated = DateTime.Now
            };
        }


        private static Weld GenerateWeld(long pipe1ID, long pipe2ID)
        {
            long weldID = GetNextID();

            Weld weld = new Weld()
            {
                WeldID = weldID,
                JobID = parsedArgs.JobID,
                Pipe1ID = pipe1ID,
                Pipe2ID = pipe2ID,
                WeldBarcode = weldID + "_bc",
                WeldNumber = weldID + "_nm",
                WpsNumber = weldID + "wps",
                RepairWpsNumber = weldID + "_rwps",
                Longitude = "30.25",
                Latitude = "97.75",
                Station = "Station W",
                CurrentLocation = "Springfield, OR",
                CreationDate = DateTime.Now,
                LastUpdated = DateTime.Now,
                Status = WeldStatus.Active,
                Repaired = false,
                Type = "Fillet",
                ParentWeldID = null,
                WeldCoating = "FBE",
                WelderFirstName = "Rock",
                WelderLastName = "Strongo"
            };

            return weld;
        }


        private static WeldPart GenerateWeldPart(long weldID)
        {
            long weldPartID = GetNextID();

            WeldPart weldPart = new WeldPart()
            {
                WeldPartID = weldPartID,
                WeldID = weldID,
                JobID = parsedArgs.JobID,
                CreatedDate = DateTime.Now,
                LastUpdated = DateTime.Now,
                MachineBugID1 = "machine bug id 1",
                MachineBugID2 = "machine bug id 2",
                MachineID = "machine id",
                Position = "left",
                Position2 = "right",
                WelderName = "Rock Strongo",
                VisualInspectionPassed = true,
                Types = new WeldPartType[]
                {
                    new WeldPartType() { Type = "Branch", WeldPartID = weldPartID, WeldPartTypeID = GetNextID() },
                    new WeldPartType() { Type = "Butt", WeldPartID = weldPartID, WeldPartTypeID = GetNextID() }
                }
            };

            weldPart.WeldPartDocuments = new WeldPartDocument[1];

            //calibration image will either be a hard image or a reference number
            if (rand.Next() % 2 == 0)
                weldPart.CalibrationImage1ID = calibrationImages[rand.Next(0, calibrationImages.Count)].CalibrationImageID;
            else
            {
                weldPart.CalibrationRefNum1 = "refnum_" + GetNextID();
            }

            //calibration image will either be a hard image or a reference number
            if (rand.Next() % 2 == 0)
                weldPart.CalibrationImage2ID = calibrationImages[rand.Next(0, calibrationImages.Count)].CalibrationImageID;
            else
            {
                weldPart.CalibrationRefNum2 = "refnum_" + GetNextID();
            }

            //half of the documents will be reference docs
            if (rand.Next() % 2 == 0)
            {
                string refNum = "refnum_" + GetNextID();
                weldPart.WeldPartDocuments[0] = GenerateWeldPartDoc(weldPartID, refNum);
            }
            else
            {
                weldPart.WeldPartDocuments[0] = GenerateWeldPartDoc(weldPartID);
            }

            return weldPart;
        }


        private static WeldPartDocument GenerateWeldPartDoc(long weldPartID)
        {
            return new WeldPartDocument()
            {
                WeldPartDocumentID = GetNextID(),
                WeldPartID = weldPartID,
                JobID = parsedArgs.JobID,
                ContentType = "image/png",
                DocumentImage = "1234",
                CreatedDate = DateTime.Now,
                LastUpdated = DateTime.Now
            };
        }


        private static WeldPartDocument GenerateWeldPartDoc(long weldPartID, string refNum)
        {
            WeldPartDocument doc = GenerateWeldPartDoc(weldPartID);
            doc.DocumentRefNumber = refNum;
            doc.DocumentImage = string.Empty;
            doc.ContentType = null;
            return doc;
        }


        private static CalibrationImage GenerateCalibrationImage()
        {
            return new CalibrationImage()
            {
                CalibrationImageID = GetNextID(),
                CreationDate = DateTime.Now,
                LastUpdated = DateTime.Now,
                Image = "1234"
            };
        }


        private static WeldInspection GenerateWeldInspection(long weldID)
        {
            long inspectionID = GetNextID();

            WeldInspection inspection = new WeldInspection()
            {
                WeldInspectionID = inspectionID,
                WeldID = weldID,
                JobID = parsedArgs.JobID,
                DateCreated = DateTime.Now,
                LastUpdated = DateTime.Now,
                InspectionType = "NDE",
                Spread = "spread 1",
                Rig = "rig 1",
                Crew = "crew 1",
                DatePerformed = DateTime.Now,
                InspectorName = "Lance Uppercut",
                Passed = true,
                Delayed = false
            };

            inspection.WeldInspectionDocuments = new WeldInspectionDocument[1];

            //half of the documents will be reference docs
            if (rand.Next() % 2 == 0)
            {
                string refNum = "refnum_" + GetNextID();
                inspection.WeldInspectionDocuments[0] = GenerateWeldInspectionDoc(inspectionID, refNum);
            }
            else
            {
                inspection.WeldInspectionDocuments[0] = GenerateWeldInspectionDoc(inspectionID);
            }

            return inspection;
        }


        private static WeldInspectionDocument GenerateWeldInspectionDoc(long weldInspID)
        {
            return new WeldInspectionDocument()
            {
                WeldInspectionDocumentID = GetNextID(),
                WeldInspectionID = weldInspID,
                JobID = parsedArgs.JobID,
                DocumentImage = "1234",
                ContentType = "image/png",
                CreatedDate = DateTime.Now,
                LastUpdated = DateTime.Now
            };
        }


        private static WeldInspectionDocument GenerateWeldInspectionDoc(long weldInspID, string refNum)
        {
            WeldInspectionDocument doc = GenerateWeldInspectionDoc(weldInspID);
            doc.DocumentRefNumber = refNum;
            doc.DocumentImage = string.Empty;
            doc.ContentType = null;
            return doc;
        }


        public static Load GenerateLoad()
        {
            long loadID = GetNextID();

            Load load = new Load()
            {
                LoadID = loadID,
                Number = loadID + "nm",
                VendorID = parsedArgs.VendorID,
                JobID = parsedArgs.JobID,
                Status = "IN",
                Shipper = "Pipe Bros",
                Receiver = "Rock Strongo",
                Type = "T",
                CreationDate = DateTime.Now.ToString(),
                LastUpdated = DateTime.Now.ToString(),
                Vessel = "Test Vessel",
                Carrier = "Test Carrier",
                OriginAddress1 = "123 Fake St.",
                OriginAddress2 = "Apt 99",
                OriginCity = "Springfield",
                OriginState = "OR",
                OriginZip = "12345",
                DestAddress1 = "0 No Way",
                DestAddress2 = "",
                DestCity = "Hometown",
                DestState = "XX",
                DestZip = "99999",
                LastLocation = "Anywhere, USA",
                DriverName = "Carl Carlson",
                DriverCell = "012-345-6789",
                Latitude = 12.45m,
                Longitude = 56.78m,
                Customer = "Pipe Co"
            };

            List<LoadImage> images = new List<LoadImage>();
            images.Add(GenerateLoadImage(load.LoadID, LoadImageType.Identification));

            if (rand.Next(0, 10) == 0)
                images.Add(GenerateLoadImage(load.LoadID, LoadImageType.Standard));

            load.Images = images.ToArray();

            return load;
        }


        private static LoadImage GenerateLoadImage(long loadID, LoadImageType type)
        {
            return new LoadImage()
            {
                LoadImageID = GetNextID(),
                LoadID = loadID,
                Type = type,
                ContentType = "image/png",
                Image = string.Empty,
                ReferenceNumber = "loadimagerefnum",
                LastUpdated = DateTime.Now
            };
        }


        private static ReferenceDocument GenerateReferenceDocument(string refNum)
        {
            return new ReferenceDocument()
            {
                ReferenceDocumentID = GetNextID(),
                JobID = parsedArgs.JobID,
                DateCreated = DateTime.Now,
                LastUpdated = DateTime.Now,
                ReferenceNumber = refNum,
                Document = "1234",
                ContentType = "image/png"
            };
        }


        private static MainService GetService()
        {
            MainService service = new MainService();
            service.ServiceHeaderValue = new ServiceHeader() { Username = "test", Password = "PT=Awesome", ApiVersion = "release7" };
            service.Timeout = 100000000;
            return service;
        }


        private static void OutputInsertStatements(List<Pipe> pipes, List<Weld> welds, List<Load> loads, List<ReferenceDocument> refDocs)
        {
            /*
             * Output pipe statements
             */

            using (StreamWriter sw = File.CreateText(PIPE_FILE))
            {
                foreach (Pipe pipe in pipes)
                    sw.WriteLine(pipe.BulkInsertLine);
            }

            using (StreamWriter sw = File.CreateText(PIPE_BARCODE_FILE))
            {
                foreach (Pipe pipe in pipes)
                    foreach (string barcode in pipe.AdditionalBarcodes)
                        sw.WriteLine(string.Format("{0}|{1}||{2}|{3}", pipe.PipeID, barcode, "", ""));
            }

            using (StreamWriter sw = File.CreateText(PIPE_STENCIL_FILE))
            {
                foreach (Pipe pipe in pipes)
                    foreach (JointStencilData stencil in pipe.StencilData)
                        sw.WriteLine(stencil.BulkInsertLine);
            }

            using (StreamWriter sw = File.CreateText(PIPE_IMAGE_FILE))
            {
                foreach (Pipe pipe in pipes)
                    foreach (JointImageData image in pipe.ImageData)
                        sw.WriteLine(image.BulkInsertLine);
            }


            /*
             * Output weld statements
             */

            using (StreamWriter sw = File.CreateText(WELD_FILE))
            {
                foreach (Weld weld in welds)
                    sw.WriteLine(weld.BulkInsertLine);
            }

            using (StreamWriter sw = File.CreateText(WELD_PASS_FILE))
            {
                foreach (Weld weld in welds)
                    foreach(WeldPart part in weld.WeldParts)
                        sw.WriteLine(part.BulkInsertLine);
            }

            using (StreamWriter sw = File.CreateText(WELD_PASS_TYPE_FILE))
            {
                foreach (Weld weld in welds)
                    foreach (WeldPart part in weld.WeldParts)
                        foreach (WeldPartType type in part.Types)
                            sw.WriteLine(type.BulkInsertLine);
            }

            using (StreamWriter sw = File.CreateText(WELD_PASS_DOC_FILE))
            {
                foreach (Weld weld in welds)
                    foreach (WeldPart part in weld.WeldParts)
                        foreach (WeldPartDocument doc in part.WeldPartDocuments)
                            sw.WriteLine(doc.BulkInsertLine);
            }

            using (StreamWriter sw = File.CreateText(WELD_INSP_FILE))
            {
                foreach (Weld weld in welds)
                    foreach (WeldInspection insp in weld.WeldInspections)
                        sw.WriteLine(insp.BulkInsertLine);
            }

            using (StreamWriter sw = File.CreateText(WELD_INSP_DOC_FILE))
            {
                foreach (Weld weld in welds)
                    foreach (WeldInspection insp in weld.WeldInspections)
                        foreach (WeldInspectionDocument doc in insp.WeldInspectionDocuments)
                            sw.WriteLine(doc.BulkInsertLine);
            }


            /*
             * Output load statements
             */

            using (StreamWriter sw = File.CreateText(LOAD_FILE))
            {
                foreach (Load load in loads)
                    sw.WriteLine(load.BulkInsertLine);
            }

            using (StreamWriter sw = File.CreateText(LOAD_DOC_FILE))
            {
                foreach (Load load in loads)
                    foreach (LoadImage image in load.Images)
                        sw.WriteLine(image.BulkInsertLine);
            }

            using (StreamWriter sw = File.CreateText(PLM_FILE))
            {
                foreach (Load load in loads)
                {
                    for (int j = 0; j < NUM_PIPES_PER_LOAD; j++)
                    {
                        //select a random pipe to add to the load
                        long pipeID = pipes[j % pipes.Count].PipeID;

                        /* Pipe_Load_Map_ID,Load_ID,Pipe_ID,Deleted,Last_Updated,Pipe_Status,Modified_By_User_ID,Billable_Txn_Details_ID */
                        sw.WriteLine("|{0}|{1}|0|{2}|||", load.LoadID, pipeID, DateTime.Now.Formatted());
                    }
                }
            }


            /*
             * Output reference documents
             */
            using (StreamWriter sw = File.CreateText(REF_DOC_FILE))
            {
                foreach (ReferenceDocument refDoc in refDocs)
                    sw.WriteLine(refDoc.BulkInsertLine);
            }


            /*
             * Output calibration images
             */
            using (StreamWriter sw = File.CreateText(CAL_IMAGE_FILE))
            {
                foreach (CalibrationImage calImage in calibrationImages)
                    sw.WriteLine(calImage.BulkInsertLine);
            }
        }


        private static void OutputSqlScript()
        {
            using (StreamWriter sw = File.CreateText("insert.sql"))
            {
                List<Tuple<string, string>> data = new List<Tuple<string,string>>();
                data.Add(new Tuple<string,string>("Pipes", PIPE_FILE));
                data.Add(new Tuple<string,string>("Joint_Image_Data", PIPE_IMAGE_FILE));
                data.Add(new Tuple<string,string>("Joint_Stencil_Data", PIPE_STENCIL_FILE));
                data.Add(new Tuple<string, string>("Pipe_Barcodes", PIPE_BARCODE_FILE));
                data.Add(new Tuple<string, string>("Welds", WELD_FILE));
                data.Add(new Tuple<string, string>("Weld_Parts", WELD_PASS_FILE));
                data.Add(new Tuple<string, string>("Weld_Part_Types", WELD_PASS_TYPE_FILE));
                data.Add(new Tuple<string, string>("Weld_Part_Documents", WELD_PASS_DOC_FILE));
                data.Add(new Tuple<string, string>("Weld_Inspections", WELD_INSP_FILE));
                data.Add(new Tuple<string, string>("Weld_Inspection_Documents", WELD_INSP_DOC_FILE));
                data.Add(new Tuple<string, string>("Loads", LOAD_FILE));
                data.Add(new Tuple<string, string>("Load_Images", LOAD_DOC_FILE));
                data.Add(new Tuple<string, string>("Pipe_Load_Map", PLM_FILE));
                data.Add(new Tuple<string, string>("Reference_Documents", REF_DOC_FILE));
                data.Add(new Tuple<string, string>("Calibration_Images", CAL_IMAGE_FILE));

                string nl = System.Environment.NewLine;
                FileInfo file = new FileInfo(System.Reflection.Assembly.GetExecutingAssembly().Location);
                
                foreach(Tuple<string, string> d in data)
                    sw.WriteLine(
                        "BULK INSERT {0}" + nl +
                        "FROM '{1}\\{2}'" + nl +
                        "WITH" + nl +
                        "(" + nl +
                        "FIELDTERMINATOR = '|'," + nl +
                        "ROWTERMINATOR = '\\n'" + nl +
                        ")" + nl +
                        "GO" + nl + nl,
                        d.Item1, file.Directory, d.Item2);
            }
        }
    }
}
