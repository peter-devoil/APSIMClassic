using System;
using CSGeneral;
using System.Xml;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace ApsimFile
   {
   // ------------------------------------------
   // This class converts an APSIM file from one 
   // version to the 'current' version
   // ------------------------------------------
   public class APSIMChangeTool
      {
      public static int CurrentVersion = 18;
      private delegate void UpgraderDelegate(XmlNode Data);

      public static bool Upgrade(XmlNode Data)
         {
         // ------------------------------------------
         // Upgrade the specified data
         // to the 'current' version. Returns true
         // if something was upgraded.
         // ------------------------------------------
         return UpgradeToVersion(Data, CurrentVersion);
         }

      public static bool UpgradeToVersion(XmlNode Data, int ToVersion)
         {
         // ------------------------------------------
         // Upgrade the specified data
         // to the specified version. Returns true
         // if something was upgraded.
         // ------------------------------------------

         UpgraderDelegate[] Upgraders = { null,
                                          new UpgraderDelegate(ToVersion2), 
                                          new UpgraderDelegate(ToVersion3),
                                          new UpgraderDelegate(ToVersion4),
                                          new UpgraderDelegate(ToVersion5),
                                          new UpgraderDelegate(ToVersion6),
                                          new UpgraderDelegate(ToVersion7),
                                          new UpgraderDelegate(ToVersion8),
                                          new UpgraderDelegate(ToVersion9),
                                          new UpgraderDelegate(ToVersion10),
                                          new UpgraderDelegate(ToVersion11),
                                          new UpgraderDelegate(ToVersion12),
                                          new UpgraderDelegate(ToVersion13),
                                          new UpgraderDelegate(ToVersion14),
                                          new UpgraderDelegate(ToVersion15),
                                          new UpgraderDelegate(ToVersion16),
                                          new UpgraderDelegate(ToVersion17),
                                          new UpgraderDelegate(ToVersion18)
                                       };
         if (Data != null)
            {
            // Get version number of data.
            int DataVersion = 1;
            if (XmlHelper.Attribute(Data, "version") != "")
               DataVersion = Convert.ToInt32(XmlHelper.Attribute(Data, "version"));

            while (DataVersion < ToVersion)
               {
               Upgrade(Data, Upgraders[DataVersion]);
               DataVersion++;
               }

            // All finished upgrading - write version number out.
            XmlHelper.SetAttribute(Data, "version", ToVersion.ToString());
            return (DataVersion != CurrentVersion);
            }
         else
            return false;
         }

      private static void Upgrade(XmlNode Data, UpgraderDelegate Upgrader)
         {
         // ------------------------------------------------
         // Upgrade the data using the specified 'upgrader'
         // ------------------------------------------------

         foreach (XmlNode Child in XmlHelper.ChildNodes(Data, ""))
            {
            Upgrader(Child);
            if (Child.Name.ToLower() == "area"
               || Child.Name.ToLower() == "folder"
               || Child.Name.ToLower() == "simulation"
               || Child.Name.ToLower() == "manager"
               || Child.Name.ToLower() == "outputfile"
               || Child.Name.ToLower() == "graph"
               || Child.Name.ToLower() == "data"
               || Child.Name.ToLower() == "tclmanager"
               || Child.Name.ToLower() == "farmmanager"
               || Child.Name.ToLower() == "paddockmanager")
               Upgrade(Child, Upgrader);  // recurse
            }
         }

      #region Version2
      private static void ToVersion2(XmlNode Data)
         {
         // ---------------------------------------------------------------------------
         // Upgrade the data to file version 2. This file version was used in APSIM 4.2
         // ---------------------------------------------------------------------------
         if (XmlHelper.Type(Data).ToLower() == "soil")
            ToVersion2Soil(Data);
         else if (XmlHelper.Type(Data).ToLower() == "registrations")
            XmlHelper.SetName(Data, "global");
         else if (XmlHelper.Type(Data).ToLower() == "outputfile")
            {
            ToVersion2Variables(Data, "OutputFileDescription/Variables", "variable");
            ToVersion2Variables(Data, "OutputFileDescription/Events", "event");
            }
         }
      private static void ToVersion2Soil(XmlNode Data)
         {
         XmlNode Water = XmlHelper.Find(Data, "Water");
         XmlNode Nitrogen = XmlHelper.Find(Data, "Nitrogen");
         XmlNode InitWater = XmlHelper.Find(Data, "InitWater");
         XmlNode InitNitrogen = XmlHelper.Find(Data, "InitNitrogen");

         if (InitWater == null)
            {
            InitWater = Data.AppendChild(XmlHelper.CreateNode(Data.OwnerDocument, "InitWater", ""));
            MoveVersion2SoilInfo(Water, "sw", InitWater);
            }
         else
            DeleteVersion2SoilInfo(Water, "Sw");

         if (InitNitrogen == null)
            {
            InitNitrogen = Data.AppendChild(XmlHelper.CreateNode(Data.OwnerDocument, "InitNitrogen", ""));
            MoveVersion2SoilInfo(Nitrogen, "no3", InitNitrogen);
            MoveVersion2SoilInfo(Nitrogen, "nh4", InitNitrogen);
            }
         else
            {
            DeleteVersion2SoilInfo(Nitrogen, "no3");
            DeleteVersion2SoilInfo(Nitrogen, "nh4");
            }
         }
      private static void DeleteVersion2SoilInfo(XmlNode FromNode, string InfoName)
         {
         foreach (XmlNode Layer in XmlHelper.ChildNodes(FromNode, "Layer"))
            {
            XmlNode Value = XmlHelper.Find(Layer, InfoName);
            if (Value != null)
               Layer.RemoveChild(Value);
            }
         }
      private static void MoveVersion2SoilInfo(XmlNode FromNode, string InfoName, XmlNode ToNode)
         {
         // Make sure we have enough layers in the ToNode.
         int NumLayers = XmlHelper.ChildNodes(FromNode, "Layer").Count;
         for (int LayerNumber = 1; LayerNumber <= NumLayers; LayerNumber++)
            if (XmlHelper.Find(ToNode, LayerNumber.ToString()) == null)
               ToNode.AppendChild(XmlHelper.CreateNode(ToNode.OwnerDocument, "layer", LayerNumber.ToString()));

         foreach (XmlNode Layer in XmlHelper.ChildNodes(FromNode, "Layer"))
            {
            XmlNode Value = XmlHelper.Find(Layer, InfoName);
            if (Value != null)
               {
               Layer.RemoveChild(Value);
               XmlNode ToLayer = XmlHelper.Find(ToNode, XmlHelper.Name(Layer));
               ToLayer.AppendChild(Value);
               }
            }
         }
      private static void ToVersion2Variables(XmlNode Data, string NodeToOperateOn, string ChildType)
         {
         // ------------------------------------------
         // Remove all 'data outside paddock' from all 
         // children of specified data.
         // ------------------------------------------
         XmlNode Variables = XmlHelper.FindByType(Data, NodeToOperateOn);

         if (Variables != null)
            {
            foreach (XmlNode Child in XmlHelper.ChildNodes(Variables, ChildType))
               {
               if (XmlHelper.Attribute(Child, "module").ToLower() == "data outside paddock")
                  XmlHelper.SetAttribute(Child, "module", "global");
               if (XmlHelper.Name(Child).ToLower().IndexOf("data outside paddock.") == 0)
                  XmlHelper.SetName(Child, Child.Name.Remove(0, 21));
               }
            }
         }
      #endregion

      private static void ToVersion3(XmlNode Data)
         {
         // ---------------------------------------------------------------------------
         // Upgrade the data to file version 3. This file version was used in APSIM 5.0
         // NB: APSIM 5.0 and 5.1 didn't actually do any conversions due to the converter
         // never being called. There was also another bug where it was looking for
         // 'sample' nodes rather than 'soilsample'. Only YieldProphet ever used
         // soil samples at this time.
         // ---------------------------------------------------------------------------

         if (XmlHelper.Type(Data).ToLower() == "soil")
            {
            XmlNode SoilSample = XmlHelper.FindByType(Data, "soilsample");
            if (SoilSample != null)
               {
               string SWUnit = XmlHelper.Value(SoilSample, "swunit");
               if (SWUnit != "")
                  {
                  string WaterFormat;
                  if (SWUnit.ToLower() == "volumetric")
                     WaterFormat = "VolumetricPercent";
                  else
                     WaterFormat = "GravimetricPercent";
                  XmlNode NewNode = SoilSample.OwnerDocument.CreateElement("WaterFormat");
                  XmlHelper.SetValue(NewNode, "", WaterFormat);
                  SoilSample.ReplaceChild(NewNode, XmlHelper.Find(SoilSample, "swunit"));
                  }

               XmlNode Nitrogen = XmlHelper.Find(SoilSample, "Nitrogen");
               XmlNode Other = XmlHelper.Find(SoilSample, "Other");
               if (Other == null)
                  Other = SoilSample.AppendChild(SoilSample.OwnerDocument.CreateElement("Other"));
               MoveVersion2SoilInfo(Nitrogen, "oc", Other);
               MoveVersion2SoilInfo(Nitrogen, "ph", Other);
               }
            }
         }

      private static void ToVersion4(XmlNode Data)
         {
         if (XmlHelper.Type(Data).ToLower() == "rule")
            {
            foreach (XmlNode category in XmlHelper.ChildNodes(Data, "category"))
               {
               foreach (XmlNode property in XmlHelper.ChildNodes(category, "property"))
                  {
                  XmlNode NewProperty = XmlHelper.CreateNode(category.OwnerDocument, XmlHelper.Name(property), "");
                  XmlHelper.SetAttribute(NewProperty, "type", XmlHelper.Attribute(property, "type"));

                  if (XmlHelper.Attribute(property, "croppropertyname") != "")
                     XmlHelper.SetAttribute(NewProperty, "croppropertyname", XmlHelper.Attribute(property, "croppropertyname"));

                  if (XmlHelper.Attribute(property, "listvalues") != "")
                     XmlHelper.SetAttribute(NewProperty, "listvalues", XmlHelper.Attribute(property, "listvalues"));

                  XmlHelper.SetAttribute(NewProperty, "description", XmlHelper.Attribute(property, "description"));
                  NewProperty.InnerText = XmlHelper.Attribute(property, "value");
                  category.ReplaceChild(NewProperty, property);
                  }
               }
            }
         }

      private static void ToVersion5(XmlNode Data)
         {
         if (XmlHelper.Attribute(Data, "shortcut") != "" && XmlHelper.Attribute(Data, "name") == "")
            XmlHelper.SetName(Data, XmlHelper.Attribute(Data, "shortcut"));

         // get rid of <filename>
         if (XmlHelper.Type(Data).ToLower() == "outputfile")
            {
            XmlNode FileNameNode = XmlHelper.Find(Data, "filename");
            if (FileNameNode != null)
               Data.RemoveChild(FileNameNode);
            }

         if (XmlHelper.Type(Data).ToLower() == "outputfiledescription")
            {
            XmlNode outputfiledescription = Data;
            if (XmlHelper.Attribute(outputfiledescription, "shortcut") == "")
               {
               string[] VGNames = XmlHelper.ChildNames(outputfiledescription, "variables");
               foreach (string VGName in VGNames)
                  {
                  XmlNode VariablesGroup = XmlHelper.Find(outputfiledescription, VGName);

                  string[] VNames = XmlHelper.ChildNames(VariablesGroup, "variable");
                  foreach (string VName in VNames)
                     {
                     XmlNode Variable = XmlHelper.Find(VariablesGroup, VName);
                     if (XmlHelper.Attribute(Variable, "name") != XmlHelper.Attribute(Variable, "variablename"))
                        XmlHelper.SetName(Variable, XmlHelper.Attribute(Variable, "variablename") + " as " + XmlHelper.Attribute(Variable, "name"));
                     if (XmlHelper.Attribute(Variable, "arrayspec").Trim() != "")
                        XmlHelper.SetName(Variable, XmlHelper.Name(Variable) + XmlHelper.Attribute(Variable, "arrayspec"));
                     string ComponentName = XmlHelper.Attribute(Variable, "module");
                     if (ComponentName.ToLower() == "global")
                        ComponentName = "";
                     if (ComponentName != "" && XmlHelper.Attribute(Variable, "ModuleType") != "soil")
                        XmlHelper.SetName(Variable, ComponentName + "." + XmlHelper.Name(Variable));
                     XmlHelper.SetAttribute(Variable, "array", "?");
                     XmlHelper.DeleteAttribute(Variable, "ModuleType");
                     XmlHelper.DeleteAttribute(Variable, "arrayspec");
                     XmlHelper.DeleteAttribute(Variable, "module");
                     XmlHelper.DeleteAttribute(Variable, "variablename");
                     }
                  XmlHelper.SetName(VariablesGroup, XmlHelper.Name(outputfiledescription));
                  VariablesGroup.ParentNode.ParentNode.AppendChild(VariablesGroup);
                  }

               string[] EGNames = XmlHelper.ChildNames(outputfiledescription, "events");
               foreach (string EGName in EGNames)
                  {
                  XmlNode EventsGroup = XmlHelper.Find(outputfiledescription, EGName);
                  string[] EventNames = XmlHelper.ChildNames(EventsGroup, "event");
                  foreach (string EventName in EventNames)
                     {
                     XmlNode Event = XmlHelper.Find(EventsGroup, EventName);
                     string ComponentName;
                     string NewEventName;

                     if (XmlHelper.Name(Event).IndexOf('.') != -1)
                        {
                        ComponentName = XmlHelper.Name(Event).Substring(0, XmlHelper.Name(Event).IndexOf('.'));
                        NewEventName = XmlHelper.Name(Event).Substring(XmlHelper.Name(Event).IndexOf('.') + 1);
                        }
                     else
                        {
                        NewEventName = XmlHelper.Name(Event);
                        ComponentName = XmlHelper.Attribute(Event, "module");
                        }

                     if (ComponentName.ToLower() == "global")
                        ComponentName = "";

                     if (ComponentName != "")
                        XmlHelper.SetName(Event, ComponentName + "." + NewEventName);
                     else
                        XmlHelper.SetName(Event, NewEventName);

                     XmlHelper.DeleteAttribute(Event, "ModuleType");
                     XmlHelper.DeleteAttribute(Event, "module");
                     XmlHelper.DeleteAttribute(Event, "eventname");
                     }
                  XmlHelper.SetName(EventsGroup, XmlHelper.Name(outputfiledescription) + " Events");
                  EventsGroup.ParentNode.ParentNode.AppendChild(EventsGroup);
                  }
               }
            else
               {
               XmlNode VariablesGroup = Data.ParentNode.AppendChild(XmlHelper.CreateNode(Data.OwnerDocument, "variables", XmlHelper.Attribute(outputfiledescription, "shortcut")));
               XmlHelper.SetAttribute(VariablesGroup, "shortcut", XmlHelper.Attribute(outputfiledescription, "shortcut"));

               XmlNode EventsGroup = Data.ParentNode.AppendChild(XmlHelper.CreateNode(Data.OwnerDocument, "events", XmlHelper.Attribute(outputfiledescription, "shortcut") + " Events"));
               XmlHelper.SetAttribute(EventsGroup, "shortcut", XmlHelper.Attribute(outputfiledescription, "shortcut") + " Events");
               }
            outputfiledescription.ParentNode.RemoveChild(outputfiledescription);
            }
         }

      private static void ToVersion6(XmlNode Data)
         {
         if (XmlHelper.Type(Data).ToLower() == "logic")
            {
            foreach (XmlNode script in XmlHelper.ChildNodes(Data, "script"))
               {
               string text = script.InnerText;
               script.InnerText = "";
               string eventName = XmlHelper.Name(script);
               eventName = eventName.Replace("startofday", "start_of_day");
               eventName = eventName.Replace("endofday", "end_of_day");
               XmlHelper.SetValue(script, "event", eventName);
               XmlHelper.SetValue(script, "text", text);
               XmlHelper.DeleteAttribute(script, "name");
               }
            }
         }

      #region Version7
      private static void ToVersion7(XmlNode Data)
         {
         // ---------------------------------------------------------
         // Version 7 soils now have a 'profile' element. Go create
         // one and populate it with all children of 
         // <Water>, <Nitrogen> and <Other> elements.
         // SoilCrop nodes are turned into:
         //     <ll name="Barley">0.460</ll>
         //     <kl name="Barley">0</kl>
         //     <xf name="Barley">0</xf>
         // for each layer under profile.
         // ---------------------------------------------------------

         if (XmlHelper.Type(Data).ToLower() == "soil" || XmlHelper.Type(Data).ToLower() == "soilsample")
            {
            foreach (XmlNode Child in XmlHelper.ChildNodes(Data, ""))
               {
               if (XmlHelper.Type(Child).ToLower() == "water" ||
                   XmlHelper.Type(Child).ToLower() == "nitrogen" ||
                   XmlHelper.Type(Child).ToLower() == "other" ||
                   XmlHelper.Type(Child).ToLower() == "soilcrop" ||
                   XmlHelper.Type(Child).ToLower() == "initwater" ||
                   XmlHelper.Type(Child).ToLower() == "initnitrogen")
                  ToSoilProfile(Child);
               else if (XmlHelper.Type(Child).ToLower() == "soilsample")
                  ToVersion7(Child);
               }
            foreach (XmlNode Child in XmlHelper.ChildNodes(Data, ""))
               {
               if (XmlHelper.Type(Child).ToLower() == "water" ||
                   XmlHelper.Type(Child).ToLower() == "nitrogen" ||
                   XmlHelper.Type(Child).ToLower() == "other" ||
                   XmlHelper.Type(Child).ToLower() == "soilcrop" ||
                   XmlHelper.Type(Child).ToLower() == "waterformat")
                  Data.RemoveChild(Child);
               }
            }
         }
      private static void ToSoilProfile(XmlNode Data)
         {
         XmlNode Profile;
         if (XmlHelper.Type(Data).ToLower() == "soilsample" ||
             XmlHelper.Type(Data).ToLower() == "initwater" ||
             XmlHelper.Type(Data).ToLower() == "initnitrogen")
            Profile = XmlHelper.EnsureNodeExists(Data, "profile");
         else
            Profile = XmlHelper.EnsureNodeExists(Data.ParentNode, "profile");

         int NumLayers = XmlHelper.ChildNodes(Data, "layer").Count;
         XmlHelper.EnsureNumberOfChildren(Profile, "layer", "", NumLayers);

         int LayerNumber = 0;
         foreach (XmlNode Child in XmlHelper.ChildNodes(Data, ""))
            {
            if (XmlHelper.Type(Child).ToLower() == "layer")
               {
               LayerNumber++;
               XmlNode Layer = XmlHelper.ChildNodes(Profile, "layer")[LayerNumber - 1];
               foreach (XmlNode Value in XmlHelper.ChildNodes(Child, ""))
                  {
                  if (Value.InnerText != MathUtility.MissingValue.ToString())
                     {
                     XmlNode LayerData = Layer.AppendChild(Value);

                     // truncates to 3 dec places.
                     if (Value.InnerText.IndexOf('.') != -1)
                        {
                        double DoubleValue = Convert.ToDouble(Value.InnerText);
                        LayerData.InnerText = DoubleValue.ToString("f3");
                        }

                     if (XmlHelper.Type(Data).ToLower() == "soilcrop")
                        XmlHelper.SetName(LayerData, XmlHelper.Name(Data));
                     }
                  }
               Data.RemoveChild(Child);
               }
            else if (XmlHelper.Type(Child).ToLower() != "profile")
               Data.ParentNode.AppendChild(Child);
            }
         }
      #endregion

      #region Version8
      private static void ToVersion8(XmlNode Data)
         {
         // --------------------------------------------------------------
         // Put a <thickness> into <InitWater> and <InitNitrogen> elements
         // when they have layered info.
         // --------------------------------------------------------------

         if (XmlHelper.Type(Data).ToLower() == "soil")
            {
            XmlNode WaterProfile = XmlHelper.Find(Data, "Profile");
            if (WaterProfile != null)
               {
               foreach (XmlNode Child in XmlHelper.ChildNodes(Data, ""))
                  {
                  if (XmlHelper.Type(Child).ToLower() == "initwater" ||
                      XmlHelper.Type(Child).ToLower() == "initnitrogen")
                     {
                     int LayerNumber = 1;
                     XmlNode Profile = XmlHelper.Find(Child, "Profile");
                     foreach (XmlNode Layer in XmlHelper.ChildNodes(Profile, "layer"))
                        {
                        string Thickness = GetSoilProfileInfo(WaterProfile, "thickness", LayerNumber);
                        XmlHelper.SetValue(Layer, "thickness", Thickness);
                        LayerNumber++;
                        }
                     }
                  }
               }
            }
         }
      private static string GetSoilProfileInfo(XmlNode Profile, string InfoName, int LayerNumber)
         {
         if (XmlHelper.ChildNodes(Profile, "layer").Count >= LayerNumber)
            {
            XmlNode Layer = XmlHelper.ChildNodes(Profile, "layer")[LayerNumber - 1];
            return XmlHelper.Value(Layer, InfoName);
            }
         return "";
         }
      #endregion

      private static void ToVersion9(XmlNode Data)
         {
         if (XmlHelper.Type(Data).ToLower() == "stockherbageconverter")
            {
            string[] TypesToDelete = {"proportion_legume", "dmdValue", "p_conc_green_leaf_default",
                                          "p_conc_green_stem_default", "p_conc_senesced_leaf_default",
                                          "p_conc_senesced_stem_default", "p_conc_dead_leaf_default",
                                          "p_conc_dead_stem_default", "ash_alk_green_leaf_default",
                                          "ash_alk_green_stem_default", "ash_alk_senesced_leaf_default",
                                          "ash_alk_senesced_stem_default", "ash_alk_dead_leaf_default",
                                          "ash_alk_dead_stem_default", "ns_ratio_green_leaf_default",
                                          "ns_ratio_green_stem_default", "ns_ratio_senesced_leaf_default",
                                          "ns_ratio_senesced_stem_default", "ns_ratio_dead_leaf_default",
                                          "ns_ratio_dead_stem_default", "np_ratio_green_leaf_default",
                                          "np_ratio_green_stem_default", "np_ratio_senesced_leaf_default",
                                          "np_ratio_senesced_stem_default", "np_ratio_dead_leaf_default",
                                          "np_ratio_dead_stem_default", "dmd_green_leaf",
                                          "dmd_green_stem", "dmd_senesced_leaf",
                                          "dmd_senesced_stem", "dmd_dead_leaf",
                                          "dmd_dead_stem", "KQ5Leaf",
                                          "KQ5Stem", "KQ4",
                                          "cp_n_ratio"};

            foreach (string Type in TypesToDelete)
               {
               XmlNode Child = XmlHelper.Find(Data, Type);
               if (Child != null)
                  Data.RemoveChild(Child);
               }
            }
         }

      private static void ToVersion10(XmlNode Data)
         {
         // ---------------------------------------------------------------
         // This conversion doesn't really work.
         // ---------------------------------------------------------------

         string[] OkDataTypes = {"apsimfilereader", "xmlfilereader", "probability", "filter",
                                    "cumulative", "depth", "diff", "frequency", "kwtest",
                                    "predobs", "regression", "stats", "soi", "rems", 
                                    "excelreader", "recordfilter"};

         if (XmlHelper.Type(Data).ToLower() == "data")
            {
            foreach (XmlNode Child in XmlHelper.ChildNodes(Data, ""))
               foreach (XmlNode SubChild in XmlHelper.ChildNodes(Child, ""))
                  {
                  if (Array.IndexOf(OkDataTypes, XmlHelper.Type(SubChild).ToLower()) != -1)
                     {
                     // Add a source node to our data node.
                     XmlNode NewNode = SubChild.AppendChild(XmlHelper.CreateNode(SubChild.OwnerDocument, "source", ""));
                     XmlHelper.SetValue(NewNode, "", XmlHelper.Name(SubChild.ParentNode));

                     // Move data node to parent.
                     Data.AppendChild(SubChild);
                     }
                  }
            }
         }

      #region Version11
      private static void ToVersion11(XmlNode Data)
         {
         // ---------------------------------------------------------------
         // Shortcut paths are now full paths. e.g.
         //     <manager name="SharedLogic" shortcut="/untitled/shared/SharedLogic" />
         // ---------------------------------------------------------------

         string ShortcutPath = XmlHelper.Attribute(Data, "shortcut");

         if (ShortcutPath != "" && ShortcutPath[0] != '/')
            {
            ShortcutPath = "/" + XmlHelper.Name(Data.OwnerDocument.DocumentElement)
                               + "/shared/" + ShortcutPath.Replace("\\", "/");
            if (XmlHelper.FullPath(Data) != ShortcutPath)
               XmlHelper.SetAttribute(Data, "shortcut", ShortcutPath);
            else
               XmlHelper.DeleteAttribute(Data, "shortcut");
            XmlNode RealNode = XmlHelper.Find(Data, ShortcutPath);
            MakeNodeShortcuts(Data, RealNode);
            }
         }
      private static void MakeNodeShortcuts(XmlNode ShortCutNode, XmlNode RealNode)
         {
         XmlHelper.SetName(ShortCutNode, XmlHelper.Name(RealNode));
         foreach (XmlNode Child in XmlHelper.ChildNodes(RealNode, ""))
            {
            if (Types.Instance.IsVisible(Child.Name) || Child.Name == "rule")
               {
               XmlNode NewNode = ShortCutNode.AppendChild(ShortCutNode.OwnerDocument.CreateElement(Child.Name));
               string ShortCutPath = XmlHelper.FullPath(RealNode) + "/" + XmlHelper.Name(Child);
               XmlNode RealChildNode = XmlHelper.Find(RealNode, ShortCutPath);
               XmlHelper.SetAttribute(NewNode, "shortcut", ShortCutPath);
               MakeNodeShortcuts(NewNode, Child);
               }
            }
         }
      #endregion

      private static void ToVersion12(XmlNode Data)
         {
         // -----------------------------------------------------------
         // <rule> and <logic> nodes are now called manager.
         // -----------------------------------------------------------
         string nodeName = Data.Name.ToLower();
         if (nodeName == "manager" && XmlHelper.Name(Data) != "Manager folder")
            {
            // Change <manager> to <folder name="Manager folder">
            XmlNode NewNode = XmlHelper.ChangeType(Data, "folder");
            XmlHelper.SetName(NewNode, "Manager folder");
            foreach (XmlNode Rule in XmlHelper.ChildNodes(NewNode, ""))
               ToVersion12(Rule);
            }

         else if (nodeName == "rule" || nodeName == "logic")
            {
            // Change <rule> to <manager>
            XmlNode NewNode = XmlHelper.ChangeType(Data, "manager");

            foreach (XmlNode Condition in XmlHelper.ChildNodes(NewNode, "condition"))
               {
               XmlNode ScriptNode = XmlHelper.CreateNode(NewNode.OwnerDocument, "script", "");
               NewNode.AppendChild(ScriptNode);
               XmlHelper.SetValue(ScriptNode, "text", Condition.InnerText);
               XmlHelper.SetValue(ScriptNode, "event", XmlHelper.Name(Condition));
               NewNode.RemoveChild(Condition);
               }
            if (XmlHelper.ChildNodes(NewNode, "category").Count > 0)
               {
               XmlNode UI = NewNode.AppendChild(NewNode.OwnerDocument.CreateElement("ui"));
               foreach (XmlNode Category in XmlHelper.ChildNodes(NewNode, "category"))
                  {
                  XmlNode CategoryNode = UI.AppendChild(UI.OwnerDocument.CreateElement("category"));
                  XmlHelper.SetName(CategoryNode, XmlHelper.Name(Category));
                  foreach (XmlNode Prop in XmlHelper.ChildNodes(Category, ""))
                     UI.AppendChild(UI.OwnerDocument.ImportNode(Prop, true));
                  NewNode.RemoveChild(Category);
                  }
               }
            }
         else if (nodeName == "tclmanager" || nodeName == "farmmanager" || nodeName == "paddockmanager")
            {
            foreach (XmlNode Rule in XmlHelper.ChildNodes(Data, "rule"))
               {
               if (XmlHelper.Attribute(Rule, "shortcut") != "")
                  {
                  string ShortCutPath = XmlHelper.Attribute(Rule, "shortcut");
                  XmlHelper.SetAttribute(Rule, "shortcut", ShortCutPath);
                  }
               else
                  {
                  foreach (XmlNode Condition in XmlHelper.ChildNodes(Rule, "condition"))
                     {
                     XmlNode ScriptNode = XmlHelper.CreateNode(Rule.OwnerDocument, "script", "");
                     Rule.AppendChild(ScriptNode);
                     XmlHelper.SetValue(ScriptNode, "text", Condition.InnerText);
                     XmlHelper.SetValue(ScriptNode, "event", XmlHelper.Name(Condition));
                     }
                  }
               Data.AppendChild(Rule);
               }
            }
         }

      private static void ToVersion13(XmlNode Variables)
         {
         ApplyConversionsFile(Variables, Configuration.Instance.ConversionsNode("5.4"));

         if (XmlHelper.Type(Variables) == "tclgroup")
            {
            // Clone this node with a new "type"
            XmlNode NewNode = Variables.ParentNode.AppendChild(Variables.OwnerDocument.CreateElement("tclmanager"));
            XmlHelper.SetName(NewNode, XmlHelper.Name(Variables));
            foreach (XmlNode Child in XmlHelper.ChildNodes(Variables, ""))
               {
               if (XmlHelper.Type(Child) == "initscript")
                  {
                  // make this an explicit "logic" component
                  XmlNode InitScriptNode = XmlHelper.CreateNode(Variables.OwnerDocument, "rule", "Initialisation logic");
                  XmlNode LogicScriptNode = XmlHelper.CreateNode(Variables.OwnerDocument, "script", "init");
                  XmlHelper.SetValue(LogicScriptNode, "text", Child.InnerText);
                  XmlHelper.SetValue(LogicScriptNode, "event", "init");
                  InitScriptNode.AppendChild(LogicScriptNode);
                  NewNode.AppendChild(InitScriptNode);
                  }
               else
                  {
                  NewNode.AppendChild(NewNode.OwnerDocument.ImportNode(Child, true));
                  }
               }
            Variables.ParentNode.ReplaceChild(NewNode, Variables);
            }
         }

      private static void ToVersion14(XmlNode Variables)
         {
         ApplyConversionsFile(Variables, Configuration.Instance.ConversionsNode("6.1"));
         }

      private static void ApplyConversionsFile(XmlNode Variables, XmlNode Conversions)
         {
         if (Variables.Name.ToLower() == "variables")
            {
            foreach (XmlNode Conversion in XmlHelper.ChildNodes(Conversions, "conversion"))
               {
               string[] Bits = XmlHelper.Value(Conversion, "description").Split(' ');
               if (Bits.Length == 5 && Bits[0] == "Renamed")
                  {
                  string OldName = Bits[2].ToLower();
                  string NewName = Bits[4];
                  foreach (XmlNode Variable in XmlHelper.ChildNodes(Variables, "Variable"))
                     {
                     string VariableLine = XmlHelper.Name(Variable);

                     // Do replacement where a module name was specified.
                     int Pos = VariableLine.ToLower().IndexOf("." + OldName);
                     if (Pos != -1)
                        {
                        Pos += OldName.Length + 1;
                        if (Pos == VariableLine.Length || VariableLine[Pos] == ' ')
                           {
                           Pos -= OldName.Length + 1;
                           VariableLine = VariableLine.Substring(0, Pos)
                                         + "." + NewName
                                         + VariableLine.Substring(Pos + OldName.Length + 1);
                           }
                        }
                     else if (VariableLine.Length >= OldName.Length && VariableLine.ToLower().Substring(0, OldName.Length) == OldName.ToLower())
                        {
                        VariableLine = NewName;
                        if (NewName.Length < VariableLine.Length)
                           VariableLine += VariableLine.Substring(NewName.Length);
                        }
                     XmlHelper.SetName(Variable, VariableLine);
                     }
                  }
               else if (Bits.Length == 3 && Bits[0] == "Removed")
                  {
                  string NameToDelete = Bits[2].ToLower();
                  foreach (XmlNode Variable in XmlHelper.ChildNodes(Variables, "Variable"))
                     {
                     string VariableLine = XmlHelper.Name(Variable).ToLower();
                     int PosSpace = VariableLine.IndexOf(' ');
                     if (PosSpace == -1)
                        PosSpace = VariableLine.Length;
                     int PosPeriod = VariableLine.IndexOf('.');

                     // get the variable name
                     string VariableName;
                     if (PosPeriod != -1 && PosPeriod < PosSpace)
                        VariableName = VariableLine.Substring(PosPeriod, PosSpace - PosPeriod - 1);
                     else
                        VariableName = VariableLine.Substring(0, PosSpace);

                     // Do we want to delete this variable?
                     if (VariableName == NameToDelete)
                        Variables.RemoveChild(Variable);
                     }

                  }

               }
            }
         else if (Variables.Name.ToLower() == "manager")
            {
            foreach (XmlNode Conversion in XmlHelper.ChildNodes(Conversions, "conversion"))
               {
               string[] Bits = XmlHelper.Value(Conversion, "description").Split(' ');
               if (Bits.Length == 5 && Bits[0] == "Renamed")
                  {
                  string OldName = Bits[2].ToLower();
                  string NewName = Bits[4];
                  foreach (XmlNode Script in XmlHelper.ChildNodes(Variables, "script"))
                     {
                     string Text = XmlHelper.Value(Script, "text");
                     Text = Regex.Replace(Text, "(\\W)" + OldName + "(\\W)", "$1" + NewName + "$2");
                     XmlHelper.SetValue(Script, "text", Text);
                     }
                  }
               }
            }
         }

      private static void ToVersion15(XmlNode Node)
         {
         if (XmlHelper.Type(Node) == "irrigation")
            {
            XmlNode no3_conc = Node.AppendChild(Node.OwnerDocument.CreateElement("default_no3_conc"));
            XmlHelper.SetAttribute(no3_conc, "type", "text");
            XmlHelper.SetAttribute(no3_conc, "description", "Nitrate concentration (ppm N)");
            XmlHelper.SetValue(no3_conc, "", "0.0");

            XmlNode nh4_conc = Node.AppendChild(Node.OwnerDocument.CreateElement("default_nh4_conc"));
            XmlHelper.SetAttribute(nh4_conc, "type", "text");
            XmlHelper.SetAttribute(nh4_conc, "description", "Ammonium concentration (ppm N)");
            XmlHelper.SetValue(nh4_conc, "", "0.0");

            XmlNode cl_conc = Node.AppendChild(Node.OwnerDocument.CreateElement("default_cl_conc"));
            XmlHelper.SetAttribute(cl_conc, "type", "text");
            XmlHelper.SetAttribute(cl_conc, "description", "Chloride concentration (ppm Cl)");
            XmlHelper.SetValue(cl_conc, "", "0.0");
            }
         }

      private static void ToVersion16(XmlNode Node)
         {
         // ---------------------------------------------------------------
         // Converts:
         //     <SplitReport name="Probability Exceedence">
         //       <Page name="Data" Left="0" Top="0" Width="829" Height="199">
         //         <ApsimFileReader name="OutputFile" Visible="yes" Left="0" Top="0" Width="318" Height="186">
         //           <BySeries>yes</BySeries>
         //           <FileName>
         //           </FileName>
         //         </ApsimFileReader>
         //         <Probability Left="489" Top="3" Width="128" Height="186">
         //           <FieldName>yield</FieldName>
         //           <Exceedence>Yes</Exceedence>
         //           <source>SeriesSplitter</source>
         //         </Probability>
         //         <SeriesSplitter Left="346" Top="0" Width="113" Height="189">
         //           <source>OutputFile</source>
         //           <FieldName>Title</FieldName>
         //         </SeriesSplitter>
         //       </Page>
         //       <Page name="Report" Left="0" Top="202" Width="829" Height="295">
         //         <Chart Left="18" Top="10" RightMargin="5" BottomMargin="5" Width="586" Height="478">
         //           <source>Probability</source>
         //           <XY>
         //             <X>*</X>
         //             <Y>Probability</Y>
         //             <SeriesType>Line</SeriesType>
         //             <PointType>None</PointType>
         //           </XY>
         //           <Properties>
         //             <Axes>
         //               <LeftAxis>
         //                 <Maximum>100</Maximum>
         //                 <Minimum>0</Minimum>
         //               </LeftAxis>
         //               <BottomAxis />
         //               <TopAxis />
         //               <RightAxis />
         //             </Axes>
         //           </Properties>
         //         </Chart>
         //       </Page>
         //      </SplitReport>
         //  
         //  To:
         //     <Graph name="Probability Exceedence">
         //        <Plot name="">
         //          <SeriesType>Solid line</SeriesType>
         //          <PointType>None</PointType>
         //          <Y>Probability</Y>
         //          <GDProbability>
         //            <GDApsimFileReader name="ApsimFileReader"/>
         //            <Exceedence>Yes</Exceedence>
         //          </GDProbability>
         //        </Plot>
         //      </Graph>
         //  ---------------------------------------------------------------

         if (Node.Name.ToLower() == "splitreport" || Node.Name.ToLower() == "tabbedreport")
            {
            XmlNode Data = XmlHelper.Find(Node, "data");
            if (Data != null)
               {
               string ChartName = XmlHelper.Name(Node);
               XmlHelper.SetName(Node, "zzzzzzzzzzzzzzz");
               foreach (XmlNode Page in XmlHelper.ChildNodes(Node, "page"))
                  {
                  XmlNode Chart = null;
                  if (XmlHelper.ChildNodes(Page, "chart").Count > 1)
                     {
                     // Turn page into a report.
                     Chart = ProcessReport(Page, Data);
                     }
                  else if (XmlHelper.ChildNodes(Page, "chart").Count == 1)
                     {
                     // Simple chart.
                     Chart = ProcessChart(XmlHelper.Find(Page, "chart"), Page.ParentNode.ParentNode, Data);
                     }
                  if (Chart != null)
                     {
                     XmlHelper.SetName(Chart, ChartName);
                     XmlHelper.EnsureNodeIsUnique(Chart);
                     }
                  }
               }
            Node.ParentNode.RemoveChild(Node);
            }
         }

      private static void ToVersion17(XmlNode Node)
         {
         // ---------------------------------------------------------------
         // Converts:
         //     <memo>e1xydGYxXGFuc2lcYW5zaWNwZzEyNTJcZGVmZjB</memo>
         //  
         //  To:
         //     <memo>This is some text</memo>
         //  ---------------------------------------------------------------

         if (Node.Name.ToLower() == "memo")
            {
            System.Windows.Forms.RichTextBox RichText = new System.Windows.Forms.RichTextBox();
            RichText.Rtf = CSGeneral.Utility.EncodeBase64ToString(Node.InnerXml);
            XmlHelper.SetValue(Node, "", RichText.Text.Replace("\n","\r\n"));
            }
         }


      private static XmlNode ProcessReport(XmlNode Page, XmlNode Data)
         {
         XmlNode Report = Page.ParentNode.ParentNode.AppendChild(Page.OwnerDocument.CreateElement("GraphReport"));
         int ChartNumber = 1;
         foreach (XmlNode Chart in XmlHelper.ChildNodes(Page, "chart"))
            {
            XmlNode Graph = ProcessChart(Chart, Report, Data);
            XmlHelper.SetName(Graph, "Graph" + ChartNumber.ToString());
            ChartNumber++;
            }
         return Report;
         }


      private static XmlNode ProcessChart(XmlNode Node, XmlNode ParentForChart, XmlNode Data)
         {
         XmlNode Chart = ParentForChart.AppendChild(Node);
         Chart = XmlHelper.ChangeType(Chart, "Graph");

         // If there is a <source> element under this chart then go put it into all <xy> nodes.
         XmlNode Source = XmlHelper.Find(Chart, "source");
         if (Source != null)
            {
            foreach (XmlNode XY in XmlHelper.ChildNodes(Chart, "XY"))
               XY.AppendChild(Source.CloneNode(true));
            if (XmlHelper.ChildNodes(Chart, "XY").Count > 0)
               Chart.RemoveChild(Source);
            }

         // recursively go resolve all <source> nodes.
         ResolveSourceNodes(Chart, Data);

         CleanUpGraphNode(Chart);
         return Chart;
         }

      private static void ResolveSourceNodes(XmlNode Node, XmlNode Data)
         {
         // --------------------------------------------------------------------
         // Go looking for all <source> nodes and replace them with the 
         // appropriate nodes under the Data node.
         // --------------------------------------------------------------------
         
         foreach (XmlNode Source in XmlHelper.ChildNodes(Node, "source"))
            {
            string SourceDataName = Source.InnerText;
            if (SourceDataName != "")
               {
               XmlNode SourceDataNode = XmlHelper.Find(Data, SourceDataName);
               if (SourceDataNode != null && SourceDataNode.Name == "SeriesSplitter")
                  {
                  SourceDataName = XmlHelper.Value(SourceDataNode, "source");
                  SourceDataNode = XmlHelper.Find(Data, SourceDataName);
                  }

               if (SourceDataNode != null)
                  {
                  XmlNode NewSource = SourceDataNode.CloneNode(true);
                  Node.ReplaceChild(NewSource, Source);
                  ResolveSourceNodes(NewSource, Data);
                  }
               }
            }

         // Recurse through all the <XY> nodes.
         foreach (XmlNode XY in XmlHelper.ChildNodes(Node, "xy"))
            ResolveSourceNodes(XY, Data);
         }

      private static void CleanUpGraphNode(XmlNode Node)
         {
         // --------------------------------------------------------------------
         // Cleanup all unwanted cruft from the Node.
         // --------------------------------------------------------------------
         
         string[] NodesToDelete = { "BySeries", "Colour", "Properties", "Phase" };

         string[] OldNames = {"XY", "ApsimFileReader", "Probability", "SOIData", "Depth", "Filter",  };
         string[] NewNames = { "Plot", "GDApsimFileReader", "GDProbability", "GDSOI", "GDDepth", "GDFilter" };
         
         string NodeName = XmlHelper.Name(Node);
         Node.Attributes.RemoveAll();
         XmlHelper.SetName(Node, NodeName);

         // If we have a value of '*' then delete ourselves and go no further
         if (Node.InnerText == "*" || (Node.InnerText.Length > 0 && Node.InnerText.Substring(0, 1) == "!"))
            {
            Node.ParentNode.RemoveChild(Node);
            return;
            }

         if (Node.InnerText == "SeriesName")
            Node.InnerText = "Title";

         // See if there are child nodes we can delete.
         foreach (XmlNode Child in XmlHelper.ChildNodes(Node, ""))
            {
            if (Array.IndexOf(NodesToDelete, Child.Name) != -1)
               Child.ParentNode.RemoveChild(Child);
            }

         // Change month string to month number.
         if (Node.Name == "Month")
            {
            string[] Months = { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };
            int MonthIndex = Array.IndexOf(Months, Node.InnerText);
            if (MonthIndex == -1)
               Node.ParentNode.RemoveChild(Node);
            else
               {
               MonthIndex++;
               Node.InnerText = MonthIndex.ToString();
               }
            return;
            }

         // If this is a <depth> node then extract the child <filter> node
         if (Node.Name == "Depth")
            ProcessDepthGraph(Node);

         // If this is a <depth> node then extract the child <filter> node
         if (Node.Name == "SOIData")
            {
            XmlNode FileName = XmlHelper.Find(Node, "filename");
            if (FileName != null)
               Node.RemoveChild(FileName);
            }

         //See if we need to rename ourselves.
         int Index = Array.IndexOf(OldNames, Node.Name);
         if (Index != -1)
            Node = XmlHelper.ChangeType(Node, NewNames[Index]);

         // Now recurse down to clean up all children.
         foreach (XmlNode Child in XmlHelper.ChildNodes(Node, ""))
            CleanUpGraphNode(Child);

         }

      private static void ProcessDepthGraph(XmlNode Node)
         {
         // --------------------------------------------------------------------
         // Need to convert: 
         //    <GDDepth>
         //      <GDFilter>
         //        <FilterString>Date='10/10/1942'</FilterString>
         //        <GDApsimFileReader name="OutputFile">
         //          <FileName>
         //          </FileName>
         //        </GDApsimFileReader>
         //      </GDFilter>
         //    </GDDepth>
         // To:
         //    <GDDepth>
         //      <Date>10/10/1942</Date>
         //      <GDApsimFileReader name="OutputFile">
         //        <FileName>
         //        </FileName>
         //      </GDApsimFileReader>
         //    </GDDepth>
         // --------------------------------------------------------------------

         XmlNode Filter = XmlHelper.Find(Node, "filter");
         List<string> DateStrings = XmlHelper.Values(Filter, "FilterString");

         for (int i = 0; i != DateStrings.Count; i++)
            {
            DateStrings[i] = DateStrings[i].Replace("Date='", "");
            DateStrings[i] = DateStrings[i].Replace("'", "");
            }
         XmlHelper.SetValues(Node, "Date", DateStrings);
         foreach (XmlNode FilterChild in XmlHelper.ChildNodes(Filter, ""))
            {
            if (FilterChild.Name != "FilterString")
               Node.AppendChild(FilterChild);
            }
         Node.RemoveChild(Filter);
         }


      private static void ToVersion18(XmlNode Node)
         {
         // ---------------------------------------------------------------
         // Removes:
         //     <registrations name="global" />
         // from all paddocks
         //  ---------------------------------------------------------------

         if (Node.Name.ToLower() == "area")
            {
            XmlNode RegistrationsNode = XmlHelper.FindByType(Node, "registrations");
            if (RegistrationsNode != null)
               Node.RemoveChild(RegistrationsNode);
            }
         }

      }
   }