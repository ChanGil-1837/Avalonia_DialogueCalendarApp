using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Input;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DialogueCalendarApp.Views;

namespace DialogueCalendarApp.ViewModels
{
    public class CalendarEvent
    {
        public int Id { get; set; }
        public int Month { get; set; }
        public int Day { get; set; }
        public string Date { get; set; }
        public string Time { get; set; }
        public string Location { get; set; }
        public string Conditions { get; set; }

        // CSV에서 가져온 이름 그대로 둠 (확장자 없어도 됨)
        public string KRDialogue { get; set; }
        public string ENDialogue { get; set; }

        // 존재 여부 즉석 확인
        public bool KRDialogueExists
        {
            get
            {
                if (string.IsNullOrWhiteSpace(KRDialogue)) return false;
                var path = Path.Combine(AppSettings.DIRPATH, "KR", KRDialogue);
                if (!Path.HasExtension(path)) path += ".json";
                return File.Exists(path);
            }
        }

        public bool ENDialogueExists
        {
            get
            {
                if (string.IsNullOrWhiteSpace(ENDialogue)) return false;
                var path = Path.Combine(AppSettings.DIRPATH, "EN", ENDialogue);
                if (!Path.HasExtension(path)) path += ".json";
                return File.Exists(path);
            }
        }

        public string Desc { get; set; }
    }


    public class CalendarDayViewModel : ObservableObject
    {
        private int _dayNumber;
        public int DayNumber
        {
            get => _dayNumber;
            set => SetProperty(ref _dayNumber, value);
        }

        private bool _isCurrentMonth;
        public bool IsCurrentMonth
        {
            get => _isCurrentMonth;
            set => SetProperty(ref _isCurrentMonth, value);
        }

        public ObservableCollection<CalendarEvent> Events { get; } = new();
    }

    public class CalendarPageViewModel : PageViewModelBase
    {
        private Dictionary<string, int> _monthStartDays = new(); // 저장용: 월별 시작 요일

        // 시작 요일 선택용
        private DayOfWeek _selectedStartDay = DayOfWeek.Sunday;
        public DayOfWeek SelectedStartDay
        {
            get => _selectedStartDay;
            set
            {
                if (SetProperty(ref _selectedStartDay, value))
                {
                    if (!string.IsNullOrEmpty(CurrentMonth) && !string.IsNullOrEmpty(_csvPath))
                        SetMonth(CurrentMonth, _csvPath, SelectedStartDay);
                }
            }
        }

        public Array WeekDays => Enum.GetValues(typeof(DayOfWeek));

        public ICommand AddEventCommand { get; }
        public ICommand EditEventCommand { get; }

        public ObservableCollection<CalendarDayViewModel> Days { get; } = new();

        private string _currentMonth;
        public string CurrentMonth
        {
            get => _currentMonth;
            private set
            {
                if (_currentMonth != value)
                {
                    _currentMonth = value;
                    OnPropertyChanged(nameof(CurrentMonth));
                }
            }
        }

        public override bool CanNavigateNext { get => throw new NotImplementedException(); protected set => throw new NotImplementedException(); }
        public override bool CanNavigatePrevious { get => throw new NotImplementedException(); protected set => throw new NotImplementedException(); }

        private string _csvPath;

        public CalendarPageViewModel()
        {
            LoadSettings();
            AddEventCommand = new RelayCommand<(int DayNumber, Window ParentWindow)>(async tuple =>
            {
                int dayNumber = tuple.DayNumber;
                Window parentWindow = tuple.ParentWindow;

                if (parentWindow == null)
                    return;

                Console.WriteLine($"{dayNumber}일의 이벤트를 추가합니다.");

                // CSV에서 기존 이벤트 목록 읽기
                var events = ParseCsv(AppSettings.CSVPATH);

                // 새 ID는 가장 큰 ID + 1 (이벤트가 없으면 1부터 시작)
                int newId = (events.Count > 0) ? events.Max(e => e.Id) + 1 : 1;


                // date 문자열은 "MM/dd" 같은 포맷으로 만들 수 있음

                var vm = new EventEditViewModel()
                {
                    Id = newId,
                    Month = "",
                    Day = "",
                    Date = "",
                    Time = "",
                    Location = "",
                    Condition = "",
                    KRDialogue = "",
                    ENDialogue = "",
                    Desc = ""
                };

                var window = new EventEditWindow { DataContext = vm };
                await window.ShowDialog(parentWindow);
            });


            EditEventCommand = new RelayCommand<(int id, Window ParentWindow)>(async tuple =>
            {
                int id = tuple.id;
                Window parentWindow = tuple.ParentWindow;

                if (parentWindow == null)
                    return;

                // CSV에서 해당 id의 이벤트를 찾음
                var events = ParseCsv(AppSettings.CSVPATH);
                var ev = events.FirstOrDefault(e => e.Id == id);

                if (ev == null)
                {
                    Console.WriteLine($"ID {id}에 해당하는 이벤트를 찾을 수 없습니다.");
                    return;
                }

                // Id를 이용해서 EventEditViewModel 초기화
                var vm = new EventEditViewModel(id)
                {
                    // 필요한 경우 추가 데이터도 넘김
                    Id = ev.Id,
                    Month = ev.Month + "",
                    Day = ev.Day + "",
                    Date = ev.Date,
                    Time = ev.Time,
                    Location = ev.Location,
                    Condition = ev.Conditions,
                    KRDialogue = ev.KRDialogue,
                    ENDialogue = ev.ENDialogue,
                    Desc = ev.Desc
                };

                var window = new EventEditWindow { DataContext = vm };
                await window.ShowDialog(parentWindow);
            });
        }


