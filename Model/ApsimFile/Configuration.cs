using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Windows.Forms;
using System.Drawing;
using CSGeneral;
using System.IO;
using System.Collections.Specialized;
using System.Reflection;
using Microsoft.Win32;


namespace ApsimFile
   {
   public class Configuration
      {
      private static Configuration Singleton = null;
      protected XmlNode SettingsNode;
      private string SectionName = "ApsimUI";

      protected Configuration()
         {
         // ---------------------------
         // Constructor
         // ---------------------------

         // 1. Find version number
         string SettingsFile = Path.Combine(ApsimDirectory(), "Apsim.xml");
         XmlDocument SettingsDoc = new XmlDocument();
         SettingsDoc.Load(SettingsFile);
         SettingsNode = SettingsDoc.DocumentElement;


         // 2. Update from local data
         SettingsFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        Path.Combine("Apsim", Path.Combine(ApsimVersion(), "Apsim.xml")));
         if (File.Exists(SettingsFile))
             {
             string ExecutableBuildDate = ApsimBuildDate();
             string ExecutableBuildNumber = ApsimBuildNumber();
             SettingsDoc.Load(SettingsFile);
             SettingsNode = SettingsDoc.DocumentElement;
             // store the build number & date of the currently executing apsim to fix bug #1159
             XmlHelper.SetValue(SettingsNode, "version/builddate", ExecutableBuildDate);
             XmlHelper.SetValue(SettingsNode, "version/buildnumber", ExecutableBuildNumber);
             }

         }
      public static Configuration Instance
         {
         get
            {
            if (Singleton == null)
               Singleton = new Configuration();
            return Singleton;
            }
         }
      public void RevertToDefaults()
         {
         string SettingsFile = Path.Combine(Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                     "Apsim"), ApsimVersion()), "Apsim.xml");
         File.Delete(SettingsFile);
         Singleton = null;
         }
      private void Save()
         {
         // The settings in the installation dir are read only. Save in a local (ie writeable) path.
         string SettingsFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                               Path.Combine("Apsim", Path.Combine(ApsimVersion(), "Apsim.xml")));

         // The first time this runs on a machine, none of these dirs will exist.
         makePathExist (Path.GetDirectoryName(SettingsFile));
         SettingsNode.OwnerDocument.Save(SettingsFile);
         }
      public static void makePathExist(string path)
         {
         List<string> pathsToMake = new List<string>();
         string dir = path;
         while (!Directory.Exists(dir) && dir.Length > 3)
             {
             pathsToMake.Add(dir);
             dir = Path.GetDirectoryName(dir);
             }
         for (int i = pathsToMake.Count - 1; i >= 0; i-- )
             {
             Directory.CreateDirectory(pathsToMake[i]);
             }
         }
       public static string RemoveMacros(string St)
         {
         string result = St.Replace("%apsim%", ApsimDirectory());
         return result.Replace("%ausfarm%", AusFarmDirectory());
         }
      public static string AddMacros(string St)
         {
         string ReturnString = St;
         int Pos = St.ToLower().IndexOf(ApsimDirectory().ToLower());
         if (Pos != -1)
            {
            ReturnString = ReturnString.Remove(Pos, ApsimDirectory().Length);
            ReturnString = ReturnString.Insert(Pos, "%apsim%");
            }
         Pos = St.ToLower().IndexOf(AusFarmDirectory().ToLower());
         if (Pos != -1)
         {
             ReturnString = ReturnString.Remove(Pos, AusFarmDirectory().Length);
             ReturnString = ReturnString.Insert(Pos, "%ausfarm%");
         }
         return ReturnString;
         }
      public static string ApsimDirectory()
         {
         string Directory = Path.GetDirectoryName(CSGeneral.Utility.ConvertURLToPath(Assembly.GetExecutingAssembly().CodeBase));
         while (Directory != Path.GetPathRoot(Directory) && !File.Exists(Path.Combine(Directory, "Apsim.xml")))
            Directory = Path.GetFullPath(Path.Combine(Directory, ".."));
         if (Directory == Path.GetPathRoot(Directory))
            return "";
         return Directory;
         }
      public static string ApsimBinDirectory()
         {
         return Path.Combine(ApsimDirectory(), "Model");
         }

      // from http://stackoverflow.com/questions/692410/hello-os-with-c-mono
      public static bool amRunningOnUnix ()
         {
         // can also use "bool runningOnMono = Type.GetType ("Mono.Runtime") != null;" but mono can be run on windows too.
         int p = (int) Environment.OSVersion.Platform;
         if ((p == 4) || (p == 6) || (p == 128)) 
            {
            return true; // Running on Unix
            }
         return false; // Something else
         }

       // When we look up the AusFarm directory, we cache the result so we don't need
       // to make registry calls over and over and over...
      private static string AusFarmDir = ""; 

      public static string AusFarmDirectory()
      {
          if (! amRunningOnUnix() && AusFarmDir == "" )
          {
              RegistryKey rk = Registry.LocalMachine.OpenSubKey("SOFTWARE");
              if (rk != null)
                  rk = rk.OpenSubKey("CSIRO");
              if (rk != null)
                  rk = rk.OpenSubKey("AusFarm");
              if (rk != null)
                  rk = rk.OpenSubKey("ModLibs");
              if (rk != null)
                  rk = rk.OpenSubKey("CSIRO");
              if (rk != null)
                  rk = rk.OpenSubKey("Output");
              if (rk != null)
              {
                  string OutputModPath = (string)rk.GetValue("Path");
                  if (OutputModPath != null)
                      AusFarmDir = Path.GetDirectoryName(OutputModPath);
              }
              if (AusFarmDir == "")
                  AusFarmDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "AusFarm");
          }
          return AusFarmDir;
      }
      public string ApplicationName
         {
         get { return SectionName; }
         set { SectionName = value; }
         }
      public string ApsimVersion()
         {
         return XmlHelper.Value(SettingsNode, "version" + XmlHelper.Delimiter + "apsim");
         }
      public string ApsimBuildDate()
         {
         return XmlHelper.Value(SettingsNode, "version" + XmlHelper.Delimiter + "builddate");
         }
      public string ApsimBuildNumber()
         {
         return XmlHelper.Value(SettingsNode, "version" + XmlHelper.Delimiter + "buildnumber");
         }
      public string Setting(string SettingName)
         {
         return RemoveMacros(XmlHelper.Value(SettingsNode, SectionName + XmlHelper.Delimiter + SettingName));
         }
      public string ClimateSetting(string SettingName)
         {
         return RemoveMacros(XmlHelper.Value(SettingsNode, "climate" + XmlHelper.Delimiter + SettingName));
         }
      public List<string> Settings(string SettingName)
         {
         List<string> Values = XmlHelper.Values(SettingsNode, SectionName + XmlHelper.Delimiter + SettingName);
         for (int i = 0; i != Values.Count; i++)
            Values[i] = RemoveMacros(Values[i]);
         return Values;
         }
      public void SetSetting(string SettingName, string Value)
         {
         XmlHelper.SetValue(SettingsNode, SectionName + XmlHelper.Delimiter + SettingName, AddMacros(Value));
         Save();
         }
      public void SetSettings(string SettingName, List<string> Values)
         {
         List<string> NewValues = Values;
         for (int i = 0; i != NewValues.Count; i++)
            NewValues[i] = AddMacros(NewValues[i]);
         XmlHelper.SetValues(SettingsNode, SectionName + XmlHelper.Delimiter + SettingName, NewValues);
         Save();
         }
      public List<string> ComponentOrder()
         {
         return XmlHelper.Values(SettingsNode, "ComponentOrder" + XmlHelper.Delimiter + "Component");
         }
      public XmlNode GetSettingsNode(string NodeName)
         {
         return XmlHelper.Find(SettingsNode, NodeName);
         }
      public XmlNode ConversionsNode(string VersionNumber)
         {
         foreach (XmlNode Conversion in XmlHelper.ChildNodes(SettingsNode, "Conversions"))
            {
            if (XmlHelper.Value(Conversion, "to") == VersionNumber)
               return Conversion;
            }
         return null;
         }

      private const int MAX_NUM_FREQUENT_SIMS = 10;
      public void AddFileToFrequentList(string FileName)
         {
         List<string> FileNames = GetFrequentList();

         int FoundIndex = FileNames.IndexOf(FileName);
         if (FoundIndex != -1)
            FileNames.RemoveAt(FoundIndex);

         FileNames.Insert(0, FileName);
         while (FileNames.Count > MAX_NUM_FREQUENT_SIMS)
            FileNames.RemoveAt(MAX_NUM_FREQUENT_SIMS-1);
         SetSettings("RecentFile", FileNames);
         }
      public List<string> GetFrequentList()
         {
         List<string> FileNames = Settings("RecentFile");
         List<string> GoodFileNames = new List<string>();
         foreach (string FileName in FileNames)
            {
            if (File.Exists(FileName))
               GoodFileNames.Add(FileName);
            }
         return GoodFileNames;
         }
      }
   }
