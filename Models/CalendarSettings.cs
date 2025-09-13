using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using DialogueCalendarApp;

public class CalendarSettings
{
    private static CalendarSettings? _instance;

    public static CalendarSettings Instance
    {
        get
        {
            if (_instance == null)
                _instance = new CalendarSettings(); // 최초 생성 시 자동 로드
            return _instance;
        }
    }
    public CalendarSettings() { }


    public Dictionary<string, int> MonthStartDays { get; set; } = new();
    public string DialogueAppPath { get; set; } = "";

    private static string SettingsFilePath => Path.Combine(AppSettings.AppDataFolder, "settings.json");

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(AppSettings.AppDataFolder);
            DialogueAppPath = AppSettings.DIALOGUEAPPLOC; // 현재 경로 반영
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFilePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to save settings: {ex.Message}");
        }
    }

    public void Load()
    {
        if (File.Exists(SettingsFilePath))
        {
            try
            {
                var json = File.ReadAllText(SettingsFilePath);
                Console.WriteLine($"Loading settings from {SettingsFilePath}");
                Console.WriteLine($"JSON content: {json}");

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                };
                var settings = JsonSerializer.Deserialize<CalendarSettings>(json, options);

                if (settings != null)
                {
                    Console.WriteLine($"Deserialized DialogueAppPath: {settings.DialogueAppPath}");
                    if (settings.MonthStartDays != null)
                    {
                        Console.WriteLine($"MonthStartDays count: {settings.MonthStartDays.Count}");
                        foreach (var kvp in settings.MonthStartDays)
                        {
                            Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("MonthStartDays is null after deserialization.");
                    }

                    MonthStartDays = settings.MonthStartDays ?? new Dictionary<string, int>();
                    DialogueAppPath = settings.DialogueAppPath ?? "";
                    AppSettings.DIALOGUEAPPLOC = DialogueAppPath; // 전역 경로 반영
                }
                else
                {
                    Console.WriteLine("Deserialization returned null settings object.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load settings: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }
        else
        {
            Console.WriteLine($"Settings file not found at {SettingsFilePath}");
        }
    }
}
