namespace ThsHevoSyncTool.Formatting;

public static class ByteFormatter
{
    private const long OneKiB = 1024;
    private const long OneMiB = OneKiB * 1024;
    private const long OneGiB = OneMiB * 1024;

    public static string Format(long bytes)
    {
        if (bytes < OneKiB)
        {
            return $"{bytes} B";
        }

        if (bytes < OneMiB)
        {
            return $"{bytes / (double)OneKiB:0.##} KiB";
        }

        if (bytes < OneGiB)
        {
            return $"{bytes / (double)OneMiB:0.##} MiB";
        }

        return $"{bytes / (double)OneGiB:0.##} GiB";
    }
}

