using ApiIncidentFastTrack.Services;

namespace ApiIncidentFastTrack.Tests.Services;

// The uploaded name is only for display and the download name, but it must still be tidy and fit the column.
public class AttachmentNameCleaningTests
{
    [Theory]
    [InlineData("photo.jpg", "photo.jpg")]
    [InlineData(@"C:\Users\ana\Desktop\photo.jpg", "photo.jpg")]
    [InlineData("folder/sub/report.pdf", "report.pdf")]
    [InlineData(@"..\..\appsettings.json", "appsettings.json")]
    [InlineData("  spaced.txt  ", "spaced.txt")]
    [InlineData("bad\u0000name\u0007.txt", "badname.txt")]
    public void FileName_KeepsOnlyTheLastPart(string uploaded, string expected)
    {
        Assert.Equal(expected, IncidentAttachmentService.CleanFileName(uploaded));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(@"C:\folder\")]
    public void FileName_Empty_BecomesFile(string? uploaded)
    {
        Assert.Equal("file", IncidentAttachmentService.CleanFileName(uploaded));
    }

    [Fact]
    public void FileName_TooLong_IsShortenedKeepingTheExtension()
    {
        var cleaned = IncidentAttachmentService.CleanFileName(new string('a', 300) + ".pdf");

        Assert.Equal(260, cleaned.Length);
        Assert.EndsWith(".pdf", cleaned);
    }

    [Theory]
    [InlineData("image/png", "image/png")]
    [InlineData(" application/pdf ", "application/pdf")]
    [InlineData(null, "application/octet-stream")]
    [InlineData("", "application/octet-stream")]
    public void ContentType_IsTrimmedOrDefaulted(string? declared, string expected)
    {
        Assert.Equal(expected, IncidentAttachmentService.CleanContentType(declared));
    }

    [Fact]
    public void ContentType_TooLong_IsDefaulted()
    {
        Assert.Equal("application/octet-stream", IncidentAttachmentService.CleanContentType(new string('x', 101)));
    }
}
