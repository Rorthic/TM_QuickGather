using StudioForge.Engine.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace QuickGather
{
    class ConfigFile
    {
        #region Member Variables
        public string playerMsg = string.Empty;
        public string pathFileName;
        Dictionary<string, string> dict;

        #endregion

        #region Constructors

        /// <summary>
        /// The default Constructor.
        /// </summary>
        public ConfigFile(string fileName)
        {
            //check if directoy exists, if not create it
            System.IO.Directory.CreateDirectory(FileSystem.RootPath + QuickGatherMod.Path + "config");
            pathFileName = FileSystem.RootPath + QuickGatherMod.Path + "config"+ Path.DirectorySeparatorChar + fileName; 
            dict = new Dictionary<string, string>();
        }

        #endregion

        #region Properties
        #endregion

        #region Functions
        public void SaveConfig()
        {

            using (StreamWriter outputFile = new StreamWriter(pathFileName))
            {
                //write to file from dictionary
                foreach (var entry in dict)
                    outputFile.WriteLine("{0}{1}{2}", entry.Key, "=", entry.Value);
            }

        }
        void AppendConfig(string key, string value)
        {

            using (StreamWriter outputFile = File.AppendText(pathFileName))
            {
                //append to file 
                outputFile.WriteLine("{0}{1}{2}", key, "=", value);

            }

        }

        public void LoadConfig()
        {
            try
            {   // Open the text file using a stream reader.
                using (StreamReader sr = new StreamReader(pathFileName))
                {
                    //// Read the stream to a string

                    string[] lines = File.ReadAllLines(pathFileName);

                    // Get the position of the = sign within each line
                    var pairs = lines.Select(l => new { Line = l, Pos = l.IndexOf("=") });

                    // Build a dictionary of key/value pairs by splitting the string at the = sign
                    dict = pairs.ToDictionary(p => p.Line.Substring(0, p.Pos), p => p.Line.Substring(p.Pos + 1));


                }
            }
            catch (Exception e)
            {

                playerMsg = "Config: The file could not be read: " + e.Message;


            }


        }

        public bool GetBoolEntry(string key)
        {
            string sReturn = "ERROR";
            if (dict.ContainsKey(key))
            {
                sReturn = dict[key];
            }
            //else
            //{
            //    playerMsg = key + " missing from config";
            //}
            //dict.TryGetValue(key, out sReturn); //causes an error due to missing key value being uninitialized
            
            return StringToBool(sReturn);

        }

        public int GetIntEntry(string key)
        {
            string sReturn = "ERROR";
            if (dict.ContainsKey(key))
            {
                sReturn = dict[key];
            }
            else
            {
                return 0;
            }

            int iReturn = 0;
                
            // ToInt32 can throw FormatException or OverflowException.
            try
            {
                iReturn = Convert.ToInt32(sReturn);
            }
            catch (FormatException e)
            {
                playerMsg = "Input string for " + key + " is not a sequence of digits." + e;
            }
            catch (OverflowException e)
            {
                playerMsg = "The number for " + key + " cannot fit in an Int32. " + e ;
            }
            return iReturn;
        }

        bool StringToBool(String s)
        {
            s = s.ToLower();
            if (s == "true") return true;
            else return false;
        }

        public void CreateEmptyConfig()
        {
            dict = new Dictionary<string, string>();
            SaveConfig();
        }

        public void AddConfigKey(string key, string value)
        {
            if (!dict.ContainsKey(key))
            {
                AppendConfig(key, value);
            }
            else
            {
                playerMsg = key + " already exists";
            }
        }


        public void UpdateKey(string key, string value)
        {
            if (dict.ContainsKey(key))
            {
                dict[key] = value;
            }
            else
            {
                playerMsg = key + " doesn't  exists";
            }
        }

        public bool ContainsEntry(string key)
        {
            return dict.ContainsKey(key);

        }

        #endregion

        #region Enums
        #endregion




    }
}
