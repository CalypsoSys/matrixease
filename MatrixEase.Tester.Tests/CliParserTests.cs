using Xunit;

namespace MatrixEase.Tester.Tests;

public class CliParserTests
{
    [Fact]
    public void SpecAddCsv_ParsesExpectedFields()
    {
        var command = CliParser.Parse(new[]
        {
            "spec", "add",
            "--spec", "tester.json",
            "--type", "csv",
            "--name", "sample",
            "--path", "input.csv",
            "--separator", ";",
            "--header-row", "2",
        });

        var add = Assert.IsType<SpecAddCommand>(command);
        Assert.Equal("tester.json", add.SpecPath);
        Assert.Equal("sample", add.TestCase.Name);
        Assert.Equal("csv", add.TestCase.Type);
        Assert.Equal("input.csv", add.TestCase.Path);
        Assert.Equal(';', add.TestCase.Separator);
        Assert.Equal(2, add.TestCase.HeaderRow);
    }

    [Fact]
    public void RunCommand_ParsesOnlyAndBaselineFlags()
    {
        var command = CliParser.Parse(new[]
        {
            "baseline",
            "--spec", "tester.json",
            "--output", "artifacts",
            "--only", "one,two",
            "--max-rows", "15",
        });

        var run = Assert.IsType<RunCommand>(command);
        Assert.True(run.Baseline);
        Assert.Equal("tester.json", run.SpecPath);
        Assert.Equal("artifacts", run.OutputDirectory);
        Assert.Equal(15, run.MaxRowsOverride);
        Assert.Equal(new[] { "one", "two" }, run.OnlyNames);
    }

    [Fact]
    public void SpecStorage_RoundTripsJsonSpec()
    {
        string tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        string specPath = Path.Combine(tempRoot, "tester.json");

        try
        {
            var spec = new MatrixTestSpec
            {
                Defaults = new MatrixTestDefaults
                {
                    HeaderRow = 2,
                    HeaderRows = 3,
                },
                Tests = new List<MatrixTestCase>
                {
                    new MatrixTestCase
                    {
                        Name = "google-sheet",
                        Type = "google",
                        SheetId = "sheet-id",
                        Range = "A1:B5",
                        GoogleClientId = "client-id",
                        GoogleClientSecret = "client-secret",
                    }
                }
            };

            SpecStorage.Save(specPath, spec);
            var loaded = SpecStorage.Load(specPath);

            Assert.Equal(2, loaded.Defaults.HeaderRow);
            Assert.Single(loaded.Tests);
            Assert.Equal("google-sheet", loaded.Tests[0].Name);
            Assert.Equal("sheet-id", loaded.Tests[0].SheetId);

            string json = File.ReadAllText(specPath);
            Assert.Contains("\"tests\"", json);
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }
}
