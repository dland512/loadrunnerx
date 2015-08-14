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
        private const int NUM_PIPES_PER_LOAD = 100;

        private const string PIPE_FILE = "pipes.txt";
        private const string PIPE_BARCODE_FILE = "barcodes.txt";
        private const string PIPE_STENCIL_FILE = "pipestencils.txt";
        private const string PIPE_IMAGE_FILE = "pipeimages.txt";
        private const string WELD_FILE = "welds.txt";
        private const string WELD_PASS_FILE = "passes.txt";
        private const string WELD_PASS_DOC_FILE = "passdocs.txt";
        private const string WELD_INSP_FILE = "inspections.txt";
        private const string WELD_INSP_DOC_FILE = "inspdocs.txt";
        private const string LOAD_FILE = "loads.txt";
        private const string LOAD_DOC_FILE = "loaddocs.txt";
        private const string PLM_FILE = "plm.txt";

        static void Main(string[] args)
        {
            parsedArgs = new CommandLineArgs();
            bool ok = CommandLine.Parser.Default.ParseArguments(args, parsedArgs);

            if (!ok)
            {
                HelpText help = HelpText.AutoBuild(parsedArgs);
                throw new Exception(string.Format("Invalid command line args: {0}", help.ToString()));
            }

            Stopwatch watch = new Stopwatch();
            watch.Start();

            List<long> pipeIDs = CreatePipes();
            CreateWelds(pipeIDs);
            CreateLoads(pipeIDs);
            OutputSqlScript();

            watch.Stop();
            Console.WriteLine(watch.Elapsed);
            Console.WriteLine("done, press a key");
            Console.ReadKey();
        }


        private static long GetNextID()
        {
            if (nextID == 0)
                nextID = GetRandomLong();

            return nextID++;
        }


        private static List<long> CreatePipes()
        {
            List<Pipe> pipes = new List<Pipe>();

            using (StreamWriter sw = File.CreateText(PIPE_FILE))
            {
                for (int i = 0; i < parsedArgs.NumPipes; i++)
                {
                    Pipe pipe = GeneratePipe();
                    sw.WriteLine(pipe.BulkInsertLine);
                    pipes.Add(pipe);
                }
            }

            using (StreamWriter sw = File.CreateText(PIPE_BARCODE_FILE))
            {
                foreach(Pipe pipe in pipes)
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

            return pipes.Select(p => p.PipeID).ToList();
        }


        private static void CreateWelds(List<long> pipeIDs)
        {
            List<Weld> welds = new List<Weld>();
            Random rand = new Random();
                
            using (StreamWriter sw = File.CreateText(WELD_FILE))
            {
                int p = 0;

                for (int i = 0; i < parsedArgs.NumWelds; i++)
                {
                    long pipe1ID = pipeIDs[p];
                    p = (p + 1) % pipeIDs.Count;
                    long pipe2ID = pipeIDs[p];
                    p = (p + 1) % pipeIDs.Count;
                    Weld weld = GenerateWeld(pipe1ID, pipe2ID);

                    sw.WriteLine(weld.BulkInsertLine);
                    welds.Add(weld);
                }
            }

            List<WeldPart> parts = new List<WeldPart>();

            using (StreamWriter sw = File.CreateText(WELD_PASS_FILE))
            {
                foreach(Weld weld in welds)
                {
                    for (int j = 0; j < NUM_WELD_PASSES; j++)
                    {
                        WeldPart part = GenerateWeldPart(weld.WeldID);
                        sw.WriteLine(part.BulkInsertLine);
                        parts.Add(part);
                    }
                }
            }

            using (StreamWriter sw = File.CreateText(WELD_PASS_DOC_FILE))
            {
                foreach (WeldPart part in parts)
                    foreach (WeldPartDocument doc in part.WeldPartDocuments)
                        sw.WriteLine(doc.BulkInsertLine);
            }

            List<WeldInspection> inspections = new List<WeldInspection>();

            using (StreamWriter sw = File.CreateText(WELD_INSP_FILE))
            {
                foreach (Weld weld in welds)
                {
                    for (int j = 0; j < NUM_WELD_INSPECTIONS; j++)
                    {
                        WeldInspection insp = GenerateWeldInspection(weld.WeldID);
                        sw.WriteLine(insp.BulkInsertLine);
                        inspections.Add(insp);
                    }
                }
            }

            using (StreamWriter sw = File.CreateText(WELD_INSP_DOC_FILE))
            {
                foreach (WeldInspection insp in inspections)
                    foreach (WeldInspectionDocument doc in insp.WeldInspectionDocuments)
                        sw.WriteLine(doc.BulkInsertLine);
            }
        }


        private static void CreateLoads(List<long> pipeIDs)
        {
            Random rand = new Random();
            List<Load> loads = new List<Load>();

            using (StreamWriter sw = File.CreateText(LOAD_FILE))
            {
                for (int i = 0; i < parsedArgs.NumLoads; i++)
                {
                    Load load = GenerateLoad();
                    sw.WriteLine(load.BulkInsertLine);
                    loads.Add(load);
                }
            }

            using (StreamWriter sw = File.CreateText(PLM_FILE))
            {
                foreach(Load load in loads)
                {
                    for (int j = 0; j < NUM_PIPES_PER_LOAD; j++)
                    {
                        long pipeID = pipeIDs[j % pipeIDs.Count];
                        //Pipe_Load_Map_ID,Load_ID,Pipe_ID,Deleted,Last_Updated,Pipe_Status,Modified_By_User_ID,Billable_Txn_Details_ID
                        sw.WriteLine("|{0}|{1}|0|{2}|||", load.LoadID, pipeID, DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
                    }
                }
            }

            using (StreamWriter sw = File.CreateText(LOAD_DOC_FILE))
            {
                foreach (Load load in loads)
                {
                    foreach (LoadImage image in load.Images)
                        sw.WriteLine(image.BulkInsertLine);
                }
            }
        }


        private static Int64 GetRandomLong()
        {
            Random rand = new Random();
            byte[] buffer = new byte[sizeof(Int64)];
            rand.NextBytes(buffer);
            return Math.Abs(BitConverter.ToInt64(buffer, 0));
        }


        private static Pipe GeneratePipe()
        {
            Random rand = new Random();
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
            Random rand = new Random();
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
            Random rand = new Random();
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
                Types = new WeldPartType[] { new WeldPartType() { Type = "Branch", WeldPartID = weldPartID, WeldPartTypeID = GetNextID() } }
            };

            weldPart.WeldPartDocuments = new WeldPartDocument[] { GenerateWeldPartDoc(weldPart.WeldPartID) };

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
                DocumentImage = string.Empty,
                DocumentRefNumber = "weldpartrefnum",
                CreatedDate = DateTime.Now,
                LastUpdated = DateTime.Now
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

            inspection.WeldInspectionDocuments = new WeldInspectionDocument[] { GenerateWeldInspectionDoc(inspection.WeldInspectionID) };
            return inspection;
        }


        private static WeldInspectionDocument GenerateWeldInspectionDoc(long weldInspID)
        {
            return new WeldInspectionDocument()
            {
                WeldInspectionDocumentID = GetNextID(),
                WeldInspectionID = weldInspID,
                JobID = parsedArgs.JobID,
                ContentType = "image/png",
                CreatedDate = DateTime.Now,
                LastUpdated = DateTime.Now,
                DocumentImage = string.Empty,
                DocumentRefNumber = "weldinsprefnum"
            };
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


        private static MainService GetService()
        {
            MainService service = new MainService();
            service.ServiceHeaderValue = new ServiceHeader() { Username = "test", Password = "PT=Awesome", ApiVersion = "release7" };
            service.Timeout = 100000000;
            return service;
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
                data.Add(new Tuple<string, string>("Weld_Part_Documents", WELD_PASS_DOC_FILE));
                data.Add(new Tuple<string, string>("Weld_Inspections", WELD_INSP_FILE));
                data.Add(new Tuple<string, string>("Weld_Inspection_Documents", WELD_INSP_DOC_FILE));
                data.Add(new Tuple<string, string>("Loads", LOAD_FILE));
                data.Add(new Tuple<string, string>("Load_Images", LOAD_DOC_FILE));
                data.Add(new Tuple<string, string>("Pipe_Load_Map", PLM_FILE));

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
