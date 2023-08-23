using Controller.Commands;
using Controller.Models;
using NAudio.Wave;
using System;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Windows.Gaming.Input;

namespace Controller.ViewModels
{
    class MainWindowViewModel : ViewModelBaseClass
    {
        public ICommand OpenConfigCommand { get; set; }

        public ICommand OpenWindowComand { get; set; }

        public ControllerModel? throttle { get; set; }

        public ControllerModel? joystick { get; set; }

        private enum Modes {Play, Learn, Game};

        private string modeName = "Play Mode";
        public string ModeName
        {
            get { return modeName; }
            set 
            { 
                modeName = value; OnPropertyChanged(nameof(ModeName)); 
                if(_mode == Modes.Game)
                {
                    ButtonVisibility = "Visible";
                    ButtonText = "Open Games Application";
                }
                else if (_mode == Modes.Learn)
                {
                    ButtonVisibility = "Visible";
                    ButtonText = "Open Teaching Mode";
                }
                else
                {
                    ButtonVisibility = "Collapsed";
                }
                OpenWindowParameter.Mode = modeName;
                OnPropertyChanged(nameof(ButtonText));
                OnPropertyChanged(nameof(ButtonVisibility));
            }
        }

        public class OpenWindowParameterClass
        {
            public string Mode;
            public ControllerModel? Throttle;
            public ControllerModel? Joystick;

            public OpenWindowParameterClass(string mode, ControllerModel? throttle, ControllerModel? joystick)
            {
                Mode = mode;
                Throttle = throttle;
                Joystick = joystick;
            }
        }
        public OpenWindowParameterClass OpenWindowParameter { get; set; }

        private Modes _mode = Modes.Play;
        private Modes mode
        {
            get { return _mode; }
            set 
            {
                _mode = value; 
                ModeName = _mode + " Mode"; 
                if(throttle != null) throttle!.Mode = _mode.ToString();
                if (joystick != null) joystick!.Mode = _mode.ToString();
                AudioPlaybackEngine.Instance.StopAll();
            }
        }

        public string ButtonVisibility { get; private set; } = "Collapsed";

        public string ButtonText { get; private set; } = "Open Teaching Mode";

        private const int SAMPLE_RATE = 44100;

        public static bool GettingInput { private get; set; } = true;

        private bool isRecording = false;
        private string buttonBeingRecorded = "";

        private static WaveInEvent? recorder;
        private static WaveFileWriter? waveFileWriter;

        private static void RecorderOnDataAvailable(object? sender, WaveInEventArgs waveInEventArgs)
        {
            if (waveFileWriter != null)
            {
                waveFileWriter.Write(waveInEventArgs.Buffer, 0, waveInEventArgs.BytesRecorded);
                waveFileWriter.Flush();
            }
        }

        private bool audioBeingUsed = false;

        private string startRecording()
        {
            if (audioBeingUsed) { return ""; }
            audioBeingUsed = true;

            // set up the recorder
            AudioPlaybackEngine.Instance?.StopAll();
            recorder = new WaveInEvent();
            recorder.WaveFormat = new WaveFormat(SAMPLE_RATE, 1);
            recorder.DataAvailable += RecorderOnDataAvailable;

            // begin record
            recorder.StartRecording();

            //Open file
            string name = Guid.NewGuid().ToString();
            waveFileWriter = new WaveFileWriter(name + ".wav", recorder.WaveFormat);

            return name;
        }
        private void stopRecording()
        {
            // stop recording
            if (recorder != null) { recorder!.StopRecording(); recorder!.Dispose(); recorder = null; }
            // finalise the WAV file
            if (waveFileWriter != null) { waveFileWriter.Dispose(); waveFileWriter = null; }
            audioBeingUsed = false;
        }

        private ControllerInputModel? recordButton;
        private string? fileName;


