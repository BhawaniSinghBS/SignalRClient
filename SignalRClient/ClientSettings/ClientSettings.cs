namespace SignalRClient.ClientSettings
{
    public class ClientSettings
    {
        public static string SignalRServerUrl { get; set; } = @"http://localhost:300"; // over rided in proram.cs
        public static string SignalRHubUrl { get; set; } = @"http://localhost:300/SignalRHub"; // over rided in proram.cs
        public const string TagEncruptionKey = "5a3afjhskjfhsifh7687";

        public static string AppContentRootPath { get; set; } = @"";
        public static string AppWebRootPath { get; set; } = @"wwwroot";
        public static string LogDirectoryPath { get; set; } = Path.Combine(AppContentRootPath, @"Logs");// overide with appsetings
        public static string SignalRLogFilePath { get; set; } = Path.Combine(LogDirectoryPath, $@"Logs{DateTime.UtcNow:dd-MM-yyyy HH-mm-ss}.txt");// overrirde with app settings
        public static object SignalRLoggingFileLockObject { get; set; } = new object();
        public static bool IsSignalRLoggingOn { get; set; } = false;// fill it from app settings
        public static DateTime LastTimeLoggedInFile { get; set; } = DateTime.MinValue;
    }
}
