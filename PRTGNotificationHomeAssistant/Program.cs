using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using System.Data.SQLite;
using System.Drawing;
using System.Net;
using Newtonsoft.Json;

namespace PRTGNotificationHomeAssistant
{
    class Program
    {
        class Options
        {
            //[Option('r', "read", Required = true, HelpText = "Input files to be processed.")]
            //public IEnumerable<string> InputFiles { get; set; }

            // Omitting long name, defaults to name of property, ie "--verbose"
            [Option(
              Default = false,
              HelpText = "Prints all messages to standard output.")]
            public bool Verbose { get; set; }

            [Option('k', "key", Required = true, HelpText = "Home Assistant key (long random string)")]
            public string Key { get; set; }

            [Option('u', "url", Required = true, HelpText = "The base URL of the Home Assistant API")]
            public string Url { get; set; }

            [Option('l', "light", Required = true, HelpText = "The name of the light that should react (like 'light.noc')")]
            public string Light { get; set; }

            [Option('c', "color", Required = true, HelpText = "The color of the status %colorofstate")]
            public string Color { get; set; }

            [Option('s', "sensor", Required = true, HelpText = "The id of the sensor %sensorid")]
            public string Sensor { get; set; }

            //[Value(0, MetaName = "offset", HelpText = "File offset.")]
            //public long? Offset { get; set; }
        }
        class Color
        {
            public string Name { get; set; }
            public string Hex { get; set; }
            public int R { get; set; }
            public int G { get; set; }
            public int B { get; set; }
            public int Brightness { get; set; }
            public bool Blink { get; set; }
        }
        // from php:
        //$payload = array("entity_id" => LIGHT, "brightness" => $brightness, "rgb_color" => $colorparts);
        //if ($colorparts[0] > 210){
        //  // when prtg red, blink
        //  $payload["flash"] = "long";
        //}
        class HomeAssistantPayload
        {
            public string entity_id { get; set; }
            public int brightness { get; set; }
            public int[] rgb_color { get; set; }
            public string flash { get; set; }
        }

        private static List<Color> colors;
        private static readonly string sqliteFile = "database.sqlite";
        
        static void Main(string[] args)
        {
            // colors:
            //    # hex   | name   | R | G | B
            //    # b4cc38|green   |180|204| 56
            //    # d71920|red     |215| 25| 32
            //    # ffcb05|orange  |255|203|  5
            //    # 447fc1|Blue    | 68|127|193
            //    the brightness is also in use for priority.
            colors = new List<Color>
            {
                new Color { Name = "Green",  R = 180, G = 204, B = 56,  Hex = "#b4cc38", Brightness = 2 },
                new Color { Name = "Red",    R = 215, G = 25,  B = 32,  Hex = "#d71920", Brightness = 255, Blink = true },
                new Color { Name = "Orange", R = 255, G = 203, B = 5,   Hex = "#ffcb05", Brightness = 120, Blink = true },
                new Color { Name = "Blue",   R = 68,  G = 127, B = 193, Hex = "#447fc1", Brightness = 50 },
                //new Color { Name = "Grey", R = 180, G = 204, B = 56, Hex = "#b4cc38" },

            };

            var errors = new List<CommandLine.Error>();
            CommandLine.Parser.Default.ParseArguments<Options>(args)
             .WithParsed<Options>(opts => RunOptionsAndReturnExitCode(opts));
        }

        private static int[] GenerateRgb(string backgroundColor)
        {
            System.Drawing.Color color = ColorTranslator.FromHtml(backgroundColor);
            return new int []  { Convert.ToInt16(color.R), Convert.ToInt16(color.G), Convert.ToInt16(color.B) }; 
        }

        private static void RunOptionsAndReturnExitCode(Options opts)
        {
            bool createTables = false;
            List<string> colorsinuse = new List<string>() ;
            if (!File.Exists(sqliteFile))
            {
                SQLiteConnection.CreateFile(sqliteFile);
                createTables = true;
            }
            SQLiteConnection m_dbConnection = new SQLiteConnection("Data Source=" + sqliteFile + "; Version=3;");
            m_dbConnection.Open();
            using (SQLiteCommand cmd = new SQLiteCommand(m_dbConnection))
            {
                // create table if file didn't exists
                if (createTables)
                {
                    cmd.CommandText = "CREATE TABLE IF NOT EXISTS 'checks' (sensor INTEGER, color TEXT, PRIMARY KEY(sensor ASC));";
                    cmd.ExecuteNonQuery();
                }
                // insert or replace new data
                cmd.CommandText = "INSERT or REPLACE INTO checks VALUES(@sensor,@color); ";
                cmd.Prepare();
                cmd.Parameters.AddWithValue("@sensor", opts.Sensor);
                cmd.Parameters.AddWithValue("@color", opts.Color);
                cmd.ExecuteNonQuery();
                // get list of all different colors.
                cmd.CommandText = "select color,count(*) as c from checks group by color;";
                SQLiteDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {

                    //Console.WriteLine("Color: " + reader["color"] + "\tScore: " + reader["c"]);
                    colorsinuse.Add(reader["color"].ToString());
                }
                // close db, so it is available for other requests.
                m_dbConnection.Close();
                GC.Collect();
                GC.WaitForPendingFinalizers();
                // so now we have a couple of colors in our array.
                Color pushedcolor = colors[0]; // default green
                foreach (String c in colorsinuse)
                {
                    Console.Write("Color: " + c);
                    Color selectedColor = colors.Where(i => i.Hex == c).FirstOrDefault();
                    Console.WriteLine(" " + selectedColor.Name);
                    if (selectedColor.Brightness > pushedcolor.Brightness)
                    {
                        pushedcolor = selectedColor;
                    }
                }
                // we now have the most important color. So we now pushed that to the api.
                HomeAssistantPayload hap = new HomeAssistantPayload
                {
                    entity_id = opts.Light,
                    brightness = pushedcolor.Brightness,
                    rgb_color = GenerateRgb(pushedcolor.Hex),
                    flash = null
                };
                if (pushedcolor.Blink)
                {
                    hap.flash = "long";
                }
                string json = JsonConvert.SerializeObject(hap, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                try
                {
                    var httpWebRequest = (HttpWebRequest)WebRequest.Create(opts.Url + "/api/services/light/turn_on");
                    httpWebRequest.ContentType = "application/json";
                    httpWebRequest.Method = "POST";
                    httpWebRequest.Headers.Add("Authorization", "Bearer " + opts.Key);
                    using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                    {
                        streamWriter.Write(json);
                        streamWriter.Flush();
                        streamWriter.Close();
                    }
                    Console.WriteLine();
                    Console.WriteLine(json);
                    Console.WriteLine(" -> ");
                    Console.WriteLine(httpWebRequest.Address);
                    var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                    {
                        var result = streamReader.ReadToEnd();
                        Console.WriteLine(result);
                    }
                }catch(Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }


            }
            


            //Console.WriteLine(opts.Key);
        }

        public static void Log(string logMessage, int level = 0)
        {
            StreamWriter w = File.AppendText("log.txt");
            if (level == 0)
            {
                w.Write("Log : ");
            }
            else if (level == 1)
            {
                w.Write("ERROR : ");
            }
            w.Write($" {DateTime.Now.ToLongDateString()} - {DateTime.Now.ToLongTimeString()}");
            w.Write("  :");
            w.WriteLine($"  :{logMessage}");

            Console.WriteLine("Bericht:");
            Console.WriteLine(logMessage);
            Console.ReadLine();
        }

    }
}
