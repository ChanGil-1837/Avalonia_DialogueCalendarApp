using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DialogueCalendarApp.Views;

namespace DialogueCalendarApp.ViewModels
{
    public enum EditResultType
    {
        None,
        Saved,
        Deleted,
        Cancelled
    }
    public class CalendarEvent : ObservableObject
    {
        private int _id;
        public int Id { get => _id; set => SetProperty(ref _id, value); }

        private int _month;
        public int Month { get => _month; set => SetProperty(ref _month, value); }

        private int _day;
        public int Day { get => _day; set => SetProperty(ref _day, value); }

        private string _date;
        public string Date { get => _date; set => SetProperty(ref _date, value); }

        private string _time;
        public string Time { get => _time; set => SetProperty(ref _time, value); }

        private string _location;
        public string Location { get => _location; set => SetProperty(ref _location, value); }

        private string _conditions;
        public string Conditions { get => _conditions; set => SetProperty(ref _conditions, value); }

        private string _krDialogue;
        public string KRDialogue
        {
            get => _krDialogue;
            set
            {
                if (SetProperty(ref _krDialogue, value))
                    OnPropertyChanged(nameof(KRDialogueExists));
            }
        }

        private string _enDialogue;
        public string ENDialogue
        {
            get => _enDialogue;
            set
            {
                if (SetProperty(ref _enDialogue, value))
                    OnPropertyChanged(nameof(ENDialogueExists));
            }
        }

        public bool KRDialogueExists
        {
            get
            {
                if (string.IsNullOrWhiteSpace(KRDialogue)) return false;
                var path = KRDialogue;
                if (!Path.HasExtension(path)) path += ".json";
                return File.Exists(path);
            }
        }

        public bool ENDialogueExists
        {
            get
            {
                if (string.IsNullOrWhiteSpace(ENDialogue)) return false;
                var path = ENDialogue;
                if (!Path.HasExtension(path)) path += ".json";
                return File.Exists(path);
            }
        }

        private string _desc;
        public string Desc { get => _desc; set => SetProperty(ref _desc, value); }
    }

    public class CalendarDayViewModel : ObservableObject
    {
        private int _dayNumber;
        public int DayNumber { get => _dayNumber; set => SetProperty(ref _dayNumber, value); }

        private bool _isCurrentMonth;
        public bool IsCurrentMonth { get => _isCurrentMonth; set => SetProperty(ref _isCurrentMonth, value); }

        public ObservableCollection<CalendarEvent> Events { get; } = new();
    }

    public class CalendarPageViewModel : PageViewModelBase
    {
        private Dictionary<string, int> _monthStartDays = new();
        private DayOfWeek _selectedStartDay = DayOfWeek.Sunday;

        public DayOfWeek SelectedStartDay
        {
            get => _selectedStartDay;
            set
            {
                if (SetProperty(ref _selectedStartDay, value))
                    RefreshCurrentMonth();
            }
        }

        public Array WeekDays => Enum.GetValues(typeof(DayOfWeek));

        public ICommand AddEventCommand { get; }
        public ICommand EditEventCommand { get; }
        public ICommand DeleteEventCommand { get; }

        public ObservableCollection<CalendarDayViewModel> Days { get; } = new();

        private string _currentMonth;
        public string CurrentMonth
        {
            get => _currentMonth;
            private set => SetProperty(ref _currentMonth, value);
        }

        public override bool CanNavigateNext { get => throw new NotImplementedException(); protected set => throw new NotImplementedException(); }
        public override bool CanNavigatePrevious { get => throw new NotImplementedException(); protected set => throw new NotImplementedException(); }

        private string _csvPath;
        Dictionary<string, int> monthMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { "Jan", 1 }, { "Feb", 2 }, { "Mar", 3 }, { "Apr", 4 },
            { "May", 5 }, { "Jun", 6 }, { "Jul", 7 }, { "Aug", 8 },
            { "Sep", 9 }, { "Oct", 10 }, { "Nov", 11 }, { "Dec", 12 }
        };

