# Migracja WPF → Avalonia UI

## Kontekst

Projekt był aplikacją desktopową .NET/WPF z Prism + Unity DI. Celem było uruchomienie na Linux (Ubuntu) bez zmiany logiki biznesowej.

---

## 1. Plik projektu (`.csproj`)

### Co zmienić

| Przed | Po |
|---|---|
| `net10.0-windows` | `net10.0` |
| `<UseWPF>true</UseWPF>` | usunąć |
| `<UseWindowsForms>true</UseWindowsForms>` | usunąć |
| `Prism.Unity` | `Avalonia`, `Avalonia.Desktop`, `Avalonia.Themes.Fluent` |

### Minimalne paczki Avalonia

```xml
<PackageReference Include="Avalonia" Version="11.2.3" />
<PackageReference Include="Avalonia.Desktop" Version="11.2.3" />
<PackageReference Include="Avalonia.Themes.Fluent" Version="11.2.3" />
<PackageReference Include="Avalonia.Fonts.Inter" Version="11.2.3" />
```

### Paczki MVVM i DI (zamiennik Prism/Unity)

```xml
<PackageReference Include="CommunityToolkit.Mvvm" Version="8.3.2" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="9.0.6" />
```

> **Uwaga:** suffix `-windows` w target framework to jedyna zmiana blokująca Linux. Bez niego projekt kompiluje się i działa cross-platform.

---

## 2. Entry point — nowy `Program.cs`

WPF nie wymaga jawnego `Main()` — generuje go automatycznie. Avalonia wymaga własnego `Program.cs`:

```csharp
internal sealed class Program
{
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()   // automatycznie X11 / Wayland / macOS / Windows
            .WithInterFont()
            .LogToTrace();
}
```

---

## 3. App i MainWindow — `.xaml` → `.axaml`

Avalonia używa rozszerzenia `.axaml` (konwencja, nie wymóg techniczny). Zmienia się namespace XML:

| WPF | Avalonia |
|---|---|
| `xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"` | `xmlns="https://github.com/avaloniaui"` |
| `xmlns:prism="http://prismlibrary.com/"` | usunąć |
| `prism:PrismApplication` | `Application` |
| `prism:ViewModelLocator.AutoWireViewModel="True"` | usunąć (ViewModel ustawiamy ręcznie przez DataContext) |

`App.axaml.cs` dziedziczy z `Avalonia.Application` zamiast `Prism.Unity.PrismApplication`. Zamiast `CreateShell()` i `RegisterTypes()` używamy `OnFrameworkInitializationCompleted()`.

---

## 4. MVVM — Prism → CommunityToolkit.Mvvm

### ViewModel base class

```csharp
// WPF + Prism
using Prism.Mvvm;
class MyViewModel : BindableBase { }

// Avalonia + CommunityToolkit
using CommunityToolkit.Mvvm.ComponentModel;
class MyViewModel : ObservableObject { }
```

`SetProperty(ref _field, value)` ma identyczną sygnaturę w obu bibliotekach — **żadna zmiana w ciele właściwości**.

### Komendy

| Prism | CommunityToolkit |
|---|---|
| `DelegateCommand` | `RelayCommand` |
| `DelegateCommand<T>` | `RelayCommand<T>` |
| brak | `AsyncRelayCommand` — dla komend `async Task` |

```csharp
// Prism
CopyCommand = new DelegateCommand(CopyAction);

// CommunityToolkit — komenda synchroniczna
CopyCommand = new RelayCommand(CopyAction);

// CommunityToolkit — komenda asynchroniczna (zalecane dla operacji I/O)
CopyCommand = new AsyncRelayCommand(CopyActionAsync);
```

> **Uwaga:** `AsyncRelayCommand` przyjmuje `Func<Task>`. Jeśli oryginalna komenda robiła fire-and-forget (`Task.Run` bez `await`), warto ją przepisać na prawdziwy `async Task` — kod staje się czystszy i błędy nie są gubione.

### DI — Unity → Microsoft.Extensions.DependencyInjection

Unity pozwalał na wstrzykiwanie nazwanych zależności przez atrybut `[Dependency("nazwa")]`. W MEDI od .NET 8 odpowiednikiem są **keyed services**:

```csharp
// Rejestracja
services.AddKeyedSingleton<ISynchronization, SynchronizationMultiThread>("VerMultiThread");
services.AddKeyedSingleton<ISynchronization, SynchronizationOneThread>("VerOneThread");

// Konstruktor ViewModelu
public MainWindowViewModel(
    [FromKeyedServices("VerMultiThread")] ISynchronization multiThread,
    [FromKeyedServices("VerOneThread")]   ISynchronization oneThread,
    IFolderPickerService folderPicker)
```

> **Uwaga:** `[FromKeyedServices]` działa tylko od .NET 8 i `Microsoft.Extensions.DependencyInjection` ≥ 8.0.

---

