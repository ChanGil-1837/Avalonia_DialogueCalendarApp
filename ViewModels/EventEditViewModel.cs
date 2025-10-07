
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DialogueCalendarApp.ViewModels;

public class EventEditViewModel : ObservableObject
{
    private int _id = 0;
    private string _originalKrDialoguePath = "";
    private string _originalEnDialoguePath = "";
    public int Id { get => _id; set => SetProperty(ref _id, value); }

    // 날짜와 시간을 하나의 DateTime 프로퍼티로 통합합니다.
    private string _month = "";
    public string Month
    {
        get => _month;
        set
        {
            // Convert month number to "MMM" format if it's a number
            string formattedMonth = value;
            if (int.TryParse(value, out int monthNumber))
            {
                if (monthNumber >= 1 && monthNumber <= 12)
                {
                    formattedMonth = new System.DateTime(System.DateTime.Now.Year, monthNumber, 1).ToString("MMM", System.Globalization.CultureInfo.InvariantCulture);
                }
            }

            if (SetProperty(ref _month, formattedMonth))
            {
                UpdateDialoguePaths(FileName); // Reconstruct paths when Month changes
            }
        }
    }
    private string _day = "";
    public string Day
    {
        get => _day;
        set
        {
            if (SetProperty(ref _day, value))
            {
                UpdateDialoguePaths(FileName); // Reconstruct paths when Day changes
                UpdateDayOfWeek();
            }
        }
    }
    private void UpdateDayOfWeek()
    {
        if (!int.TryParse(Day, out int dayNumber) || dayNumber <= 0)
            return;

        // 달력 시작 요일 기준으로 요일 계산
        int startIndex = (int)_selectedStartDay;  // 0=Sunday
        int weekdayIndex = (startIndex + (dayNumber - 1)) % 7;

        DayOfWeek = (DayOfWeek)weekdayIndex;
        Date = DayOfWeek.ToString(); // "Monday", "Saturday" 등
    }
    private string _date = "";
    public string Date
    {
        get => _date;
        set => SetProperty(ref _date, value);
    }

    private DayOfWeek _dayOfWeek;
    public DayOfWeek DayOfWeek { get => _dayOfWeek; set => SetProperty(ref _dayOfWeek, value); }

    public System.Array TimeSlots => System.Enum.GetValues(typeof(Models.TimeSlot));

    private Models.TimeSlot _selectedTimeSlot;
    public Models.TimeSlot SelectedTimeSlot
    {
        get => _selectedTimeSlot;
        set
        {
            if (SetProperty(ref _selectedTimeSlot, value))
            {
                Time = value.ToString();
            }
        }
    }

    private string _time = "";
    public string Time
    {
        get => _time;
        set
        {
            if (SetProperty(ref _time, value))
            {
                if (System.Enum.TryParse<Models.TimeSlot>(value, true, out var timeSlot))
                {
                    SelectedTimeSlot = timeSlot;
                }
                else
                {
                    SelectedTimeSlot = Models.TimeSlot.none;
                }
            }
        }
    }


    private string _location = "";
    public string Location { get => _location; set => SetProperty(ref _location, value); }

    private string _fileName = "";
    public string FileName
    {
        get => _fileName;
        set
        {
            if (SetProperty(ref _fileName, value))
            {
                UpdateDialoguePaths(value);
            }
        }
    }

    private void UpdateDialoguePaths(string newFileName)
    {
        // Construct new KR path
        var newKrDir = Path.Combine(AppSettings.DIRPATH, "KR", Month, Day);
        KRDialogue = Path.Combine(newKrDir, newFileName);

        // Construct new EN path
        var newEnDir = Path.Combine(AppSettings.DIRPATH, "EN", Month, Day);
        ENDialogue = Path.Combine(newEnDir, newFileName);
    }

    private string _krDialogue = "";
    public string KRDialogue
    {
        get => _krDialogue;
        set
        {
            var newPath = value;
            if (!string.IsNullOrEmpty(newPath) && !newPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                newPath = Path.ChangeExtension(newPath, ".json");
            }
            if (SetProperty(ref _krDialogue, newPath))
            {
                // When KRDialogue changes, update FileName
                _fileName = Path.GetFileNameWithoutExtension(newPath);
                OnPropertyChanged(nameof(FileName));
            }
        }
    }

