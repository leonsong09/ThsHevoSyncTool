using ThsHevoSyncTool.Formatting;

namespace ThsHevoSyncTool.Services;

public sealed record ExportPreviewItem(
    string CategoryId,
    string CategoryName,
    int FileCount,
    long TotalBytes);

public sealed record ExportPreviewSummary(
    string ZipPath,
    IReadOnlyList<ExportPreviewItem> Items)
{
    public int TotalFiles => Items.Sum(static x => x.FileCount);
    public long TotalBytes => Items.Sum(static x => x.TotalBytes);

    public string ToDisplayText()
    {
        var lines = new List<string>
        {
            $"导出目标：{ZipPath}",
            $"分类数：{Items.Count}",
            $"文件数：{TotalFiles}",
            $"大小：{ByteFormatter.Format(TotalBytes)}",
            string.Empty,
            "本次导出内容：",
        };

        lines.AddRange(Items.Select(static item =>
            $"- {item.CategoryName}（{item.FileCount} 文件，{ByteFormatter.Format(item.TotalBytes)}）"));

        return string.Join(Environment.NewLine, lines);
    }
}
