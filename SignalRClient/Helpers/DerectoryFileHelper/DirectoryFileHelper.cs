namespace SignalRClient.Helpers.DerectoryFileHelper
{
    public class DirectoryFileHelper
    {
        public static bool CreateLogFileAtGivenPathIfNotPresent(string fileCompletePathFithFileName)
        {
            try
            {
                return ClientSettings.ClientSettings.IsSignalRLoggingOn && EnsureDirectoryExistsOrCreate(fileCompletePathFithFileName)
                        &&
                    // Create the file if it doesn't exist
                    CreateFileIfAllPathDirectoriesAreThere(fileCompletePathFithFileName);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        public static bool CreateFileAtGivenPathIfNotPresent(string fileCompletePathFithFileName)
        {
            try
            {
                return EnsureDirectoryExistsOrCreate(fileCompletePathFithFileName)
                        &&
                    // Create the file if it doesn't exist
                    CreateFileIfAllPathDirectoriesAreThere(fileCompletePathFithFileName);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        private static bool EnsureDirectoryExistsOrCreate(string filePath)
        {
            try
            {
                // Get the directory part of the file path
                string directoryPath = Path.GetDirectoryName(filePath);

                // Create the directory if it doesn't exist
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
                return true;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private static bool CreateFileIfAllPathDirectoriesAreThere(string filePath)
        {
            try
            {
                // Create the file if it doesn't exist
                if (!File.Exists(filePath))
                {
                    // You can write content to the file here if needed
                    File.Create(filePath).Close();
                }
                return true;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
