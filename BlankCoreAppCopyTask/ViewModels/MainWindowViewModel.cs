using BlankCoreAppCopyTask.Services;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace BlankCoreAppCopyTask.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private readonly ISynchronizationPlaylist _synchronizationPlaylist;
        private string _title = "Prism Application";
        private long _sumOfAllFileSize;
        private double _progressValue;
        private bool _canCopy = true;

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

        public DelegateCommand CopyCommand { get; }

        public MainWindowViewModel(ISynchronizationPlaylist synchronizationPlaylist)
        {
            _synchronizationPlaylist = synchronizationPlaylist;
            CopyCommand = new DelegateCommand(CopyExecuteAction, CanCopyExecute);
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

            CanCopy = false;

            var filesToCopy = await _synchronizationPlaylist.CreateListOfFilesToCopy(@"c:\temp\src", @"c:\temp\dst");
            SumOfAllFileSize = _synchronizationPlaylist.GetSumOfAllFileSize(filesToCopy);
            Debug.WriteLine($"SumOfAllFileSize: {SumOfAllFileSize}");
            await _synchronizationPlaylist.Copy(filesToCopy, progress);

            CanCopy = true;
        }
    }
}
