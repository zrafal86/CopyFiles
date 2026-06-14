using System.Threading.Tasks;
using Avalonia.Platform.Storage;

namespace BlankCoreAppCopyTask.Services;

public class AvaloniaFolderPickerService : IFolderPickerService
{
    private IStorageProvider? _storageProvider;

    public void Configure(IStorageProvider storageProvider)
    {
        _storageProvider = storageProvider;
    }

    public async Task<string?> PickFolderAsync()
    {
        if (_storageProvider is null) return null;
        var folders = await _storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { AllowMultiple = false });
        return folders.Count > 0 ? folders[0].Path.LocalPath : null;
    }
}
