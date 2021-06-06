namespace AnythingToMp3Converter.UI
{
    using AnythingToMp3Converter.UI.Enums;
    using AnythingToMp3Converter.UI.Extensions;
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

        // ListView event handler: update controls on listview changes
        private void OnListViewSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            deleteMediaFileButton.IsEnabled = mediaFilesListView.SelectedItem is MediaFile;
        }

        // Button event handler: set output folder path
        private void OnSetOutputFolderPathButtonClick(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog().Equals(System.Windows.Forms.DialogResult.OK))
                {
                    string selectedPath = dialog.SelectedPath;
                    if (Directory.Exists(selectedPath)) outputFolderPathTextBox.Text = selectedPath;
                }
            }
        }

        // Button event handler: add files to listview (inserted as MediaFile)
        private void OnAddFilesButtonClick(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Select any non MP3 file(s)",
                Filter = "All Media Files|*.wav;*.aac;*.wma;*.wmv;*.avi;*.mpg;*.mpeg;*.m1v;*.mp2;*.mpa;*.mpe;*.m3u;*.mp4;*.mov;*.3g2;*.3gp2;*.3gp;*.3gpp;*.m4a;*.cda;*.aif;*.aifc;*.aiff;*.mid;*.midi;*.rmi;*.mkv;*.WAV;*.AAC;*.WMA;*.WMV;*.AVI;*.MPG;*.MPEG;*.M1V;*.MP2;*.MPA;*.MPE;*.M3U;*.MP4;*.MOV;*.3G2;*.3GP2;*.3GP;*.3GPP;*.M4A;*.CDA;*.AIF;*.AIFC;*.AIFF;*.MID;*.MIDI;*.RMI;*.MKV",
                Multiselect = true
            };

            // Get files
            if ((bool)openFileDialog.ShowDialog())
            {
                int totalFailed = 0;
                List<FileInfo> newFiles = openFileDialog.FileNames.SelectAs(f => new FileInfo(f)).ToList();
                if (newFiles.Count <= 0) return;

                // Go tru all selected files
                foreach (FileInfo fileInfo in newFiles)
                {
                    try
                    {
                        if (mediaFilesListView.Items.OfType<MediaFile>().GotAny(mf => mf.FileName.Equals(fileInfo.Name.RemoveValue(fileInfo.Extension)))) continue;
                        mediaFilesListView.Items.Add(new MediaFile
                        {
                            FilePath = fileInfo.FullName,
                            FileName = fileInfo.Name.RemoveValue(fileInfo.Extension),
                            FileFullName = fileInfo.Name,
                            FileStatus = FileStatus.Waiting,
                            Progress = 0
                        });
                    }
                    catch (Exception)
                    {
                        totalFailed++;
                    }
                }

                clearCompletedButton.IsEnabled = mediaFilesListView.Items.OfType<MediaFile>().GotAny(mf => mf.FileStatus.Equals(FileStatus.Completed));
                clearConverterButton.IsEnabled = mediaFilesListView.Items.Count > 0;

                // Note failed files
                if (totalFailed > 0)
                {
                    MessageBox.Show($"\n{totalFailed} {(totalFailed == 1 ? "file" : "files")} failed to add, it is possible that the file is either corrupt or too large. Please try again...", "Something went wrong!");
                }
            }
        }

        // Button event handler: remove selected mediafile from listview
        private void OnRemoveMediaFileButtonClick(object sender, RoutedEventArgs e)
        {
            if (mediaFilesListView.SelectedItem is MediaFile mediaFile)
            {
                mediaFilesListView.Items.Remove(mediaFile);
                clearCompletedButton.IsEnabled = mediaFilesListView.Items.OfType<MediaFile>().GotAny(mf => mf.FileStatus.Equals(FileStatus.Completed));
                clearConverterButton.IsEnabled = mediaFilesListView.Items.Count > 0;
            }
        }

        // Button event handler: clear entire listview
        private void OnClearConverterButtonClick(object sender, RoutedEventArgs e)
        {
            if (mediaFilesListView.Items.Count > 0) mediaFilesListView.Items.Clear();
            clearCompletedButton.Disable();
            clearConverterButton.Disable();
            deleteMediaFileButton.Disable();
        }

        // Button event handler: clear all converted media files from listview
        private void OnClearCompletedButtonClick(object sender, RoutedEventArgs e)
        {
            if (mediaFilesListView.Items.Count > 0)
            {
                foreach (var mediaFile in mediaFilesListView.Items.OfType<MediaFile>().Where(mf => mf.FileStatus.Equals(FileStatus.Completed)).ToList())
                {
                    mediaFilesListView.Items.Remove(mediaFile);
                }
            }

            deleteMediaFileButton.Disable();
            clearCompletedButton.Disable();
            clearConverterButton.IsEnabled = mediaFilesListView.Items.Count > 0;
        }

        // Button event handler: convert files
        private async void OnStartConvertingButtonClickAsync(object sender, RoutedEventArgs e)
        {
            string outputFolderPath = outputFolderPathTextBox.Text;

            // First check if output folder is set
            if (string.IsNullOrWhiteSpace(outputFolderPath) || !Directory.Exists(outputFolderPath))
            {
                MessageBox.Show("No output folder set, cannot start converting!", "Missing output folder");
                return;
            }

            // Dont continue if there are no media files found in listview
            List<MediaFile> mediaFiles = mediaFilesListView.Items.OfType<MediaFile>().ToList();
            if (mediaFiles.Count <= 0)
            {
                MessageBox.Show("No files found to convert, converting aborted!", "No files found");
                return;
            }

            listViewBlockerTextBlock.Show();
            clearCompletedButton.Disable();
            clearConverterButton.Disable();
            startConvertingButton.Disable();
            deleteMediaFileButton.Disable();
            addMediaFilesButton.Disable();
            mediaFilesListView.SelectedItem = null;

            string alertMessage = string.Empty;
            int totalFailed = 0;

            // Convert each file from listview
            foreach (var mediaFile in mediaFiles)
            {
                try
                {
                    FFmpeg.SetExecutablesPath(AppDomain.CurrentDomain.BaseDirectory, _ffmpegFileName, _ffprobeFileName);
                    string outputFileName = string.Concat(Path.Combine(outputFolderPath, mediaFile.FileName), ".mp3");
                    IMediaInfo mediaInfo = await FFmpeg.GetMediaInfo(mediaFile.FilePath);
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
                        mediaFile.Progress = args.Percent;
                        RefreshListView();
                    };

                    // Change file state
                    mediaFile.FileStatus = FileStatus.Converting;
                    mediaFile.Progress = 0;

                    // Convert
                    RefreshListView();
                    await conversion.Start();

                    // Change file state on completion
                    mediaFile.FileStatus = FileStatus.Completed;
                    mediaFile.Progress = 100;
                }
                catch (Exception)
                {
                    mediaFile.FileStatus = FileStatus.Failed;
                    mediaFile.Progress = 0;

                    totalFailed++;
                }

                RefreshListView();
            }

            // Report messages
            if (totalFailed > 0) alertMessage = $"\n{totalFailed} {(totalFailed == 1 ? "file" : "files")} failed to convert, it is possible that the file is either corrupt or too large. You can either skip or try convert them again.";
            MessageBox.Show(string.Concat("Converting completed.", alertMessage), "DONE!", MessageBoxButtons.OK);

            clearCompletedButton.IsEnabled = mediaFilesListView.Items.OfType<MediaFile>().GotAny(mf => mf.FileStatus.Equals(FileStatus.Completed));
            clearConverterButton.IsEnabled = mediaFilesListView.Items.Count > 0;
            startConvertingButton.Enable();
            addMediaFilesButton.Enable();
            listViewBlockerTextBlock.Hide();
        }

        // Refresh listview when items are been updated
        private void RefreshListView()
        {
            Dispatcher.Invoke(() => mediaFilesListView.Items.Refresh());
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
                    ffmpegVerificationStackPanel.Show();
                    await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official, path);

                    // Remove unused folder (macOS files)
                    string macOsFilesFolder = Path.Combine(path, "__MACOSX"); // Only support winOS
                    if (Directory.Exists(macOsFilesFolder)) Directory.Delete(macOsFilesFolder, true);

                    // Fade out effect after install completion
                    ffmpegVerificationStackPanel.ContentTemplate = TryFindResource("VerifiedFfmpegDataTemplate") as DataTemplate;
                    ffmpegVerificationStackPanel.BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromSeconds(2)));

                    // Delay before hiding panel
                    await Task.Delay(2000);
                    ffmpegVerificationStackPanel.Collapse();
                }

            }
            catch (Exception e)
            {
                MessageBox.Show($"An unexpected error happend while trying to verify FFmpeg.\n{e.Message}", "Something went wrong!", MessageBoxButtons.OK);
            }
        }
    }
}
