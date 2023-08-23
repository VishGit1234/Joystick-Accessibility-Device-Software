using Controller.ViewModels;
using NAudio.Wave;
using System;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Controller.Views
{
    /// <summary>
    /// Interaction logic for RecordWindow.xaml
    /// </summary>
    public partial class RecordWindow : Window
    {
        const int MAX_MILLISECONDS_RECORD = 1000;

        private WaveIn? recorder;
        private WaveFileWriter? waveFileWriter;

        public RecordWindow()
        {
            InitializeComponent();
            DataContext = this;
        }


        private void OnStartRecordingClick(object sender, RoutedEventArgs e)
        {
            if(audioBeingUsed) { return; }
            audioBeingUsed = true;
            ProgressIndicatorOut.StrokeDashArray[0] = 0;
            ProgressIndicatorOut.Stroke = new SolidColorBrush(Colors.Brown);
            ProgressIndicatorIn.Fill = new SolidColorBrush(Colors.Brown);
            // set up the recorder
            recorder = new WaveIn();
            recorder.DataAvailable += RecorderOnDataAvailable;

            // begin record
            recorder.StartRecording();

            //Open file
            waveFileWriter = new WaveFileWriter("temp.wav", recorder.WaveFormat);

            isWorkerDone = false;
            worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.WorkerSupportsCancellation = true;
            worker.RunWorkerCompleted += worker_Done;
            worker.DoWork += worker_DoWork;
            worker.ProgressChanged += worker_ProgressChanged;
            worker.RunWorkerAsync();

        }

        private BackgroundWorker? worker;

        

        private void worker_DoWork(object? sender, DoWorkEventArgs e)
        {
            for (int i = 0; i < 100; i++)
            {
                if (((BackgroundWorker)sender!).CancellationPending) break;
                (sender as BackgroundWorker)!.ReportProgress(i);
                Thread.Sleep(MAX_MILLISECONDS_RECORD/100);
            }
        }

        private void worker_ProgressChanged(object? sender, ProgressChangedEventArgs e)
        {
            ProgressIndicatorOut.StrokeDashArray[0] = e.ProgressPercentage * (0.6);
        }

        private void worker_Done(object? sender, RunWorkerCompletedEventArgs e)
        {
            if (!isWorkerDone) OnStopRecordingClick(null, null);
            isWorkerDone = true;
        }

        private bool isWorkerDone = false;

        private bool audioBeingUsed = false;


        private void RecorderOnDataAvailable(object? sender, WaveInEventArgs waveInEventArgs)
        {
            if (waveFileWriter != null)
            {
                waveFileWriter.Write(waveInEventArgs.Buffer, 0, waveInEventArgs.BytesRecorded);
            }
        }

        public string filename = "";

        private void OnStopRecordingClick(object? sender, MouseButtonEventArgs? e)
        {
            if (!isWorkerDone)
            {
                isWorkerDone = true;
                if(worker != null) worker.CancelAsync();
                Thread.Sleep(50);
            }
            ProgressIndicatorOut.StrokeDashArray[0] = 0;
            ProgressIndicatorOut.Stroke = new SolidColorBrush(Colors.Red);
            ProgressIndicatorIn.Fill = new SolidColorBrush(Colors.Red);
            // stop recording
            if (recorder != null) { recorder!.StopRecording(); recorder!.Dispose(); recorder = null; }
            // finalise the WAV file
            if(waveFileWriter != null) { waveFileWriter.Dispose(); waveFileWriter = null; }

            var saveWindow = new RecordedFileNameDialogWindow();
            saveWindow.ShowDialog();
            filename = saveWindow.filename;

            try { System.IO.File.Delete(filename + ".wav"); } catch { }
            System.IO.File.Move("temp.wav", filename + ".wav");

            PreviewButton.Visibility = Visibility.Visible;
            audioBeingUsed = false;
        }

        private WaveFileReader? waveFileReader;
        private WaveOut? waveOut;

        private void OnPreviewClick(object? sender, RoutedEventArgs e)
        {
            if(audioBeingUsed) { return; }
            audioBeingUsed = true;
            waveFileReader = new WaveFileReader(filename + ".wav");
            waveOut = new WaveOut();
            waveOut.PlaybackStopped += onPlaybackStopped;
            waveOut.Init(waveFileReader);
            waveOut.Play();
        }

        private void onPlaybackStopped(object? sender, StoppedEventArgs e)
        {
            waveOut?.Dispose();
            waveFileReader?.Dispose();
            audioBeingUsed = false;
        }
    }
}
