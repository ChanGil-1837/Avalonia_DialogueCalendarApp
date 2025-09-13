namespace DialogueCalendarApp.ViewModels;
using System.Windows.Input;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Animation;
using System.Linq;
using CommunityToolkit.Mvvm.Input;

public class MainWindowViewModel : INotifyPropertyChanged
{

    string eventListPath = "";
    public ObservableCollection<FolderItem> FolderList { get; set; } = new();

    private PageViewModelBase? _CurrentPage;
    public PageViewModelBase? CurrentPage
    {
        get => _CurrentPage;
        private set
        {
            if (_CurrentPage != value)
            {
                _CurrentPage = value;
                OnPropertyChanged();
            }
        }
    }


    private GridLength _slideWidth = new GridLength(0);
    public GridLength SlideWidth
    {
        get => _slideWidth;
        set
        {
            if (_slideWidth != value)
            {
                _slideWidth = value;
                OnPropertyChanged();
            }
        }
    }


    public ICommand TogglePanelCommand { get; }
    public ICommand OpenFolderCommand { get; }

    public MainWindowViewModel()
    {
        TogglePanelCommand = new RelayCommand(async _ =>
        {
            Console.WriteLine("버튼 눌림!");

            if (await LoadFolders())
            {
                SlideWidth = SlideWidth.Value == 0 ? new GridLength(200) : new GridLength(0);
            }
        });

        OpenFolderCommand = new RelayCommand<FolderItem>(folder =>
        {
            if (string.IsNullOrWhiteSpace(folder.Name))
                return;

            string upperName = folder.Name.ToUpper();
            string[] months = { "JAN", "FEB", "MAR", "APR", "MAY", "JUN", "JUL", "AUG", "SEP", "OCT", "NOV", "DEC" };

            if (months.Contains(upperName))
            {
                Console.WriteLine($"{upperName} 월 폴더를 선택했습니다");
                CurrentPage = pages[1];
                if (CurrentPage is CalendarPageViewModel monthPageViewModel)
                {
                    // 3. 변환에 성공했다면 메서드를 호출하여 값을 넘겨줍니다.
                    monthPageViewModel.SetMonth(upperName,eventListPath);
                }
            }
            else
            {
                CurrentPage = pages[2];
            }
            
        });

        _CurrentPage = pages[0];
    }

    private readonly PageViewModelBase[] pages =
    {
        new FirstPageViewModel(),
        new CalendarPageViewModel(),
        new ListViewPageViewModel()
    };

    private async Task<bool> LoadFolders()
    {
        // FolderDialog 띄우기
        var dlg = new OpenFolderDialog
        {
            Title = "폴더를 선택하세요"
        };

        string? selectedPath = await dlg.ShowAsync(App.Current.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop ? desktop.MainWindow : null);

        if (string.IsNullOrWhiteSpace(selectedPath))
            return false; // 선택 안했으면 종료

        // 폴더 내용 확인
        FolderList.Clear();

        // eventlist.csv 있는지 체크
        eventListPath = Path.Combine(selectedPath, "eventlist.csv");
        if (!File.Exists(eventListPath))
        {
            Console.WriteLine("선택한 폴더에 eventlist.csv가 없습니다.");
            return false;
        }
        AppSettings.CSVPATH = eventListPath;
        // KR, EN 폴더 있는지 체크
        string krPath = Path.Combine(selectedPath, "KR");
        string enPath = Path.Combine(selectedPath, "EN");

        if (!Directory.Exists(krPath) || !Directory.Exists(enPath))
        {
            Console.WriteLine("KR 또는 EN 폴더가 없습니다.");
            return false;
        }
        AppSettings.DIRPATH = selectedPath;
        foreach (var dir in Directory.GetDirectories(krPath))
        {
            FolderList.Add(new FolderItem(dir)); // FolderItem 생성
        }


        Console.WriteLine("폴더 로드 완료!");
        return true;
    }


    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = p => execute();
        if (canExecute != null)
        {
            _canExecute = p => canExecute();
        }
    }

    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

    public void Execute(object? parameter) => _execute(parameter);

    public event EventHandler? CanExecuteChanged;

    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}


