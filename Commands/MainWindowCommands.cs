using Controller.Models;
using Controller.Views;
using Controller.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows;

namespace Controller.Commands
{
    class OpenConfigCommand : CommandBase
    {
        public override bool CanExecute(object? parameter)
        {
            if(parameter == null) return false; 
            return true;
        }

        public override void Execute(object? parameter)
        {
            AudioPlaybackEngine.Instance.StopAll();
            ((ControllerModel)parameter!).resetEvent.Reset();
            var configWindow = new ControllerConfigWindow(((ControllerModel)parameter!).Mode, ((ControllerModel)parameter!).GetControllerCount());
            configWindow.ShowDialog();
            ((ControllerModel)parameter!).LoadConfig();
            Thread.Sleep(100);
            ((ControllerModel)parameter!).resetEvent.Set();
        }
    }

    class OpenWindowComand : CommandBase
    {
        public override void Execute(object? parameter)
        {
            var param = (MainWindowViewModel.OpenWindowParameterClass)parameter!;
            var modeName = param.Mode;
            if (modeName.Contains("Game"))
            {
                var gameProcess = Process.Start("\"C:\\WINDOWS\\system32\\cmd.exe\""); // Replace with game application
                MainWindowViewModel.GettingInput = false;
                Task.Run(() =>
                {
                    while(!gameProcess.HasExited) { Thread.Sleep(100); }
                    MainWindowViewModel.GettingInput = true;
                });
            }
            else if (modeName.Contains("Learn"))
            {
                //Open teaching window
                var teachindModeWindow = new TeachingMode();
                var viewModel = new TeachingModeViewModel(ref param.Throttle, ref param.Joystick);
                teachindModeWindow.DataContext = viewModel;
                MainWindowViewModel.GettingInput = false;
                teachindModeWindow.Closed += (object? sender, EventArgs e) => { MainWindowViewModel.GettingInput = true; viewModel.Close(); };
                teachindModeWindow.ShowDialog();
            }
        }
    }
}
