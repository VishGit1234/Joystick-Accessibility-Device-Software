using Controller.Commands;
using Controller.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;

namespace Controller.ViewModels
{
    class ControllerConfigViewModel : ViewModelBaseClass
    {
        private ControllerModel throttle;
        public ControllerModel Throttle
        {
            get { return throttle; }
            set { throttle = value; OnPropertyChanged(nameof(Throttle)); }
        }

        private ObservableCollection<ControllerInputModel> controllerInputs;
        public ObservableCollection<ControllerInputModel> ControllerInputs
        {
            get { return controllerInputs; }
            set { controllerInputs = value; OnPropertyChanged(nameof(ControllerInputs)); }
        }

        private CommandBase saveConfigCommand;
        public CommandBase SaveConfigCommand
        {
            get { return saveConfigCommand; }
            set { saveConfigCommand = value; OnPropertyChanged(nameof(SaveConfigCommand)); }
        }

        private CommandBase loadConfigCommand;
        public CommandBase LoadConfigCommand
        {
            get { return loadConfigCommand; }
            set { loadConfigCommand = value; OnPropertyChanged(nameof(LoadConfigCommand)); }
        }

        private CommandBase audioPlayConfigCommand;
        public CommandBase AudioPlayConfigCommand
        {
            get { return audioPlayConfigCommand; }
            set { audioPlayConfigCommand = value; OnPropertyChanged(nameof(AudioPlayConfigCommand)); }
        }

        private Dictionary<int, bool> isAudioButtonEnabled;

        private string audioButtonName(ControllerInputModel input)
        {
            if (input.AudioType == "None") return "None";
            if (input.AudioType == "Tone") return "Tone Selector";
            if (input.AudioType == "Record") return "Sound Recorder";
            else return "▶";
        }


        public ControllerConfigViewModel(string _mode, int _controllerCount)
        {
            throttle = new ControllerModel(_mode, _controllerCount, true);
            controllerInputs = throttle.ControllerInputs;
            saveConfigCommand = new SaveConfigCommand();
            loadConfigCommand = new LoadConfigCommand();
            audioPlayConfigCommand = new PlayAudioCommand();
            isAudioButtonEnabled = new Dictionary<int, bool>(throttle.ControllerInputs.Count);
            var dir = new DirectoryInfo("./");
            foreach(var file in dir.EnumerateFiles())
            {
                if(file.Extension == ".wav" || file.Extension == ".mp3")
                {
                    if (!ControllerInputModel.AudioTypes.Contains(file.Name.Replace(file.Extension, "")))
                    {
                        ControllerInputModel.AudioTypes.Insert(0, file.Name.Replace(file.Extension, ""));
                    }
                }
            }
            int i = 0;
            foreach (var input in throttle.ControllerInputs)
            {
                input.ID = i;
                i++;
                input.PropertyChanged += onThrottlePropertyChanged;
                isAudioButtonEnabled[input.ID] = (input.AudioType != "None");
            }
        }

        private void onThrottlePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "AudioType")
            {
                isAudioButtonEnabled[((ControllerInputModel)sender!).ID] = ((ControllerInputModel)sender!).AudioType != "None";
                audioPlayConfigCommand.OnCanExecuteChanged(null);
            }
        }



        public void OnWindowClosed(object? sender, EventArgs e)
        {
            AudioPlaybackEngine.Instance.StopAll();
            var result = MessageBox.Show("Would you like to save this config?", "Save Config", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes) throttle.SaveConfig();
            throttle.cancellationTokenSource.Cancel();

        }

    }
}
