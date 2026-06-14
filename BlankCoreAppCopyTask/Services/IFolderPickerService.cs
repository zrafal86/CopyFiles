using System.Threading.Tasks;

namespace BlankCoreAppCopyTask.Services;

public interface IFolderPickerService
{
    Task<string?> PickFolderAsync();
}
