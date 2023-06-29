using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;
using MessageBox = System.Windows.Forms.MessageBox;

namespace PixelPowerPreInterviewAssessment
{
    public partial class MainWindow : Window
    {
        private CancellationTokenSource _cancellationTokenSource;
        public MainWindow()
        {
            InitializeComponent();
            updateDir();
        }

        private void OpenFileBtn_Click(object sender, RoutedEventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog() { Description = "Select your path:" })
            {
                if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    webBrowser.Navigate(fbd.SelectedPath);
                    PathTextBox.Text = fbd.SelectedPath;
                }
            }
        }

        private void backBtn_Click(object sender, RoutedEventArgs e)
        {
            if (webBrowser.CanGoBack) webBrowser.GoBack();
            PathTextBox.Text = webBrowser.Source?.LocalPath;
        }

        private void fwdBtn_Click(object sender, RoutedEventArgs e)
        {
            if (webBrowser.CanGoForward) webBrowser.GoForward();
            PathTextBox.Text = webBrowser.Source?.LocalPath;
        }

        private bool _updateIsRunning = false;

        private async void updateDir()
        {
            if (_updateIsRunning) return;
            _updateIsRunning = true;

            while (true)
            {
                PathTextBox.Text = webBrowser.Source?.LocalPath;
                await Task.Delay(20);
            }

        }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            StartButton.IsEnabled = false;
            CancelButton.IsEnabled = true;
            FileListBox.Items.Clear();
            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                await ListFilesAsync(PathTextBox.Text, _cancellationTokenSource.Token);
                MessageBox.Show("File listing completed successfully.", "Success", (MessageBoxButtons)MessageBoxButton.OK, (MessageBoxIcon)MessageBoxImage.Information);
            }
            catch (OperationCanceledException)
            { MessageBox.Show("File listing canceled.", "Canceled", (MessageBoxButtons)MessageBoxButton.OK, (MessageBoxIcon)MessageBoxImage.Warning); }
            catch (Exception ex)
            { MessageBox.Show("An error occurred: " + ex.Message, "Error", (MessageBoxButtons)MessageBoxButton.OK, (MessageBoxIcon)MessageBoxImage.Error); }
            finally
            { // Reset
                StartButton.IsEnabled = true;
                CancelButton.IsEnabled = false;
                ProgressBar.Value = 0;
                ProgressTextBlock.Text = "Progress: 0%";
            }
        }

        private async Task ListFilesAsync(string directory, CancellationToken cancellationToken)
        {
            string[] files = null;
            try
            { files = Directory.GetFiles(directory); }
            catch (UnauthorizedAccessException)
            { return; }
            catch (DirectoryNotFoundException)
            { return; }

            int totalFiles = files.Length;
            int completedFiles = 0;

            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                FileListBox.Items.Add(file);
                completedFiles++;
                double progressPercentage = (completedFiles / (double)totalFiles) * 100;
                ProgressBar.Value = progressPercentage;
                ProgressTextBlock.Text = $"Progress: {progressPercentage:F1}%";
                await Task.Delay(10);
            } // since there isnt a simple way to find the total number of files within subdirectories. 

            string[] subDirectories = null;
            try
            { subDirectories = Directory.GetDirectories(directory); }
            catch (UnauthorizedAccessException)
            { return; }
            catch (DirectoryNotFoundException)
            { return; }

            foreach (var subDirectory in subDirectories)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ListFilesAsync(subDirectory, cancellationToken);
            }
        }


        private void CancelButton_Click(object sender, RoutedEventArgs e)
        { _cancellationTokenSource?.Cancel(); }

        private void HelpMessage_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("This application displays all the files in a folder.\n<< takes you back in the directory\n>> takes you forward in the directory" +
                "\nOpen file allows you to navigate to the folder location using your system file explorer." +
                "\nStart starts the file listing and Cancel stops this" +
                "\nThis is a browser which allows you to navigate folders without using your system file explorer ");
        }
    }
}