using BlankCoreAppCopyTask.Services;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Threading;
using Unity;

namespace BlankCoreAppCopyTask.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private readonly ISynchronizationPlaylist _synchronizationPlaylistFast;
        private readonly ISynchronizationPlaylist _synchronizationPlaylistSlow;
        private string _title = "Comparison of copy method";
        private long _sumOfAllFileSize;
        private double _progressValue;
        private bool _canCopy = true;
        private ISynchronizationPlaylist _synchronizationPlaylist;
        private string _logMessage;
        private string sourceFolder = @"c:\temp\src";
        private string destinationFolder = @"c:\temp\dst";

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
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
            [Dependency("VerFast")] ISynchronizationPlaylist synchronizationPlaylistFast,
            [Dependency("VerSlow")] ISynchronizationPlaylist synchronizationPlaylistSlow)
        {
            _synchronizationPlaylistFast = synchronizationPlaylistFast;
            _synchronizationPlaylistSlow = synchronizationPlaylistSlow;
            _synchronizationPlaylist = _synchronizationPlaylistSlow;
            SelectSrcFolderCommand = new DelegateCommand(SelectSrcFolderAction);
            SelectDstFolderCommand = new DelegateCommand(SelectDstFolderAction);
            ClearCommand = new DelegateCommand(ClearExecuteAction);
            ChangeMethodCommand = new DelegateCommand<string>(ChangeMethodExecuteAction);
            CopyCommand = new DelegateCommand(CopyExecuteAction, CanCopyExecute);
        }

        private void SelectSrcFolderAction()
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();

        }

        private void SelectDstFolderAction()
        {
            throw new NotImplementedException();
        }

        private void ChangeMethodExecuteAction(string method)
        {
            Debug.WriteLine($"method: {method}");
            _synchronizationPlaylist = method switch
            {
                "Fast" => _synchronizationPlaylistFast,
                "Slow" => _synchronizationPlaylistSlow,
                _ => _synchronizationPlaylist
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
            RemoveAllFilesFrom(destinationFolder);
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

            if (_synchronizationPlaylist == _synchronizationPlaylistSlow)
            {
                Debug.WriteLine("synchronization Slow");
            }

            if (_synchronizationPlaylist == _synchronizationPlaylistFast)
            {
                Debug.WriteLine("synchronization Fast");
            }

            var stopwatch = Stopwatch.StartNew();
            var filesToCopy = await _synchronizationPlaylist.CreateListOfFilesToCopy(sourceFolder, destinationFolder);
            stopwatch.Stop();
            var elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            Debug.WriteLine($"Hash ElapsedMilliseconds: {elapsedMilliseconds}");
            LogMessage += Environment.NewLine + $"Hash ElapsedMilliseconds: {elapsedMilliseconds}";

            SumOfAllFileSize = _synchronizationPlaylist.GetSumOfAllFileSize(filesToCopy);
            Debug.WriteLine($"SumOfAllFileSize: {SumOfAllFileSize}");

            var stopwatchCopy = Stopwatch.StartNew();
            await _synchronizationPlaylist.Copy(filesToCopy, progress);
            stopwatchCopy.Stop();
            var copyElapsedMilliseconds = stopwatchCopy.ElapsedMilliseconds;
            Debug.WriteLine($"Copy ElapsedMilliseconds: {copyElapsedMilliseconds}");
            LogMessage += Environment.NewLine + $"Copy ElapsedMilliseconds: {copyElapsedMilliseconds}";

            CanCopy = true;

        }
    }
}