    private string _enDialogue = "";
    public string ENDialogue
    {
        get => _enDialogue;
        set
        {
            var newPath = value;
            if (!string.IsNullOrEmpty(newPath) && !newPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                newPath = Path.ChangeExtension(newPath, ".json");
            }
            if (SetProperty(ref _enDialogue, newPath))
            {
                // When ENDialogue changes, update FileName
                _fileName = Path.GetFileNameWithoutExtension(newPath);
                OnPropertyChanged(nameof(FileName));
            }
        }
    }
    private string _condition = "";
    public string Condition { get => _condition; set => SetProperty(ref _condition, value); }


    private string _desc = "";
    public string Desc { get => _desc; set => SetProperty(ref _desc, value); }


    public ICommand OpenKRCommand { get; }
    public ICommand OpenENCommand { get; }

    // Command properties
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand CopyCommand { get; }
    public ICommand DeleteCommand { get; }

    // TaskCompletionSource를 사용해 모달 결과 전달
    public TaskCompletionSource<EditResultType> Completion { get; } = new();
    private readonly DayOfWeek _selectedStartDay;
    public EventEditViewModel(DayOfWeek selectedStartDay, int id = -1)
    {
        _selectedStartDay = selectedStartDay;
        SaveCommand = new RelayCommand(OnSave);
        CancelCommand = new RelayCommand(OnCancel);
        OpenKRCommand = new RelayCommand(OpenKR);
        OpenENCommand = new RelayCommand(OpenEN);
        CopyCommand = new RelayCommand(CopyDialogue);
        DeleteCommand = new RelayCommand(OnDelete);
    }

    private async void OnDelete()
    {
        if (!Completion.Task.IsCompleted)
            Completion.SetResult(EditResultType.Deleted);
        if (App.Current.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            foreach (var window in desktop.Windows)
            {
                if (window.DataContext == this) // 자기 자신 창 찾기
                {
                    window.Close();
                    break;
                }
            }
        }
    }
    private async void CopyDialogue()
    {
        if (string.IsNullOrWhiteSpace(KRDialogue) || !File.Exists(KRDialogue))
            return;

        bool proceed = await EnsureFileExists(ENDialogue);
        if (!proceed) return;

        File.Copy(KRDialogue, ENDialogue, overwrite: true);

        var doneDialog = new Views.ConfirmDialog("복사가 완료되었습니다.");
        await doneDialog.ShowDialog(
            App.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop 
            ? desktop.MainWindow : null);
    }



    private void OpenKR()
    {
        OpenDialogue(KRDialogue);
    }

    private void OpenEN()
    {
        OpenDialogue(ENDialogue);
    }
    private async void OpenDialogue(string path)
    {
        bool fileExists = File.Exists(path);

        // 파일이 없으면 디렉터리 생성 + 빈 파일 생성
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        if (!fileExists)
            File.WriteAllText(path, "{}"); // 기본 JSON 내용

        // ProcessStartInfo 설정
        var psi = new ProcessStartInfo
        {
            FileName = AppSettings.DIALOGUEAPPLOC,
            Arguments = fileExists 
                ? $"\"{path}\""       // 기존 파일이면 파일 경로만
                : $"\"{path}\" new",   // 새로 만든 파일이면 "뉴" 인자 추가
            UseShellExecute = true
        };

        Process.Start(psi);
    }