        private void makeNoises(object? param, EventArgs e)
        {
            if (param != null)
            {
                ControllerInputModel input = (ControllerInputModel)(param!);
                bool isButton = Boolean.TryParse(input.Value, out bool buttonValue);
                if(isButton)
                {
                    if (buttonValue)
                    {
                        if (input.InputType.Equals("ModeSelector") || input.InputType.Equals("RecordButton"))
                        {
                            if (GettingInput)
                            {
                                if(!isRecording)
                                {
                                    if (input.HardwareID == 33) mode = Modes.Play;
                                    else if (input.HardwareID == 34) mode = Modes.Learn;
                                    else if (input.HardwareID == 35) mode = Modes.Game;
                                    else
                                    {
                                        isRecording = true;
                                    }
                                }
                            }
                        }
                        else if (isRecording && !input.InputType.Equals("ClickWheel"))
                        {
                            if (buttonBeingRecorded == "")
                            {
                                buttonBeingRecorded = input.HardwareID.ToString() + input.Name;
                                fileName = startRecording();
                                Thread.Sleep(100);
                                recordButton = input;
                            }
                        }
                        else if (input.Sound != null)
                        {
                            if (input.AudioType.LastIndexOf('z') == (input.AudioType.Length - 1) && input.AudioType.LastIndexOf('h') == (input.AudioType.Length - 2))
                                AudioPlaybackEngine.Instance.PlaySound(input, Int32.Parse(input.AudioType.Replace("hz", "")), 200);
                            else
                            {
                                AudioPlaybackEngine.Instance.PlaySound(input);
                            }
                        }
                    }
                    else if (isRecording && (buttonBeingRecorded.Equals(input.HardwareID.ToString() + input.Name) || input.InputType.Equals("RecordButton")))
                    {
                        stopRecording(); recordButton!.AudioType = fileName!;
                        isRecording = false;
                        buttonBeingRecorded = "";
                        if(input.InputType.Equals("RecordButton"))
                                isRecording = true;
                    }
                    else
                    {
                        if (input.Sound != null)
                        {
                            if (!(input.AudioType.LastIndexOf('z') == (input.AudioType.Length - 1) && input.AudioType.LastIndexOf('h') == (input.AudioType.Length - 2)))
                            {
                                //Check if sound length is greater than 300 milliseconds
                                if (((CachedSound)input.Sound).AudioData.Length / (SAMPLE_RATE * 2.0) >= 0.3)
                                {
                                    AudioPlaybackEngine.Instance.Stop(input);
                                }
                            }
                        }
                    }
                }

                bool isAxis = Int32.TryParse(input.Value, out int axisValue);

                if (isAxis)
                {
                    int upperLimit = (input.InputType == "Axis") ? 90 : 101;
                    int lowerLimit = (input.InputType == "Axis") ? 10 : 5;

                    if (input.Name.Contains("Stick") || input.Name.Contains("Joystick"))
                    {
                        axisValue -= 50;
                        axisValue = Math.Abs(axisValue) * 2;
                        upperLimit = 101;
                    }

                    // Generate a sine wave signal
                    if (input.AudioType.LastIndexOf('z') == (input.AudioType.Length - 1) && input.AudioType.LastIndexOf('h') == (input.AudioType.Length - 2))
                    {
                        if (input.Sound != null)
                        {
                            if (axisValue < upperLimit && axisValue > lowerLimit)
                            {
                                AudioPlaybackEngine.Instance.PlaySound(input, (axisValue * 2) + 700);
                            }
                            else
                            {
                                AudioPlaybackEngine.Instance.PlaySound(input, 0);
                            }
                        }
                    }
                    //Pitch Bend a Cached Sound
                    else
                    {
                        if (input.Sound != null)
                        {
                            if (axisValue < upperLimit && axisValue > lowerLimit)
                            {
                                AudioPlaybackEngine.Instance.PlaySound(input, (float)((axisValue / 100.0f) * 2.0f));
                            }
                            else
                            {
                                AudioPlaybackEngine.Instance.Stop(input);
                            }   
                        }
                    }
                }
            }
        }

        public bool isShuttingDown { get; private set; } = false;

        public MainWindowViewModel()
        {
            OpenConfigCommand = new OpenConfigCommand();
            OpenWindowComand = new OpenWindowComand();
            OpenWindowParameter = new OpenWindowParameterClass("", null, null);

            // Wait for program to detect game controllers
            int waitCount = 0;
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
                        return;
                    }
                }
            }
            throttle = new ControllerModel("Play", 0);
            if (throttle!.isShuttingDown) { isShuttingDown = true; return; }
            if (RawGameController.RawGameControllers.Count >= 2)
            {
                joystick = new ControllerModel("Play", 1);
                if (joystick!.isShuttingDown) { isShuttingDown = true; return; }
                joystick.InputActivated += makeNoises;
            }


            throttle.InputActivated += makeNoises;
            OpenWindowParameter = new OpenWindowParameterClass(modeName, throttle, joystick);
        }
    }
}
