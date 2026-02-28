using ThsHevoSyncTool.Core.IO;

namespace ThsHevoSyncTool.Core.Tests;

public sealed class SafeRelativePathTests
{
    [Theory]
    [InlineData("bin\\users\\mx_1\\a.xml", "bin\\users\\mx_1\\a.xml")]
    [InlineData("bin/users/mx_1/a.xml", "bin\\users\\mx_1\\a.xml")]
    public void NormalizeRelativePath_NormalizesSeparators(string input, string expected)
    {
        var normalized = SafeRelativePath.NormalizeRelativePath(input);
        Assert.Equal(expected, normalized);
    }

    [Theory]
    [InlineData("..\\a")]
    [InlineData("bin\\..\\a")]
    [InlineData("./a")]
    [InlineData("bin/./a")]
    public void NormalizeRelativePath_RejectsDotSegments(string input)
    {
        Assert.Throws<ArgumentException>(() => SafeRelativePath.NormalizeRelativePath(input));
    }
}

