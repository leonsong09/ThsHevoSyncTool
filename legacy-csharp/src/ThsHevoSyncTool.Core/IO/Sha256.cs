using System.Security.Cryptography;

namespace ThsHevoSyncTool.Core.IO;

public static class Sha256
{
    private const int DefaultBufferSizeBytes = 1024 * 128;

    public static async Task<string> HashFileAsync(string filePath, CancellationToken cancellationToken)
    {
        await using var input = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: DefaultBufferSizeBytes,
            useAsync: true);

        using var sha = SHA256.Create();
        var hash = await sha.ComputeHashAsync(input, cancellationToken);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static async Task<string> CopyAndHashAsync(
        Stream input,
        Stream output,
        CancellationToken cancellationToken)
    {
        using var sha = SHA256.Create();
        var buffer = new byte[DefaultBufferSizeBytes];

        while (true)
        {
            var read = await input.ReadAsync(buffer, cancellationToken);
            if (read <= 0)
            {
                break;
            }

            sha.TransformBlock(buffer, 0, read, null, 0);
            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }

        sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        return Convert.ToHexString(sha.Hash ?? Array.Empty<byte>()).ToLowerInvariant();
    }
}

