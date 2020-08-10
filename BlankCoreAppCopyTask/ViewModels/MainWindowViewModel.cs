using BlankCoreAppCopyTask.Services;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Threading;
using Unity;

namespace BlankCoreAppCopyTask.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private readonly ISynchronization _synchronizationMultiThread;
        private readonly ISynchronization _synchronizationOneThread;
        private string _title = "Comparison of copy method";
        private long _sumOfAllFileSize;
        private double _progressValue;
        private bool _canCopy = true;
        private ISynchronization _synchronization;
        private string _logMessage;

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        private string _destinationFolderPath;

        public string DestinationFolderPath
        {
            get => _destinationFolderPath;
            set => SetProperty(ref _destinationFolderPath, value);
        }

        private string _sourceFolderPath;

        public string SourceFolderPath
        {
            get => _sourceFolderPath;
            set => SetProperty(ref _sourceFolderPath, value);
        }

        public long SumOfAllFileSize
        {
            get => _sumOfAllFileSize;
            set => SetProperty(ref _sumOfAllFileSize, value);
        }

        public double ProgressValue
        {
            get => _progressValue;
            set => SetProperty(ref _progressValue, value);
        }

        public string LogMessage
        {
            get => _logMessage;
            set => SetProperty(ref _logMessage, value);
        }

        public DelegateCommand SelectSrcFolderCommand { get; }
        public DelegateCommand SelectDstFolderCommand { get; }
        public DelegateCommand ClearCommand { get; }
        public DelegateCommand<string> ChangeMethodCommand { get; }
        public DelegateCommand CopyCommand { get; }

        public MainWindowViewModel(
            [Dependency("VerMultiThread")] ISynchronization synchronizationMultiThread,
            [Dependency("VerOneThread")] ISynchronization synchronizationOneThread)
        {
            _synchronizationMultiThread = synchronizationMultiThread;
            _synchronizationOneThread = synchronizationOneThread;
            _synchronization = _synchronizationOneThread;
            SelectSrcFolderCommand = new DelegateCommand(SelectSrcFolderAction);
            SelectDstFolderCommand = new DelegateCommand(SelectDstFolderAction);
            ClearCommand = new DelegateCommand(ClearExecuteAction);
            ChangeMethodCommand = new DelegateCommand<string>(ChangeMethodExecuteAction);
            CopyCommand = new DelegateCommand(CopyExecuteAction, CanCopyExecute);
        }

        private void SelectSrcFolderAction()
        {
            var dialog = new FolderBrowserDialog();
            var result = dialog.ShowDialog();
            if (result == DialogResult.OK) 
            {
                var dictianary = dialog.SelectedPath;
                Debug.WriteLine($"result sourceFolder: {dictianary}");
                SourceFolderPath = dictianary;
            }
        }

        private void SelectDstFolderAction()
        {
            var dialog = new FolderBrowserDialog();
            var result = dialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                var dictianary = dialog.SelectedPath;
                Debug.WriteLine($"result destinationFolder: {dictianary}");
                DestinationFolderPath = dictianary;
            }
        }

        private void ChangeMethodExecuteAction(string method)
        {
            Debug.WriteLine($"method: {method}");
            _synchronization = method switch
            {
                "MultiThread" => _synchronizationMultiThread,
                "OneThread" => _synchronizationOneThread,
                _ => _synchronization
            };
        }

        private bool CanCopyExecute()
        {
            return CanCopy;
        }

        public bool CanCopy
        {
            get => _canCopy;
            set => SetProperty(ref _canCopy, value);
        }

        private void ClearExecuteAction()
        {
            RemoveAllFilesFrom(DestinationFolderPath);
        }

        private void RemoveAllFilesFrom(string path)
        {
            var di = new DirectoryInfo(path);

            foreach (var file in di.GetFiles())
            {
                file.Delete();
            }
            foreach (var dir in di.GetDirectories())
            {
                dir.Delete(true);
            }
        }

        private void CopyExecuteAction()
        {
            var progressUpdater = new Action<double>(progress =>
            {
                Dispatcher.CurrentDispatcher.Invoke(() =>
                {
                    ProgressValue = progress;
                });
            });
            var task = CopyExecuteActionAsync(progressUpdater);
            task.ContinueWith(delegate { Debug.WriteLine("Copy finished"); }, TaskContinuationOptions.OnlyOnRanToCompletion);
            task.ContinueWith(delegate { Debug.WriteLine("error copy"); }, TaskContinuationOptions.OnlyOnFaulted);
        }

        private async Task CopyExecuteActionAsync(Action<double> progress)
        {
            // FileSystem.CopyDirectory(@"c:\temp\src", @"c:\temp\dst", UIOption.AllDialogs);
            ProgressValue = 0;
            CanCopy = false;

            if (_synchronization == _synchronizationOneThread)
            {
                Debug.WriteLine("synchronization OneThread");
            }

            if (_synchronization == _synchronizationMultiThread)
            {
                Debug.WriteLine("synchronization MultiThread");
            }

            var stopwatch = Stopwatch.StartNew();
            var filesToCopy = await _synchronization.CreateListOfFilesToCopy(SourceFolderPath, DestinationFolderPath);
            stopwatch.Stop();
            var elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            Debug.WriteLine($"Hash ElapsedMilliseconds: {elapsedMilliseconds}");
            LogMessage += Environment.NewLine + $"Hash ElapsedMilliseconds: {elapsedMilliseconds}";

            SumOfAllFileSize = _synchronization.GetSumOfAllFileSize(filesToCopy);
            Debug.WriteLine($"SumOfAllFileSize: {SumOfAllFileSize}");

            var stopwatchCopy = Stopwatch.StartNew();
            await _synchronization.Copy(filesToCopy, progress);
            stopwatchCopy.Stop();
            var copyElapsedMilliseconds = stopwatchCopy.ElapsedMilliseconds;
            Debug.WriteLine($"Copy ElapsedMilliseconds: {copyElapsedMilliseconds}");
            LogMessage += Environment.NewLine + $"Copy ElapsedMilliseconds: {copyElapsedMilliseconds}";

            CanCopy = true;

        }
    }
}