        public CalendarPageViewModel()
        {
            LoadSettings();

            AddEventCommand = new RelayCommand<(int DayNumber, Window ParentWindow)>(async tuple =>
            {
                int dayNumber = tuple.DayNumber;
                Window parentWindow = tuple.ParentWindow;
                if (parentWindow == null) return;
                monthMap.TryGetValue(CurrentMonth, out var monthVal);
                var events = ParseCsv(AppSettings.CSVPATH, monthVal);
                int newId = (events.Count > 0) ? events.Max(e => e.Id) + 1 : 1;

                // 요일 계산
                var weekDays = new[] { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };
                int startIndex = Array.IndexOf(weekDays, SelectedStartDay);
                if (startIndex < 0) startIndex = 0;
                string dayOfWeek = weekDays[(startIndex + (dayNumber - 1)) % 7];

                var vm = new EventEditViewModel(SelectedStartDay)
                {
                    Id = newId,
                    Month = monthVal.ToString(),
                    Day = dayNumber.ToString(),
                    Date = dayOfWeek,   // ← 여기서 요일 넣음
                    Time = "Morning",
                    Location = "Room",
                    Condition = "",
                    KRDialogue = "",
                    ENDialogue = "",
                    Desc = ""
                };

                var window = new EventEditWindow { DataContext = vm };
                await window.ShowDialog(parentWindow);

                if (vm.Completion.Task.IsCompletedSuccessfully && await vm.Completion.Task == EditResultType.Saved)
                {
                    events.Add(new CalendarEvent
                    {
                        Id = vm.Id,
                        Month = monthMap.TryGetValue(vm.Month, out var m) ? m : 1,
                        Day = int.TryParse(vm.Day, out int d) ? d : 1,
                        Date = vm.Date,
                        Time = vm.Time,
                        Location = vm.Location,
                        Conditions = vm.Condition,
                        KRDialogue = vm.KRDialogue,
                        ENDialogue = vm.ENDialogue,
                        Desc = vm.Desc
                    });
                    SaveCsv(AppSettings.CSVPATH, events);
                    RefreshCurrentMonth();
                }
            });

            EditEventCommand = new RelayCommand<(int id, Window ParentWindow)>(async tuple =>
            {
                int id = tuple.id;
                Window parentWindow = tuple.ParentWindow;
                if (parentWindow == null) return;
                monthMap.TryGetValue(CurrentMonth, out var monthVal);
                var events = ParseCsv(AppSettings.CSVPATH, monthVal); // 전체 읽기 (삭제 대비)
                var ev = events.FirstOrDefault(e => e.Id == id);
                if (ev == null) return;

                var vm = new EventEditViewModel(SelectedStartDay, id)
                {
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
                await window.ShowDialog(parentWindow); // 창이 닫힐 때까지 대기
                var result = await vm.Completion.Task;

                switch (result)
                {
                    case EditResultType.Saved:
                        var oldEvent = ev;
                        int oldDay = oldEvent.Day;
                        int oldMonth = oldEvent.Month;

                        int newDay = int.TryParse(vm.Day, out int d) ? d : 1;
                        int newMonth;
                        if (!int.TryParse(vm.Month, out newMonth))
                        {
                            if (!monthMap.TryGetValue(vm.Month, out newMonth))
                            {
                                newMonth = oldMonth;
                            }
                        }

                        string krPath = vm.KRDialogue;
                        string enPath = vm.ENDialogue;

                        if (oldDay != newDay || oldMonth != newMonth)
                        {
                            string newMonthName = new DateTime(DateTime.Now.Year, newMonth, 1).ToString("MMM", CultureInfo.InvariantCulture);
                            krPath = MoveFile(oldEvent.KRDialogue, newDay, newMonthName);
                            enPath = MoveFile(oldEvent.ENDialogue, newDay, newMonthName);
                        }
                        
                        events.RemoveAll(e => e.Id == oldEvent.Id);

                        events.Add(new CalendarEvent
                        {
                            Id = vm.Id,
                            Month = newMonth,
                            Day = newDay,
                            Date = vm.Date,
                            Time = vm.Time,
                            Location = vm.Location,
                            Conditions = vm.Condition,
                            KRDialogue = krPath,
                            ENDialogue = enPath,
                            Desc = vm.Desc
                        });

                        SaveCsv(AppSettings.CSVPATH, events);
                        RefreshCurrentMonth();
                        break;

                    case EditResultType.Deleted:
                        events.RemoveAll(e => e.Id == ev.Id);
                        TryDeleteFile(ev.KRDialogue);
                        TryDeleteFile(ev.ENDialogue);
                        SaveCsv(AppSettings.CSVPATH, events);
                        RefreshCurrentMonth();
                        break;

                    case EditResultType.Cancelled:
                    case EditResultType.None:
                    default:
                        // 아무것도 안 함
                        break;
                }
            });


            
        }
        private void TryDeleteFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;

            // 확장자 없으면 .json 붙이기
            if (!Path.HasExtension(path))
                path += ".json";

            if (File.Exists(path))
            {
                try
                {
                    File.Delete(path);
                }
                catch (Exception ex)
                {
                    // 로그 찍거나 무시
                    Console.WriteLine($"파일 삭제 실패: {path}, 예외: {ex.Message}");
                }
            }
        }

        private string MoveFile(string sourcePath, int newDay, string newMonthName)
        {
            if (string.IsNullOrWhiteSpace(sourcePath))
                return null;

            string sourceFilePath = sourcePath + ".json";

            if (!File.Exists(sourceFilePath))
                return null;

            try
            {
                string fileName = Path.GetFileName(sourceFilePath);
                var dirInfo = new DirectoryInfo(Path.GetDirectoryName(sourceFilePath));
                var monthDir = dirInfo.Parent;
                var langDir = monthDir?.Parent;
                var eventListDir = langDir?.Parent;

                if (eventListDir == null || langDir == null || string.IsNullOrWhiteSpace(newMonthName))
                    return null;

                string lang = langDir.Name;

                string newDir = Path.Combine(eventListDir.FullName, lang, newMonthName, newDay.ToString());
                Directory.CreateDirectory(newDir);

                string newFilePath = Path.Combine(newDir, fileName);

                if (!sourceFilePath.Equals(newFilePath, StringComparison.OrdinalIgnoreCase))
                {
                    File.Move(sourceFilePath, newFilePath);

                    string sourceMetaFilePath = sourceFilePath + ".meta";
                    string newMetaFilePath = newFilePath + ".meta";

                    if (File.Exists(sourceMetaFilePath))
                    {
                        File.Move(sourceMetaFilePath, newMetaFilePath);
                    }
                }

                return newFilePath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error moving file {sourcePath}: {ex.Message}");
                return null;
            }
        }


