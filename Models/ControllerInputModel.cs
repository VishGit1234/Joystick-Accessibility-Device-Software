using Controller.Commands;
using Controller.ViewModels;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;

namespace Controller.Models
{
    /// <summary>
    /// Contains properties and methods to describe an input on the controller
    /// </summary>
    class ControllerInputModel : ModelBaseClass
    {
        private enum INPUT_TYPES { ModeSelector, Button, Switch, Toggle, Hat, Slide, Wheel, Knob, Axis, ClickWheel, NoInput }

        private enum AUDIO_TYPES { Tone, Record, None };

        /// <summary>
        /// List of input types for binding with combobox
        /// (ModeSelector, Button, Switch, Toggle, Hat, Slide, Wheel, Knob, Axis, ClickWheel, NoInput)
        /// </summary>
        public static List<string> InputTypes { get; set; } = new List<string>(Enum.GetNames(typeof(INPUT_TYPES)));

        /// <summary>
        /// List of audio types for binding with combobox
        /// ( Audio1, Audio2, Audio3, Record)
        /// </summary>
        public static List<string> AudioTypes { get; set; } = new List<string>(Enum.GetNames(typeof(AUDIO_TYPES)));
        
        /// <summary>
        /// The array index that this input's value can be accessed at
        /// </summary>
        public int HardwareID { get; set; } = -1;

        public string ControllerName { get; private set; }

        private string inputType = string.Empty;
        /// <summary>
        /// The input type of this input
        /// </summary>
        public string InputType
        {
            get { return inputType; }
            set
            { inputType = value; OnPropertyChanged(nameof(InputType)); }
        }

        private string name = string.Empty;
        /// <summary>
        /// Input name
        /// </summary>
        public string Name
        {
            get { return name; }
            set
            {
                //Replace spaces with underscores
                name = value.Replace(' ', '_');
                OnPropertyChanged(nameof(Name));
            }
        }

        private string val = string.Empty;
        /// <summary>
        /// Current raw value of input
        /// </summary>
        public string Value
        {
            get { return val; }
            set { val = value; OnPropertyChanged(nameof(Value)); }
        }

        public int ID { get; set; } = 0;

        public string AudioButtonName { get; private set; } = string.Empty;

        private string audioType = string.Empty;
        public string AudioType
        {
            get { return audioType; }
            set
            { 
                audioType = value;
                OnPropertyChanged(nameof(AudioType));
                if (!string.IsNullOrEmpty(value) && value != "None")
                {
                    if (audioType == "Record") AudioButtonName = "Record";
                    else if (audioType == "Tone") AudioButtonName = "Select Tones";
                    else AudioButtonName = "▶";
                    OnPropertyChanged(nameof(AudioButtonName));
                    if (!audioType.Equals("Record") && !audioType.Equals("Tone"))
                    {
                        if (audioType.Contains("hz"))
                        {
                            Sound = new SineWaveProvider();
                        }
                        else
                        {
                            try
                            {
                                Sound = new CachedSound(audioType + ".mp3");
                            }
                            catch
                            {
                                Sound = new CachedSound(audioType + ".wav");
                            }
                        }
                    }
                }
                else
                {
                    if (audioType != "None")
                    {
                        audioType = "None";
                    }
                    Sound = null;
                    AudioButtonName = "";
                    OnPropertyChanged(nameof(AudioButtonName));
                }
            }
        }

        private object? sound = null;

        public object? Sound
        {
            get { return sound; }
            set
            {
                sound = value;
                OnPropertyChanged(nameof(Sound));
            }
        }

        public ControllerInputModel(int hardwareID, string inputType, string name, string controllerName, string filename = "")
        {
            ControllerName = controllerName;
            HardwareID = hardwareID;
            InputType = inputType;
            Name = name;
            if (!string.IsNullOrWhiteSpace(filename))
            {
                AudioType = filename;
            }
            else
            {
                AudioType = "None";
            }
        }
    }
}