    private async void OnSave()
    {
        // The new paths are simply the current values of KRDialogue and ENDialogue properties
        var newKrPath = KRDialogue;
        var newEnPath = ENDialogue;
        async Task<bool> ConfirmOverwrite(string path)
        {
            if (File.Exists(path))
            {
                var dialog = new Views.ConfirmDialog($"파일이 이미 존재합니다:\n{path}\n덮어쓰시겠습니까?");
                return await dialog.ShowDialog<bool>(
                    App.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                        ? desktop.MainWindow : null);
            }
            return true;
        }
        // Ensure directories exist for the new paths
        if (!string.IsNullOrEmpty(newKrPath))
        {
            var newKrDir = Path.GetDirectoryName(newKrPath);
            if (!string.IsNullOrEmpty(newKrDir))
            {
                Directory.CreateDirectory(newKrDir);
            }
        }
        if (!string.IsNullOrEmpty(newEnPath))
        {
            var newEnDir = Path.GetDirectoryName(newEnPath);
            if (!string.IsNullOrEmpty(newEnDir))
            {
                Directory.CreateDirectory(newEnDir);
            }
        }

        // Handle KR Dialogue file
        if (!string.IsNullOrEmpty(_originalKrDialoguePath) && _originalKrDialoguePath != newKrPath)
        {
            if (File.Exists(_originalKrDialoguePath))
            {
                // Move content from original to new path
                File.Move(_originalKrDialoguePath, newKrPath, overwrite: true);
            }
            else
            {
                // Original didn't exist, but new path is different, create empty file at new path
                // File.WriteAllText(newKrPath, "{}");
            }
        }
        else if (string.IsNullOrEmpty(_originalKrDialoguePath) && !string.IsNullOrEmpty(newKrPath))
        {
            // New event, create file at new path
            // File.WriteAllText(newKrPath, "{}");
        }

        // Handle EN Dialogue file (similar logic)
        if (!string.IsNullOrEmpty(_originalEnDialoguePath) && _originalEnDialoguePath != newEnPath)
        {
            if (File.Exists(_originalEnDialoguePath))
            {
                File.Move(_originalEnDialoguePath, newEnPath, overwrite: true);
            }
            else
            {
                // File.WriteAllText(newEnPath, "{}");
            }
        }
        else if (string.IsNullOrEmpty(_originalEnDialoguePath) && !string.IsNullOrEmpty(newEnPath))
        {
            // File.WriteAllText(newEnPath, "{}");
        }

        // Reset original paths for subsequent saves
        _originalKrDialoguePath = newKrPath;
        _originalEnDialoguePath = newEnPath;

        if (!Completion.Task.IsCompleted)
            Completion.SetResult(EditResultType.Saved);
        if (App.Current.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            foreach (var window in desktop.Windows)
            {
                if (window.DataContext == this) // 자기 자신 창 찾기
                {
                    window.Close();
                    break;
                }
            }
        }
    }

    private void OnCancel()
    {
        if (!Completion.Task.IsCompleted)
            Completion.SetResult(EditResultType.Cancelled);

        if (App.Current.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            foreach (var window in desktop.Windows)
            {
                if (window.DataContext == this) // 자기 자신 창 찾기
                {
                    window.Close();
                    break;
                }
            }
        }
    }

   
   private async Task<bool> EnsureFileExists(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        if (!File.Exists(path))
        {
            // 파일이 없으면 빈 파일 생성
            File.WriteAllText(path, "{}"); // 기본 JSON 내용
            return true;
        }

        // 이미 파일이 존재하면 덮어쓰기 여부 확인
        var dialog = new Views.ConfirmDialog($"이미 파일이 존재합니다:\n{path}\n덮어쓰시겠습니까?");
        var result = await dialog.ShowDialog<bool>(
            App.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop 
            ? desktop.MainWindow : null);

        return dialog.Result; // true면 덮어쓰기 진행
    }

   public void LoadEventData(string month, string day, string fileName, string krDialoguePath, string enDialoguePath, string location, string condition, string desc, Models.TimeSlot selectedTimeSlot, DayOfWeek dayOfWeek)
    {
        Month = month;
        Day = day;
        FileName = fileName;
        Location = location;
        Condition = condition;
        Desc = desc;
        SelectedTimeSlot = selectedTimeSlot;
        DayOfWeek = dayOfWeek; // Set the new DayOfWeek property

        // Capture original paths, ensuring .json extension
        _originalKrDialoguePath = Path.ChangeExtension(krDialoguePath, ".json");
        _originalEnDialoguePath = Path.ChangeExtension(enDialoguePath, ".json");

        // Ensure KRDialogue and ENDialogue are set to the provided paths
        // This will also update FileName via their setters if the paths are different
        KRDialogue = krDialoguePath;
        ENDialogue = enDialoguePath;
    }

}

