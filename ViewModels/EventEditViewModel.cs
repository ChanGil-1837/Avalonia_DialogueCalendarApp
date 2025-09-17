
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DialogueCalendarApp.ViewModels;

public class EventEditViewModel : ObservableObject
{
    private int _id = 0;
    public int Id { get => _id; set => SetProperty(ref _id, value); }

    // 날짜와 시간을 하나의 DateTime 프로퍼티로 통합합니다.
    private string _month = "";
    public string Month
    {
        get => _month;
        set => SetProperty(ref _month, value);
    }
    private string _day = "";
    public string Day
    {
        get => _day;
        set => SetProperty(ref _day, value);
    }
    private string _date = "";
    public string Date
    {
        get => _date;
        set => SetProperty(ref _date, value);
    }

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

    private string _krDialogue = "";
    public string KRDialogue
    {
        get => Path.ChangeExtension(_krDialogue, ".json");
        set => SetProperty(ref _krDialogue, value);
    }

    private string _enDialogue = "";
    public string ENDialogue
    {
        get => Path.ChangeExtension(_enDialogue, ".json");
        set => SetProperty(ref _enDialogue, value);
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

    // TaskCompletionSource를 사용해 모달 결과 전달
    public TaskCompletionSource<bool> Completion { get; } = new();

    public EventEditViewModel(int id = -1)
    {
        SaveCommand = new RelayCommand(OnSave);
        CancelCommand = new RelayCommand(OnCancel);
        OpenKRCommand = new RelayCommand(OpenKR);
        OpenENCommand = new RelayCommand(OpenEN);
        CopyCommand = new RelayCommand(CopyDialogue);

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


    private void OnSave()
    {
        if (!Completion.Task.IsCompleted)
            Completion.SetResult(true);
    }

    private void OnCancel()
    {
        if (!Completion.Task.IsCompleted)
            Completion.SetResult(false);
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

}