## 5. Dialogi systemowe — `FolderBrowserDialog` → `IStorageProvider`

`System.Windows.Forms.FolderBrowserDialog` to Windows-only. W Avalonia odpowiednikiem jest `IStorageProvider`, który jest dostępny przez `TopLevel` (czyli okno):

```csharp
var folders = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions());
string? path = folders.Count > 0 ? folders[0].Path.LocalPath : null;
```

### Problem: ViewModel nie powinien znać okna

Rozwiązanie — serwis z interfejsem:

```csharp
public interface IFolderPickerService
{
    Task<string?> PickFolderAsync();
}
```

`IStorageProvider` przekazujemy do serwisu po załadowaniu okna (event `Loaded`):

```csharp
// App.axaml.cs
var folderPickerService = new AvaloniaFolderPickerService();
var window = new MainWindow { DataContext = vm };
window.Loaded += (_, _) => folderPickerService.Configure(window.StorageProvider);
```

Dzięki temu ViewModel jest testowalny i nie zależy od UI.

---

## 6. Dispatcher (wątek UI)

### WPF

```csharp
using System.Windows.Threading;
Dispatcher.CurrentDispatcher.Invoke(() => { ProgressValue = progress; });
```

### Avalonia

```csharp
using Avalonia.Threading;
Dispatcher.UIThread.Post(() => ProgressValue = progress);
```

> **Ważne:** `Dispatcher.CurrentDispatcher` w WPF pobiera dispatcher bieżącego wątku. Jeśli wywołanie jest na wątku roboczym (np. w `Task.Run`), to był WPF-owy dispatcher tego wątku, co jest błędem. W Avalonia `Dispatcher.UIThread` zawsze zwraca dispatcher wątku UI — jest jednoznaczne i bezpieczne.

### Gdzie marshaling jest potrzebny

Marshaling należy do warstwy, która aktualizuje UI, nie do serwisów:

- **Serwisy** (`SynchronizationMultiThread` itp.) — wywołują `updater(progress)` bezpośrednio, bez Dispatchera.
- **ViewModel** — `progressUpdater` opakowuje wywołanie w `Dispatcher.UIThread.Post`.

---

## 7. XAML — różnice między WPF a Avalonia

### `BooleanToVisibilityConverter` — nie potrzebny

W WPF `Visibility` to enum (`Visible`/`Collapsed`). W Avalonia `IsVisible` to `bool`:

```xml
<!-- WPF -->
<StackPanel Visibility="{Binding CanCopy, Converter={StaticResource BooleanToVisibilityConverter}}">

<!-- Avalonia -->
<StackPanel IsVisible="{Binding CanCopy}">
```

### `Label` → `TextBlock`

`Label` w WPF to cienki wrapper nad `ContentControl`. W Avalonia też istnieje, ale `TextBlock` jest prostszy dla statycznych napisów.

### `RadioButton` z `Command`

W WPF `RadioButton` nie dziedziczył natywnie po `Button` jeśli chodzi o `Command`. W Avalonia `RadioButton` → `ToggleButton` → `Button` — komenda jest w pełni wspierana. Zamiast bindować `CommandParameter` do własnej `Content` przez `ElementName`, prościej podać wartość wprost:

```xml
<RadioButton Command="{Binding ChangeMethodCommand}" CommandParameter="OneThread">OneThread</RadioButton>
```

---

## 8. Co zostaje bez zmian

- Logika biznesowa (serwisy kopiowania, hashowania, progress) — **zero zmian**
- `INotifyPropertyChanged` i wzorzec MVVM — identyczny
- `async`/`await`, `Task`, `CancellationToken` — identyczne
- `System.IO`, `System.Collections.Immutable` — identyczne

---

## Podsumowanie — lista kontrolna

- [ ] Zmienić target framework: `net*-windows` → `net*`
- [ ] Usunąć `<UseWPF>` i `<UseWindowsForms>`
- [ ] Dodać paczki Avalonia + CommunityToolkit.Mvvm + MEDI
- [ ] Dodać `Program.cs` z `AppBuilder`
- [ ] Przepisać `App.xaml[.cs]` → `App.axaml[.cs]`
- [ ] Przepisać `MainWindow.xaml[.cs]` → `MainWindow.axaml[.cs]`
- [ ] Zmienić namespace XML w każdym pliku XAML
- [ ] `BindableBase` → `ObservableObject`
- [ ] `DelegateCommand` → `RelayCommand` / `AsyncRelayCommand`
- [ ] `[Dependency("x")]` (Unity) → `[FromKeyedServices("x")]` (MEDI)
- [ ] `FolderBrowserDialog` → `IStorageProvider` przez serwis z interfejsem
- [ ] `Dispatcher.CurrentDispatcher.Invoke` → `Dispatcher.UIThread.Post`
- [ ] Usunąć `using System.Windows.*` z serwisów
- [ ] `IsVisible="{Binding Bool}"` zamiast konwertera Visibility
