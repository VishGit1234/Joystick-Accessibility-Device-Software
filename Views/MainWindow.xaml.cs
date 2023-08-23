using Controller.ViewModels;
using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Automation;
using Windows.Gaming.Input;

namespace Controller.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        [DllImport("shell32.dll")]
        private static extern int ShellExecute(IntPtr hWnd, string lpOperaiont, string lpFile, string lpParamaters, string? lpDirectory, int nShowCmd);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr FindWindow(string? lpClassName, string lpWindowName);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        private const UInt32 WM_CLOSE = 0x0010;

        public MainWindow()
        {
            InitializeComponent();
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
                            return;
                        }
                    }
                }
            }
            OpenGameControllerWindow();
            DataContext = new MainWindowViewModel();
            if (((MainWindowViewModel)DataContext).isShuttingDown) return;

        }

        private void OpenGameControllerWindow()
        {
            string controlPanelPath = Environment.GetFolderPath(Environment.SpecialFolder.System) + @"\control.exe";
            string parameters = "joy.cpl";

            //Open Game Controller Window
            ShellExecute(IntPtr.Zero, "open", controlPanelPath, parameters, null, 1);

            //Wait for window to open
            Thread.Sleep(200);

            //Get Window Handle
            IntPtr windowHandle = FindWindow("#32770", "Game Controllers");

            //Get AutomationElement
            AutomationElement windowElement = AutomationElement.FromHandle(windowHandle);

            //Get Throttle Item in Listbox
            AutomationElement listBoxElement = windowElement.FindFirst(
                TreeScope.Descendants,
                new PropertyCondition(AutomationElement.NameProperty, "Saitek Pro Flight X-56 Rhino Throttle"));
            //Press Button (Opening up Properties Window)
            try
            {
                if (listBoxElement != null)
                {
                    SelectionItemPattern invokePattern = (SelectionItemPattern)listBoxElement.GetCurrentPattern(SelectionItemPattern.Pattern);
                    invokePattern.Select();
                    Thread.Sleep(100);
                }
            }
            catch { }

            //Get Properties Button
            AutomationElement buttonElement = windowElement.FindFirst(
                TreeScope.Descendants,
                new PropertyCondition(AutomationElement.AutomationIdProperty, "1002"));

            //Press Button (Opening up Properties Window)
            try
            {
                if (buttonElement != null)
                {
                    InvokePattern invokePattern = (InvokePattern)buttonElement.GetCurrentPattern(InvokePattern.Pattern);
                    Thread thread = new Thread(invokePattern.Invoke);
                    thread.Start();
                    Thread.Sleep(1000);
                }
            }
            catch { }

            //Close opened windows
            try
            {
                IntPtr propertiesWindowHandle = FindWindow("#32770", "Saitek Pro Flight X-56 Rhino Throttle Properties");
                try { SendMessage(propertiesWindowHandle, WM_CLOSE, IntPtr.Zero, IntPtr.Zero); }
                catch { }
                try { SendMessage(windowHandle, WM_CLOSE, IntPtr.Zero, IntPtr.Zero); }
                catch { }
            }
            catch { }
        }
    }
}
