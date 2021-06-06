namespace AnythingToMp3Converter.UI
{
    using AnythingToMp3Converter.UI.Enums;
    using AnythingToMp3Converter.UI.Models;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Forms;
    using System.Windows.Media.Animation;
    using Xabe.FFmpeg;
    using Xabe.FFmpeg.Downloader;
    using MessageBox = System.Windows.Forms.MessageBox;
    using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Dispatcher.Invoke(() => VerifyFFmpeg());
        }

        private readonly string _ffmpegFileName = "ffmpeg";
        private readonly string _ffprobeFileName = "ffprobe";

        // Update controls on listview changes
        private void OnListViewSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            btnDelete.IsEnabled = lvMediaFiles.SelectedItem is MediaFile;
        }

        // Set output folder path
        private void BtnSetOutputFolderPath(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog().Equals(System.Windows.Forms.DialogResult.OK))
                {
                    string selectedPath = dialog.SelectedPath;
                    if (Directory.Exists(selectedPath)) txtOutputFolderPath.Text = selectedPath;
                }
            }
        }

        // Add files to listview
        private void BtnBrowseMediaFiles(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Select any non MP3 files",
                Filter = "All Media Files|*.wav;*.aac;*.wma;*.wmv;*.avi;*.mpg;*.mpeg;*.m1v;*.mp2;*.mpa;*.mpe;*.m3u;*.mp4;*.mov;*.3g2;*.3gp2;*.3gp;*.3gpp;*.m4a;*.cda;*.aif;*.aifc;*.aiff;*.mid;*.midi;*.rmi;*.mkv;*.WAV;*.AAC;*.WMA;*.WMV;*.AVI;*.MPG;*.MPEG;*.M1V;*.MP2;*.MPA;*.MPE;*.M3U;*.MP4;*.MOV;*.3G2;*.3GP2;*.3GP;*.3GPP;*.M4A;*.CDA;*.AIF;*.AIFC;*.AIFF;*.MID;*.MIDI;*.RMI;*.MKV",
                Multiselect = true
            };

            // Get files
            if ((bool)openFileDialog.ShowDialog())
            {
                List<string> fileList = openFileDialog.FileNames.ToList();
                if (fileList.Count <= 0) return;

                // Go tru all selected files
                foreach (var file in fileList)
                {
                    var mediaFile = new MediaFile
                    {
                        FilePath = file,
                        FileName = file.Split('\\').Last().Replace(".mp4", string.Empty),
                        FileStatus = FileStatus.Waiting,
                        Progress = 0
                    };

                    if (!lvMediaFiles.Items.Contains(mediaFile))
                    {
                        lvMediaFiles.Items.Add(mediaFile);
                    }
                }

                btnClear.IsEnabled = lvMediaFiles.Items.Count > 0;
            }
        }

        // Remove selected file from listview
        private void BtnRemoveSelectedFile(object sender, RoutedEventArgs e)
        {
            if (lvMediaFiles.SelectedItem is MediaFile mediaFile)
            {
                lvMediaFiles.Items.Remove(mediaFile);
                btnClear.IsEnabled = lvMediaFiles.Items.Count > 0;
            }
        }

        // Clear entire listview
        private void BtnClearList(object sender, RoutedEventArgs e)
        {
            if (lvMediaFiles.Items.Count > 0) lvMediaFiles.Items.Clear();
            btnClear.IsEnabled = false;
            btnDelete.IsEnabled = false;
        }

        // Convert files
        private async void BtnConvertFilesAsync(object sender, RoutedEventArgs e)
        {
            string outputFolderPath = txtOutputFolderPath.Text;

            // First check if output folder is set
            if (string.IsNullOrWhiteSpace(outputFolderPath) || !Directory.Exists(outputFolderPath))
            {
                MessageBox.Show("No output folder set, cannot start converting!", "Missing output folder");
                return;
            }

            // Dont continue if there are no media files found in listview
            List<MediaFile> fileList = lvMediaFiles.Items.OfType<MediaFile>().ToList();
            if (fileList.Count > 0)
            {
                MessageBox.Show("No files found to convert, converting aborted!", "No files found");
                return;
            }

            btnClear.IsEnabled = false;
            btnDelete.IsEnabled = false;
            btnConvertFiles.IsEnabled = false;
            btnAddFiles.IsEnabled = false;

            string alertMessage = string.Empty;
            int totalFailed = 0;

            // Convert each file from listview
            foreach (var file in fileList)
            {
                try
                {
                    FFmpeg.SetExecutablesPath(AppDomain.CurrentDomain.BaseDirectory, _ffmpegFileName, _ffprobeFileName);
                    string outputFileName = string.Concat(Path.Combine(outputFolderPath, file.FileName), ".mp3");
                    IMediaInfo mediaInfo = await FFmpeg.GetMediaInfo(file.FilePath);
                    IStream audioStream = mediaInfo.AudioStreams.FirstOrDefault()?.SetCodec(AudioCodec.mp3);

                    // Create new conversion object
                    IConversion conversion = FFmpeg.Conversions.New()
                        .AddStream(audioStream)
                        .SetOutput(outputFileName)
                        .SetOverwriteOutput(true)
                        .UseMultiThread(false)
                        .SetPreset(ConversionPreset.UltraFast);

                    // Set progress bar event handling
                    conversion.OnProgress += (s, args) =>
                    {
                        file.Progress = args.Percent;
                        RefreshListView();
                    };

                    // Change file state
                    file.FileStatus = FileStatus.Converting;
                    file.Progress = 0;

                    // Convert
                    RefreshListView();
                    await conversion.Start();

                    // Change file state on completion
                    file.FileStatus = FileStatus.Completed;
                    file.Progress = 100;
                }
                catch (Exception)
                {
                    file.FileStatus = FileStatus.Failed;
                    file.Progress = 0;

                    totalFailed++;
                }

                RefreshListView();
            }

            // Report messages
            if (totalFailed > 0) alertMessage = $"\n{totalFailed} {(totalFailed == 1 ? "file" : "files")} failed to convert, it is possible that the file is either corrupt or too large.";
            MessageBox.Show(string.Concat("Converting completed.", alertMessage), "DONE!", MessageBoxButtons.OK);

            btnClear.IsEnabled = true;
            btnConvertFiles.IsEnabled = true;
            btnAddFiles.IsEnabled = true;
        }

        // Refresh listview when items are been updated
        private void RefreshListView()
        {
            Dispatcher.Invoke(() => lvMediaFiles.Items.Refresh());
        }

        // Verify if ffmpeg and ffprobe are installed
        private async Task VerifyFFmpeg()
        {
            try
            {
                // Get ffmpeg and ffprobe if not found yet
                string path = AppDomain.CurrentDomain.BaseDirectory;
                if (!Directory.EnumerateFiles(path).Count(f => f.Contains(_ffmpegFileName) || f.Contains(_ffprobeFileName)).Equals(2))
                {
                    cpUpdater.Visibility = Visibility.Visible;
                    await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official, path);

                    // Remove unused folder (macOS files)
                    string macOsFilesFolder = Path.Combine(path, "__MACOSX"); // Only support winOS
                    if (Directory.Exists(macOsFilesFolder)) Directory.Delete(macOsFilesFolder, true);

                    // Fade out effect after install completion
                    cpUpdater.ContentTemplate = TryFindResource("FFmpegUpdaterCompleted") as DataTemplate;
                    cpUpdater.BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromSeconds(2)));

                    // Delay before hiding panel
                    await Task.Delay(2000);
                    cpUpdater.Visibility = Visibility.Collapsed;
                }

            }
            catch (Exception e)
            {
                MessageBox.Show($"An unexpected error happend while trying to verify FFmpeg.\n{e.Message}", "Something went wrong!", MessageBoxButtons.OK);
            }
        }
    }
}
