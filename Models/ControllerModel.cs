using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Security.AccessControl;
using System.Threading;
using System.Windows;
using System.Xml.Linq;
using Windows.Gaming.Input;
using Windows.Graphics.Printing.PrintSupport;

namespace Controller.Models
{
    class ControllerModel : ModelBaseClass
    { 
        private ObservableCollection<ControllerInputModel> controllerInputs = new();

        /// <summary>
        /// Observable Collection of all inputs on the controller device
        /// </summary>
        public ObservableCollection<ControllerInputModel> ControllerInputs 
        {
            get {return controllerInputs; }
            set {controllerInputs = value; OnPropertyChanged(nameof(ControllerInputs)); }
        }

        /// <summary>
        /// Game controller object 
        /// </summary>
        public RawGameController? gameController { get; set; }

        private Thread? controllerPolling;

        // These dictionaries keep track of which inputs were loaded from the xml config file
        private Dictionary<int, int> ButtonHardwareIDToIndex = new();
        private Dictionary<int, int> AxisHardwareIDToIndex = new();
        private Dictionary<int, int> SwitchHardwareIDToIndex = new();

        public bool isShuttingDown { get; private set; } = false;

        private string mode = "Play";
        public string Mode
        {
            get { return mode; }
            set
            {
                if (mode == value) return;
                mode = value;
                if (mode != "Game")
                {
                    if (!LoadConfig()) resetEvent.Reset();
                }
            }
        }

        private int controllerCount;
        public int GetControllerCount() { return controllerCount; }

        public ControllerModel(string _mode, int controllerCount = 0, bool isConfig = false)
        {
            this.controllerCount = controllerCount;
            if (!getGameController()) { isShuttingDown = true; return; }
            Thread.Sleep(100);
            mode = _mode;
            LoadConfig(isConfig);
            controllerPolling = new Thread(getControllerOutput);
            controllerPolling.Start();
        }

        public ManualResetEvent resetEvent { get; private set; } = new ManualResetEvent(true);
        public CancellationTokenSource cancellationTokenSource { get; private set; } = new CancellationTokenSource();

        private bool getGameController()
        {
            //Milliseconds waited 
            int waitCount = 0;
            // Wait for program to detect game controllers
            while (RawGameController.RawGameControllers.Count == 0)
            {
                Thread.Sleep(100);
                waitCount += 100;
                if (waitCount >= 1000)
                {
                    var result = MessageBox.Show("Controller was not found, please connect a supported controller.", "No device detected", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                    if (result == MessageBoxResult.Cancel)
                    {
                        Application.Current.Shutdown();
                        return false;
                    }
                }
            }

            gameController = RawGameController.RawGameControllers[controllerCount];
            return true;
        }

        public EventHandler? InputActivated { get; set; } = null;

        /// <summary>
        /// Handles polling the controller for changes and updating all inputs with real-time values
        /// </summary>
        private void getControllerOutput()
        {
            RawGameController throttleController = gameController!;

            //Get number of buttons, switches and axes
            bool[] buttonArray = new bool[throttleController.ButtonCount];
            GameControllerSwitchPosition[] switchArray = new GameControllerSwitchPosition[throttleController.SwitchCount];
            double[] axisArray = new double[throttleController.AxisCount];

            //Loop to update ViewModel object with real-time controller values
            while (1 == 1)
            {
                resetEvent.WaitOne();

                if (cancellationTokenSource.IsCancellationRequested) break;


                // Get the current reading (populates the above arrays)
                throttleController.GetCurrentReading(buttonArray, switchArray, axisArray);

                try
                {
                    // Use mutexes to safely access shared member variables
                    lock (AxisHardwareIDToIndex) lock (ButtonHardwareIDToIndex) lock (SwitchHardwareIDToIndex)
                    {
                        int i = 0;
                        foreach (var button in buttonArray)
                        {
                            if (ButtonHardwareIDToIndex.Count != 0)
                            {
                                if (ButtonHardwareIDToIndex.ContainsKey(i))
                                {
                                    lock (ControllerInputs[ButtonHardwareIDToIndex[i]].Value)
                                    {

                                        if (ControllerInputs[ButtonHardwareIDToIndex[i]].Value != button.ToString())
                                        {
                                            ControllerInputs[ButtonHardwareIDToIndex[i]].Value = button.ToString();
                                            InputActivated?.Invoke(ControllerInputs[ButtonHardwareIDToIndex[i]], EventArgs.Empty);
                                        }

                                    }
                                }
                                i++;
                            }
                        }
                        i = 0;
                        foreach (var axis in axisArray)
                        {
                            if (AxisHardwareIDToIndex.Count != 0)
                            {
                                lock (controllerInputs[AxisHardwareIDToIndex[i]].Value)
                                {
                                    if (ControllerInputs[AxisHardwareIDToIndex[i]].Value != Math.Round(axis * 100).ToString())
                                        InputActivated?.Invoke(ControllerInputs[AxisHardwareIDToIndex[i]], EventArgs.Empty);
                                    ControllerInputs[AxisHardwareIDToIndex[i]].Value = Math.Round(axis * 100).ToString();
                                }
                                i++;
                            }
                        }
                        i = 0;
                        foreach (var swtch in switchArray)
                        {
                            if (SwitchHardwareIDToIndex.Count != 0)
                            {
                                lock (ControllerInputs[SwitchHardwareIDToIndex[i]].Value)
                                {
                                    ControllerInputs[SwitchHardwareIDToIndex[i]].Value = swtch.ToString();
                                    InputActivated?.Invoke(ControllerInputs[SwitchHardwareIDToIndex[i]], EventArgs.Empty);
                                }
                                i++;
                            }
                        }
                    }
                    
                }
                catch { }

                //Sleep for a bit to keep performance stable
                Thread.Sleep(10);
            }
        }

        private XDocument? FindXml(string fileName)
        {
            XDocument? xDoc = null;
            try
            {
                xDoc = XDocument.Load(Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory,
                        fileName + ".xml"));
            }
            catch (FileNotFoundException)
            {
                MessageBox.Show("An .xml configuration file was not found", "File not found", MessageBoxButton.OK, MessageBoxImage.Information);
                return null;
            }
            return xDoc;
        }

