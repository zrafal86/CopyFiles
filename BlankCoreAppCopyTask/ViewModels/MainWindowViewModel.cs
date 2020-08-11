using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Threading;
using BlankCoreAppCopyTask.Services;
using Prism.Commands;
using Prism.Mvvm;
using Unity;

namespace BlankCoreAppCopyTask.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private readonly ISynchronization _synchronizationMultiThread;
        private readonly ISynchronization _synchronizationOneThread;
        private bool _canCopy = true;
        private long _copyMultiThreadTime;
        private long _copyOneThreadTime;
        private string _destinationFolderPath;
        private long _hashMultiThreadTime;
        private long _hashOneThreadTime;
        private double _progressValue;
        private string _resultText;
        private string _sourceFolderPath;
        private long _sumOfAllFileSize;
        private ISynchronization _synchronization;
        private string _title = "Comparison of copy method";

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

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public string DestinationFolderPath
        {
            get => _destinationFolderPath;
            set => SetProperty(ref _destinationFolderPath, value);
        }

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

        public long HashMultiThreadTime
        {
            get => _hashMultiThreadTime;
            set => SetProperty(ref _hashMultiThreadTime, value);
        }

        public long CopyMultiThreadTime
        {
            get => _copyMultiThreadTime;
            set => SetProperty(ref _copyMultiThreadTime, value);
        }

        public long HashOneThreadTime
        {
            get => _hashOneThreadTime;
            set => SetProperty(ref _hashOneThreadTime, value);
        }

        public long CopyOneThreadTime
        {
            get => _copyOneThreadTime;
            set => SetProperty(ref _copyOneThreadTime, value);
        }

        public string ResultText
        {
            get => _resultText;
            set => SetProperty(ref _resultText, value);
        }

        public DelegateCommand SelectSrcFolderCommand { get; }
        public DelegateCommand SelectDstFolderCommand { get; }
        public DelegateCommand ClearCommand { get; }
        public DelegateCommand<string> ChangeMethodCommand { get; }
        public DelegateCommand CopyCommand { get; }

        public bool CanCopy
        {
            get => _canCopy;
            set => SetProperty(ref _canCopy, value);
        }

        private void SelectSrcFolderAction()
        {
            var dictionary = SelectedFolderPath();
            Debug.WriteLine($"result sourceFolder: {dictionary}");
            SourceFolderPath = dictionary;
        }

        private void SelectDstFolderAction()
        {
            var dictionary = SelectedFolderPath();
            Debug.WriteLine($"result destinationFolder: {dictionary}");
            DestinationFolderPath = dictionary;
        }

        private string SelectedFolderPath()
        {
            var dialog = new FolderBrowserDialog();
            var result = dialog.ShowDialog();
            return result == DialogResult.OK ? dialog.SelectedPath : string.Empty;
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

        private void ClearExecuteAction()
        {
            if (!string.IsNullOrEmpty(DestinationFolderPath)) RemoveAllFilesFrom(DestinationFolderPath);
        }

        private void RemoveAllFilesFrom(string path)
        {
            var di = new DirectoryInfo(path);

            foreach (var file in di.GetFiles()) file.Delete();
            foreach (var dir in di.GetDirectories()) dir.Delete(true);
        }

        private void CopyExecuteAction()
        {
            var progressUpdater = new Action<double>(progress =>
            {
                Dispatcher.CurrentDispatcher.Invoke(() => { ProgressValue = progress; });
            });
            var task = CopyExecuteActionAsync(progressUpdater);
            task.ContinueWith(delegate { Debug.WriteLine("Copy finished"); },
                TaskContinuationOptions.OnlyOnRanToCompletion);
            task.ContinueWith(delegate
            {
                Debug.WriteLine("error copy");
                CanCopy = true;
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        private async Task CopyExecuteActionAsync(Action<double> progress)
        {
            // FileSystem.CopyDirectory(@"c:\temp\src", @"c:\temp\dst", UIOption.AllDialogs);
            ProgressValue = 0;
            CanCopy = false;

            var stopwatch = Stopwatch.StartNew();
            var filesToCopy = await _synchronization.CreateListOfFilesToCopy(SourceFolderPath, DestinationFolderPath);
            stopwatch.Stop();
            var hashElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            Debug.WriteLine($"Hash ElapsedMilliseconds: {hashElapsedMilliseconds}");

            SumOfAllFileSize = _synchronization.GetSumOfAllFileSize(filesToCopy);
            Debug.WriteLine($"SumOfAllFileSize: {SumOfAllFileSize}");

            var stopwatchCopy = Stopwatch.StartNew();
            await _synchronization.Copy(filesToCopy, progress);
            stopwatchCopy.Stop();
            var copyElapsedMilliseconds = stopwatchCopy.ElapsedMilliseconds;
            Debug.WriteLine($"Copy ElapsedMilliseconds: {copyElapsedMilliseconds}");

            if (_synchronization == _synchronizationOneThread)
            {
                Debug.WriteLine("synchronization OneThread");
                HashOneThreadTime = hashElapsedMilliseconds;
                CopyOneThreadTime = copyElapsedMilliseconds;
            }

            if (_synchronization == _synchronizationMultiThread)
            {
                Debug.WriteLine("synchronization MultiThread");
                HashMultiThreadTime = hashElapsedMilliseconds;
                CopyMultiThreadTime = copyElapsedMilliseconds;
            }

            if (HashOneThreadTime > 0 &&
                CopyOneThreadTime > 0 &&
                HashMultiThreadTime > 0 &&
                CopyMultiThreadTime > 0)
            {
                var increaseHashCalculation =
                    (double) (HashOneThreadTime - HashMultiThreadTime) / HashOneThreadTime * 100.0;
                var increaseCopy = (double) (CopyOneThreadTime - CopyMultiThreadTime) / CopyOneThreadTime * 100.0;
                ResultText =
                    $"Multi thread hash calculation is faster/slower {increaseHashCalculation}% then one thread method\n" +
                    $"Multi thread copy is faster/slower {increaseCopy}% then one thread method";
            }

            CanCopy = true;
        }
    }
}