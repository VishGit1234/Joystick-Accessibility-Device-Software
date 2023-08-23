using System;
using System.Windows;
using Controller.Models;
using Controller.Views;

namespace Controller.Commands
{
    class SaveConfigCommand : CommandBase
    {
        public override void Execute(object? parameter)
        {
            ((ControllerModel)parameter!).SaveConfig();
        }
    }

    class LoadConfigCommand : CommandBase
    {
        public override void Execute(object? parameter)
        {
            ((ControllerModel)parameter!).LoadConfig(true);
        }
    }

    class PlayAudioCommand : CommandBase
    {
        public override bool CanExecute(object? parameter)
        {
            if (parameter == null) return false;
            return ((ControllerInputModel)parameter!).AudioType != "None";
        }
        public override void Execute(object? parameter)
        {
            if (((ControllerInputModel)parameter!).AudioType == "Tone") 
            {
                // Open Tone Selection Window
                var toneSelectionWindow = new ToneSelectorWindow();
                toneSelectionWindow.ShowDialog();
                var str = toneSelectionWindow.Frequency.ToString() + "hz";
                if(!ControllerInputModel.AudioTypes.Contains(str)) ControllerInputModel.AudioTypes.Insert(0, str);
                ((ControllerInputModel)parameter!).AudioType = str;
            }
            else if (((ControllerInputModel)parameter!).AudioType == "Record") 
            {
                // Open Recording Window
                var recordWindow = new RecordWindow();
                recordWindow.ShowDialog();
                ControllerInputModel.AudioTypes.Insert(0, recordWindow.filename);
                ((ControllerInputModel)parameter!).AudioType = recordWindow.filename;
            }
            else if (((ControllerInputModel)parameter!).Sound != null)
            {
                AudioPlaybackEngine.Instance.StopAll();
                if (((ControllerInputModel)parameter!).AudioType.Contains("hz"))
                {
                    AudioPlaybackEngine.Instance.PlaySound((ControllerInputModel)parameter!, Int32.Parse(((ControllerInputModel)parameter!).AudioType.Replace("hz", "")), 700);
                }
                else
                {
                    AudioPlaybackEngine.Instance.PlaySound((ControllerInputModel)parameter!);
                }
            }
        }
    }
}