        private void LoadSettings()
        {
            _monthStartDays = CalendarSettings.Instance.MonthStartDays;
        }

        public void SetMonth(string monthName, string csvPath, DayOfWeek? firstDayOverride = null)
        {
            CurrentMonth = monthName;
            _csvPath = csvPath;
            if (!File.Exists(_csvPath)) return;
            monthMap.TryGetValue(CurrentMonth, out var monthVal);
            var events = ParseCsv(_csvPath, monthVal);
            DayOfWeek firstDay = firstDayOverride ?? (_monthStartDays.ContainsKey(monthName)
                                                    ? (DayOfWeek)_monthStartDays[monthName]
                                                    : DayOfWeek.Sunday);

            if (_selectedStartDay != firstDay)
            {
                _selectedStartDay = firstDay;
                OnPropertyChanged(nameof(SelectedStartDay));
            }

            if (!DateTime.TryParseExact(monthName, "MMM", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dt)) return;
            int monthNumber = dt.Month;

            GenerateMonthDays(monthNumber, events, firstDay);

            _monthStartDays[monthName] = (int)firstDay;
            CalendarSettings.Instance.Save();
        }

        private void RefreshCurrentMonth()
        {
            if (!string.IsNullOrEmpty(_currentMonth) && !string.IsNullOrEmpty(AppSettings.CSVPATH))
                SetMonth(_currentMonth, AppSettings.CSVPATH, SelectedStartDay);
        }

        private List<CalendarEvent> ParseCsv(string path, int filterMonth)
        {
            var lines = System.IO.File.ReadAllLines(path);
            var events = new List<CalendarEvent>();

            foreach (var line in lines.Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var cols = line.Split(',');
                if (cols.Length < 9) continue;

                int.TryParse(cols[0], out int id);
                int.TryParse(cols[1], out int month);
                int.TryParse(cols[2], out int day);

                // 필터된 월이 지정되어 있고, 현재 라인의 월과 다르면 스킵
                if (filterMonth != 0 && month != filterMonth) continue;

                string monthName;
                try
                {
                    monthName = new System.DateTime(System.DateTime.Now.Year, month, 1)
                        .ToString("MMM", System.Globalization.CultureInfo.InvariantCulture);
                }
                catch
                {
                    continue; // 유효하지 않은 월이면 스킵
                }

                string dialoguePathFragment = cols[7];
                string dialogueFile = dialoguePathFragment.Split('/').LastOrDefault();

                if (string.IsNullOrEmpty(dialogueFile)) continue;

                events.Add(new CalendarEvent
                {
                    Id = id,
                    Month = month,
                    Day = day,
                    Date = cols[3],
                    Time = cols[4],
                    Location = cols[5],
                    Conditions = cols[6],
                    KRDialogue = System.IO.Path.Combine(AppSettings.DIRPATH, "KR", monthName, day.ToString(), dialogueFile),
                    ENDialogue = System.IO.Path.Combine(AppSettings.DIRPATH, "EN", monthName, day.ToString(), dialogueFile),
                    Desc = cols[8]
                });
            }

            return events;
        }


        private void GenerateMonthDays(int month, List<CalendarEvent> events, DayOfWeek firstDay)
        {
            var year = DateTime.Now.Year;
            var newDays = new ObservableCollection<CalendarDayViewModel>();

            int startOffset = (int)firstDay;
            for (int i = 0; i < startOffset; i++)
                newDays.Add(new CalendarDayViewModel { DayNumber = 0, IsCurrentMonth = false });

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

                newDays.Add(dayVM);
            }

            // UI 스레드에서 컬렉션 갱신
            Dispatcher.UIThread.Post(() =>
            {
                Days.Clear();
                foreach (var d in newDays)
                    Days.Add(d);
            });
        }

        private void SaveCsv(string path, List<CalendarEvent> events)
        {
            using var writer = new StreamWriter(path, false);
            writer.WriteLine("id,month,day,date,time,location,conditions,dialogue,desc");

            foreach (var ev in events.OrderBy(e => e.Id))
            {
                string monthName = new System.DateTime(System.DateTime.Now.Year, ev.Month, 1).ToString("MMM", System.Globalization.CultureInfo.InvariantCulture);
                string dialogueFileName = System.IO.Path.GetFileNameWithoutExtension(ev.KRDialogue);
                string dialogueColumn = $"{monthName}/{ev.Day}/{dialogueFileName}";
                writer.WriteLine($"{ev.Id},{ev.Month},{ev.Day},{ev.Date},{ev.Time},{ev.Location},{ev.Conditions},{dialogueColumn},{ev.Desc}");
            }
        }
    }
}
