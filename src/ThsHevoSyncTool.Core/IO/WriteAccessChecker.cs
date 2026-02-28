namespace ThsHevoSyncTool.Core.IO;

public static class WriteAccessChecker
{
    public static void AssertDirectoryWritable(string directoryPath)
    {
        var fullPath = Path.GetFullPath(directoryPath);
        Directory.CreateDirectory(fullPath);

        var testFile = Path.Combine(fullPath, $"write_test_{Guid.NewGuid():N}.tmp");
        try
        {
            File.WriteAllText(testFile, "ok");
        }
        finally
        {
            if (File.Exists(testFile))
            {
                File.Delete(testFile);
            }
        }
    }
}

