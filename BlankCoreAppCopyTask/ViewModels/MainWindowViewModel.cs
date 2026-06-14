using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using BlankCoreAppCopyTask.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace BlankCoreAppCopyTask.ViewModels;

public class MainWindowViewModel : ObservableObject
{
    private readonly ISynchronization _synchronizationMultiThread;
    private readonly ISynchronization _synchronizationOneThread;
    private readonly IFolderPickerService _folderPickerService;
    private bool _canCopy = true;
    private long _copyMultiThreadTime;
    private long _copyOneThreadTime;
    private string? _destinationFolderPath;
    private long _hashMultiThreadTime;
    private long _hashOneThreadTime;
    private double _progressValue;
    private string? _resultText;
    private string? _sourceFolderPath;
    private long _sumOfAllFileSize;
    private ISynchronization _synchronization;
    private string _title = "Comparison of copy method";
    private CancellationTokenSource? _syncCancellationTokenSource;

    public MainWindowViewModel(
        [FromKeyedServices("VerMultiThread")] ISynchronization synchronizationMultiThread,
        [FromKeyedServices("VerOneThread")] ISynchronization synchronizationOneThread,
        IFolderPickerService folderPickerService)
    {
        _synchronizationMultiThread = synchronizationMultiThread;
        _synchronizationOneThread = synchronizationOneThread;
        _folderPickerService = folderPickerService;
        _synchronization = _synchronizationOneThread;
        SelectSrcFolderCommand = new AsyncRelayCommand(SelectSrcFolderActionAsync);
        SelectDstFolderCommand = new AsyncRelayCommand(SelectDstFolderActionAsync);
        ClearCommand = new RelayCommand(ClearExecuteAction);
        ChangeMethodCommand = new RelayCommand<string>(ChangeMethodExecuteAction);
        CopyCommand = new AsyncRelayCommand(CopyExecuteActionAsync);
    }

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public string? DestinationFolderPath
    {
        get => _destinationFolderPath;
        set => SetProperty(ref _destinationFolderPath, value);
    }

    public string? SourceFolderPath
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

    public string? ResultText
    {
        get => _resultText;
        set => SetProperty(ref _resultText, value);
    }

    public bool CanCopy
    {
        get => _canCopy;
        set => SetProperty(ref _canCopy, value);
    }

    public IAsyncRelayCommand SelectSrcFolderCommand { get; }
    public IAsyncRelayCommand SelectDstFolderCommand { get; }
    public IRelayCommand ClearCommand { get; }
    public IRelayCommand<string> ChangeMethodCommand { get; }
    public IAsyncRelayCommand CopyCommand { get; }

    private async Task SelectSrcFolderActionAsync()
    {
        var path = await _folderPickerService.PickFolderAsync();
        if (path != null)
        {
            Debug.WriteLine($"result sourceFolder: {path}");
            SourceFolderPath = path;
        }
    }

    private async Task SelectDstFolderActionAsync()
    {
        var path = await _folderPickerService.PickFolderAsync();
        if (path != null)
        {
            Debug.WriteLine($"result destinationFolder: {path}");
            DestinationFolderPath = path;
        }
    }

    private void ChangeMethodExecuteAction(string? method)
    {
        Debug.WriteLine($"method: {method}");
        _synchronization = method switch
        {
            "MultiThread" => _synchronizationMultiThread,
            "OneThread" => _synchronizationOneThread,
            _ => _synchronization
        };
    }

    private void ClearExecuteAction()
    {
        if (!string.IsNullOrEmpty(DestinationFolderPath))
            RemoveAllFilesFrom(DestinationFolderPath);
    }

    private static void RemoveAllFilesFrom(string path)
    {
        var di = new System.IO.DirectoryInfo(path);
        foreach (var file in di.GetFiles()) file.Delete();
        foreach (var dir in di.GetDirectories()) dir.Delete(true);
    }

    private async Task CopyExecuteActionAsync()
    {
        if (SourceFolderPath is null || DestinationFolderPath is null) return;

        ProgressValue = 0;
        CanCopy = false;

        var progressUpdater = new Action<double>(progress =>
        {
            Dispatcher.UIThread.Post(() => ProgressValue = progress);
        });

        try
        {
            var stopwatch = Stopwatch.StartNew();
            var filesToCopy = await _synchronization.CreateListOfFilesToCopy(SourceFolderPath, DestinationFolderPath);
            stopwatch.Stop();
            var hashElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            Debug.WriteLine($"Hash ElapsedMilliseconds: {hashElapsedMilliseconds}");

            SumOfAllFileSize = _synchronization.GetSumOfAllFileSize(filesToCopy);
            Debug.WriteLine($"SumOfAllFileSize: {SumOfAllFileSize}");

            if (_syncCancellationTokenSource != null)
            {
                _syncCancellationTokenSource.Cancel();
                _syncCancellationTokenSource.Dispose();
            }

            _syncCancellationTokenSource = new CancellationTokenSource();
            var token = _syncCancellationTokenSource.Token;

            var stopwatchCopy = Stopwatch.StartNew();
            await _synchronization.Copy(filesToCopy, progressUpdater, token);
            stopwatchCopy.Stop();
            var copyElapsedMilliseconds = stopwatchCopy.ElapsedMilliseconds;
            Debug.WriteLine($"Copy ElapsedMilliseconds: {copyElapsedMilliseconds}");

            if (_synchronization == _synchronizationOneThread)
            {
                HashOneThreadTime = hashElapsedMilliseconds;
                CopyOneThreadTime = copyElapsedMilliseconds;
            }

            if (_synchronization == _synchronizationMultiThread)
            {
                HashMultiThreadTime = hashElapsedMilliseconds;
                CopyMultiThreadTime = copyElapsedMilliseconds;
            }

            if (HashOneThreadTime > 0 && CopyOneThreadTime > 0 && HashMultiThreadTime > 0 && CopyMultiThreadTime > 0)
            {
                var increaseHash = (double)(HashOneThreadTime - HashMultiThreadTime) / HashOneThreadTime * 100.0;
                var increaseCopy = (double)(CopyOneThreadTime - CopyMultiThreadTime) / CopyOneThreadTime * 100.0;
                ResultText =
                    $"Multi thread hash calculation is faster/slower {increaseHash}% then one thread method\n" +
                    $"Multi thread copy is faster/slower {increaseCopy}% then one thread method";
            }
        }
        catch (Exception e)
        {
            Debug.WriteLine($"error copy: {e}");
        }
        finally
        {
            CanCopy = true;
        }
    }
}
