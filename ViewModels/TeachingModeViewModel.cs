using Controller.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Controller.ViewModels
{
    class TeachingModeViewModel : ViewModelBaseClass
    {
        public ObservableCollection<string> AvailableButtons { get; private set; }

        private List<int> indicesController1;
        private List<int> indicesController2;

        private bool isPlaying = false;

        private int chosenController = 0;

        private int chosenButtonIndex;
        public int ChosenButtonIndex
        {
            get
            {
                return chosenButtonIndex;
            }
            set
            {
                var previousButton = chosenButtonIndex;
                var previousController = chosenController;
                chosenButtonIndex = value;
                if (AvailableButtons[chosenButtonIndex].Contains("Controller 1"))
                {
                    chosenController = 1;
                }
                else if (AvailableButtons[chosenButtonIndex].Contains("Controller 2"))
                {
                    chosenController = 2;
                }
                if(isPlaying)
                {
                    var result = MessageBox.Show("Are you sure would like to change?", "Change Button", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (result == DialogResult.No)
                    {
                        chosenButtonIndex = previousButton;
                        chosenController = previousController;
                    }
                }
                isPlaying = true;
            }
        }

        private ControllerModel? throttle;
        private ControllerModel? joystick;

        public TeachingModeViewModel(ref ControllerModel? _throttle, ref ControllerModel? _joystick)
        {
            throttle = _throttle;
            joystick = _joystick;
            AvailableButtons = new ObservableCollection<string>();
            indicesController1 = new List<int>();
            indicesController2 = new List<int>();
            if (throttle != null)
            {
                int i = 0;
                foreach (var input in throttle.ControllerInputs)
                {
                    if (input.InputType == "Button" || input.InputType == "Switch" || input.InputType == "Toggle")
                    {
                        AvailableButtons.Add("Controller 1 | " + input.Name);
                        indicesController1.Add(i);
                    }
                    i++;
                }
                throttle.InputActivated += checkCorrectButton;
            }
            if (joystick != null)
            {
                int i = 0;
                foreach (var input in joystick.ControllerInputs)
                {
                    if (input.InputType == "Button" || input.InputType == "Switch" || input.InputType == "Toggle")
                    {
                        AvailableButtons.Add("Controller 2 | " + input.Name);
                        indicesController2.Add(i);
                    }
                    i++;
                }
                joystick.InputActivated += checkCorrectButton;
            }
        }

        private void checkCorrectButton(object? param, EventArgs e)
        {
            ControllerInputModel input = (ControllerInputModel)(param!);
            if(chosenController == 1)
            {
                if (input.Equals(throttle!.ControllerInputs[indicesController1[chosenButtonIndex]]))
                {
                    chosenButtonIndex = 0;
                    // Done!!
                    Task.Run(() => {
                        throttle.resetEvent.Reset();
                        AudioPlaybackEngine.Instance.PlayCachedSound(new CachedSound("goodjobtest.mp3"));
                        while (!AudioPlaybackEngine.Instance.NoSounds()) { Thread.Sleep(100); }
                        throttle.resetEvent.Set();
                    });
                    MessageBox.Show("Good Job!!");
                    isPlaying = false;
                }
            }
            if (chosenController == 2)
            {
                if (input.Equals(joystick!.ControllerInputs[indicesController1[chosenButtonIndex]]))
                {
                    // Done!!
                    chosenButtonIndex = 0;
                    Task.Run(() => {
                        joystick.resetEvent.Reset();
                        AudioPlaybackEngine.Instance.PlayCachedSound(new CachedSound("goodjobtest.mp3"));
                        while (!AudioPlaybackEngine.Instance.NoSounds()) { Thread.Sleep(100); }
                        joystick.resetEvent.Set();
                    });
                    MessageBox.Show("Good Job!!");
                    isPlaying = false;
                    
                }
            }
        }

        public void Close()
        {
            if(throttle != null) throttle.InputActivated -= checkCorrectButton;
            if(joystick != null) joystick.InputActivated -= checkCorrectButton;
        }


    }
}
