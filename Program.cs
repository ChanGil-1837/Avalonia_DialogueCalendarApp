using Avalonia;
using System;
using System.IO;

namespace DialogueCalendarApp;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    // Avalonia configuration, don't remove; also used by visual designer.
  public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}

public static class AppSettings
{
    // The path to your application's data folder
    public static string AppDataFolder { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DialogueCalendarApp");

    // The full path to the settings JSON file
    public static string SettingsFilePath { get; } = Path.Combine(AppDataFolder, "settings.json");
    public static string CSVPATH = "";
    public static string DIRPATH = "";
    public static string DIALOGUEAPPLOC = "/Users/chanhogil/Documents/Python/Dialogue/dist/DialogueApp";
}