        private List<Tuple<int, string>> modeSelectorIndexes = new(3);

        private int? ReadXml(bool isConfig = false)
        {
            int index = 0;

            // Ensure file name of xml is the display name of the controller
            // with whitespace replaced with underscores
            string? controllerName = gameController!.DisplayName.Replace(' ', '_');
            if (controllerName == null) return null;
            XDocument? xDoc = FindXml(controllerName + "_" + Mode);
            if (xDoc != null)
            {
                var inputs = from i in xDoc.Element("inputs")!.Elements("input")
                             where i.Element("type")!.Value != "NoInput"
                             select i;
                foreach (var input in inputs)
                {
                    int hardwareId = Int32.Parse(input.Element("hardware_id")!.Value);
                    string type = input.Element("type")!.Value;
                    string name = (input.Element("name") == null) ? string.Empty : input.Element("name")!.Value;
                    string filename = (input.Element("filename") == null) ? string.Empty : input.Element("filename")!.Value;

                    if ((type == "ModeSelector" || type == "RecordButton") && isConfig )
                    {
                        modeSelectorIndexes.Add(new Tuple<int, string>(hardwareId, name));
                    }
                    else
                    {
                        ControllerInputs.Add(new ControllerInputModel(
                                hardwareId,
                                type,
                                name,
                                controllerName,
                                filename
                                ));
                        if (!type.ToLower().Equals("axis") && !type.ToLower().Equals("knob"))
                        {
                            ButtonHardwareIDToIndex[hardwareId] = index;
                        }
                        else
                        {
                            AxisHardwareIDToIndex[hardwareId] = index;
                        }
                        index++;
                    }
                }
            }
            else return null;
            return index;
        }

        public bool LoadConfig(bool isConfig = false)
        {
            resetEvent.Reset();
            Thread.Sleep(100);
            ButtonHardwareIDToIndex.Clear();
            AxisHardwareIDToIndex.Clear();
            SwitchHardwareIDToIndex.Clear();
            ControllerInputs.Clear();

            int index = 0;
            {
                int? temp = ReadXml(isConfig);
                if (temp == null) return false;
                index = temp.Value;
            }

            //Add whatever controller inputs where not defined in the config file
            //for (int i = 0; i < gameController!.ButtonCount; i++)
            //{
            //    if (!ButtonHardwareIDToIndex.ContainsKey(i))
            //    {
            //        ControllerInputs.Add(new ControllerInputModel(i, "Button", ""));
            //        ButtonHardwareIDToIndex[i] = index;
            //        index++;
            //    }
            //}
            //for (int i = 0; i < gameController!.AxisCount; i++)
            //{
            //    if (!AxisHardwareIDToIndex.ContainsKey(i))
            //    {
            //        ControllerInputs.Add(new ControllerInputModel(i, "Axis", ""));
            //        AxisHardwareIDToIndex[i] = index;
            //        index++;
            //    }
            //}
            //for (int i = 0; i < gameController!.SwitchCount; i++)
            //{
            //    if (!SwitchHardwareIDToIndex.ContainsKey(i))
            //    {
            //        ControllerInputs.Add(new ControllerInputModel(i, "Switch", ""));
            //        SwitchHardwareIDToIndex[i] = index;
            //        index++;
            //    }
            //}
            resetEvent.Set();
            return true;
        }

        public void SaveConfig()
        {
            Thread.Sleep(300);
            //Write to xml file
            XElement? root = new XElement("inputs");
            
            foreach (ControllerInputModel input in ControllerInputs)
            {
                root.Add(new XElement("input",
                    new XElement("type", input.InputType),
                    new XElement("name", input.Name),
                    new XElement("hardware_id", input.HardwareID),
                    new XElement("filename", input.AudioType)));
            }
            foreach(var modeSelector in modeSelectorIndexes)
            {
                root.Add(new XElement("input",
                    new XElement("type", "ModeSelector"),
                    new XElement("name", modeSelector.Item2),
                    new XElement("hardware_id", modeSelector.Item1),
                    new XElement("filename", "")));
            }
            root.Save(gameController!.DisplayName.Replace(' ', '_') + "_" + Mode + ".xml");
            MessageBox.Show("Config saved successfully with filename " + gameController!.DisplayName.Replace(' ', '_') + "_" + Mode + ".xml",
                "Saved Notification", MessageBoxButton.OK, MessageBoxImage.Information);
            LoadConfig();
            Thread.Sleep(100);
        }
    }
}