        // monthName: "Feb" 같은 문자열
        public void SetMonth(string monthName, string csvPath, DayOfWeek? firstDayOverride = null)
        {
            CurrentMonth = monthName;
            _csvPath = csvPath;
            if (!File.Exists(_csvPath)) return;

            var events = ParseCsv(_csvPath);
            var i = CalendarSettings.Instance.MonthStartDays;
            // 첫 요일 결정: 사용자 선택 > 저장된 값 > 기본 Sunday
            DayOfWeek firstDay = firstDayOverride ?? (CalendarSettings.Instance.MonthStartDays.ContainsKey(monthName) 
                                                    ? (DayOfWeek)CalendarSettings.Instance.MonthStartDays[monthName] 
                                                    : DayOfWeek.Sunday);

            if (!DateTime.TryParseExact(monthName, "MMM", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dt)) return;
            int monthNumber = dt.Month;

            GenerateMonthDays(monthNumber, events, firstDay);

            // 선택 요일 저장
            CalendarSettings.Instance.MonthStartDays[monthName] = (int)firstDay;

            // 현재 앱 경로도 업데이트
            CalendarSettings.Instance.DialogueAppPath = AppSettings.DIALOGUEAPPLOC;

            // 변경 사항 저장
            CalendarSettings.Instance.Save();
        }

        private void LoadSettings()
        {
            _monthStartDays = CalendarSettings.Instance.MonthStartDays;
        }
    

        private List<CalendarEvent> ParseCsv(string path)
        {
            var lines = File.ReadAllLines(path);
            var events = new List<CalendarEvent>();

            foreach (var line in lines.Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var cols = line.Split(',');

                if (cols.Length < 9)
                    continue;

                if (!int.TryParse(cols[1], out int month))
                    continue;

                if (!int.TryParse(cols[2], out int day))
                    continue;

                if (!int.TryParse(cols[0], out int id))
                    id = -1;

                events.Add(new CalendarEvent
                {
                    Id = id,
                    Month = month,
                    Day = day,
                    Date = cols[3],
                    Time = cols[4],
                    Location = cols[5],
                    Conditions = cols[6],
                    KRDialogue = Path.Combine(AppSettings.DIRPATH, "KR", cols[7]),
                    ENDialogue = Path.Combine(AppSettings.DIRPATH, "EN", cols[7]),
                    Desc = cols[8]
                });
                
            }

            return events;
        }

        private void GenerateMonthDays(int month, List<CalendarEvent> events, DayOfWeek firstDay)
        {
            Days.Clear();
            int year = DateTime.Now.Year;

            // 선택된 첫날 기준으로 달력 빈칸 계산
            int startOffset = (int)firstDay;
            for (int i = 0; i < startOffset; i++)
                Days.Add(new CalendarDayViewModel { DayNumber = 0, IsCurrentMonth = false });

            int daysInMonth = DateTime.DaysInMonth(year, month);
            for (int day = 1; day <= daysInMonth; day++)
            {
                var dayVM = new CalendarDayViewModel
                {
                    DayNumber = day,
                    IsCurrentMonth = true
                };

                var dayEvents = events.Where(e => e.Month == month && e.Day == day).ToList();
                foreach (var ev in dayEvents)
                    dayVM.Events.Add(ev);

                Days.Add(dayVM);
            }
        }
    }
    

}
