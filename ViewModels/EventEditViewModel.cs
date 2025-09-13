
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

    private string _time = "";
    public string Time
    {
        get => _time;
        set => SetProperty(ref _time, value);
    }


    private string _location = "";
    public string Location { get => _location; set => SetProperty(ref _location, value); }

    private string _krDialogue = "";
    public string KRDialogue { get => _krDialogue+".json"; set => SetProperty(ref _krDialogue, value); }
    private string _enDialogue = "";
    public string ENDialogue { get => _enDialogue+".json"; set => SetProperty(ref _enDialogue, value); }

    private string _condition = "";
    public string Condition { get => _condition; set => SetProperty(ref _condition, value); }


    private string _desc = "";
    public string Desc { get => _desc; set => SetProperty(ref _desc, value); }


    public ICommand OpenKRCommand { get; }
    public ICommand OpenENCommand { get; }

    // Command properties
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    // TaskCompletionSource를 사용해 모달 결과 전달
    public TaskCompletionSource<bool> Completion { get; } = new();

    public EventEditViewModel(int id = -1)
    {
        SaveCommand = new RelayCommand(OnSave);
        CancelCommand = new RelayCommand(OnCancel);
        OpenKRCommand = new RelayCommand(OpenKR);
        OpenENCommand = new RelayCommand(OpenEN);
        
    }
    private void OpenKR()
    {
        OpenDialogue(KRDialogue);
    }

    private void OpenEN()
    {
        OpenDialogue(ENDialogue);
    }
    private void OpenDialogue(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return;

        var psi = new ProcessStartInfo
        {
            FileName = AppSettings.DIALOGUEAPPLOC,
            Arguments = $"\"{path}\"",
            UseShellExecute = true
        };
        Process.Start(psi);
    }
    private void OnSave()
    {
        // 유효성 검사 로직을 여기에 추가할 수 있습니다.
        // 예를 들어: if (string.IsNullOrWhiteSpace(Location)) return;

        // Save가 성공했음을 알리고 모달을 닫습니다.
        Completion.SetResult(true);
    }

    private void OnCancel()
    {
        // 취소되었음을 알리고 모달을 닫습니다.
        Completion.SetResult(false);
    }
}

