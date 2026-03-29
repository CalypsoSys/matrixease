using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;

namespace MatrixEase.Tester;

public sealed class MatrixTestSpec
{
    public int Version { get; set; } = 1;
    public MatrixTestDefaults Defaults { get; set; } = new MatrixTestDefaults();
    public List<MatrixTestCase> Tests { get; set; } = new List<MatrixTestCase>();

    public static MatrixTestSpec CreateTemplate()
    {
        return new MatrixTestSpec
        {
            Defaults = new MatrixTestDefaults
            {
                HeaderRow = 1,
                HeaderRows = 1,
                IgnoreBlankRows = true,
                IgnoreTextCase = true,
                TrimLeadingWhitespace = true,
                TrimTrailingWhitespace = true,
            },
            Tests = new List<MatrixTestCase>
            {
                new MatrixTestCase
                {
                    Name = "sample-csv",
                    Type = "csv",
                    Enabled = true,
                    Path = "sample-data/data.csv",
                    Separator = ',',
                }
            }
        };
    }
}

public sealed class MatrixTestDefaults
{
    public int HeaderRow { get; set; } = 1;
    public int HeaderRows { get; set; } = 1;
    public bool IgnoreBlankRows { get; set; } = true;
    public bool IgnoreTextCase { get; set; } = true;
    public bool TrimLeadingWhitespace { get; set; } = true;
    public bool TrimTrailingWhitespace { get; set; } = true;
}

public sealed class MatrixTestCase
{
    public string Name { get; set; }
    public string Type { get; set; }
    public bool Enabled { get; set; } = true;
    public string Path { get; set; }
    public char? Separator { get; set; }
    public string SheetId { get; set; }
    public string Range { get; set; }
    public string GoogleClientId { get; set; }
    public string GoogleClientSecret { get; set; }
    public int? HeaderRow { get; set; }
    public int? HeaderRows { get; set; }
    public int? MaxRows { get; set; }
    public bool? IgnoreBlankRows { get; set; }
    public bool? IgnoreTextCase { get; set; }
    public bool? TrimLeadingWhitespace { get; set; }
    public bool? TrimTrailingWhitespace { get; set; }
    public string IgnoreCols { get; set; }
}

public static class SpecStorage
{
    public static MatrixTestSpec Load(string specPath)
    {
        if (!File.Exists(specPath))
        {
            throw new CliUsageException($"Spec file not found: {specPath}");
        }

        string json = File.ReadAllText(specPath);
        var spec = JsonConvert.DeserializeObject<MatrixTestSpec>(json);
        if (spec == null)
        {
            throw new CliUsageException($"Unable to parse spec file: {specPath}");
        }

        spec.Defaults ??= new MatrixTestDefaults();
        spec.Tests ??= new List<MatrixTestCase>();
        return spec;
    }

    public static void Save(string specPath, MatrixTestSpec spec)
    {
        string directory = Path.GetDirectoryName(Path.GetFullPath(specPath));
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string json = JsonConvert.SerializeObject(spec, Formatting.Indented, new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
        });
        File.WriteAllText(specPath, json + Environment.NewLine);
    }
}
