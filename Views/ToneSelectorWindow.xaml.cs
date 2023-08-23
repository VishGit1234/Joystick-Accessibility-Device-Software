using Controller.Models;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Windows.Media.Capture;

namespace Controller.Views
{
    /// <summary>
    /// Interaction logic for ToneSelectorWindow.xaml
    /// </summary>
    public partial class ToneSelectorWindow : Window
    {
        public ToneSelectorWindow()
        {
            InitializeComponent();
        }

        private WaveOut? waveOut;

        public int Frequency = 0;

        private void onPreviewClick(object? sender, RoutedEventArgs e)
        {
            waveOut = new WaveOut();
            var signal = new OffsetSampleProvider(new SineWaveProvider((int)slValue.Value));
            
            signal.Take = TimeSpan.FromMilliseconds(1000);
            waveOut.PlaybackStopped += onPlayStopped;
            waveOut.Init(signal);
            waveOut.Play();
        }

        private void onPlayStopped(object? sender, StoppedEventArgs e)
        {
            waveOut?.Dispose();
        }

        private void slValue_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Frequency = (int)e.NewValue;
        }
    }
}
