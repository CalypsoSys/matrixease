using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace MatrixEase.Tester;

public abstract class CliCommand
{
    public abstract Task<int> ExecuteAsync(TextWriter stdout, TextWriter stderr, CancellationToken cancellationToken);
}

public sealed class HelpCommand : CliCommand
{
    public override async Task<int> ExecuteAsync(TextWriter stdout, TextWriter stderr, CancellationToken cancellationToken)
    {
        await stdout.WriteLineAsync(CliHelp.Text);
        return 0;
    }
}

public sealed class SpecInitCommand : CliCommand
{
    public string SpecPath { get; init; }
    public bool Force { get; init; }

    public override async Task<int> ExecuteAsync(TextWriter stdout, TextWriter stderr, CancellationToken cancellationToken)
    {
        if (File.Exists(SpecPath) && !Force)
        {
            throw new CliUsageException($"Spec file already exists: {SpecPath}. Use --force to overwrite.");
        }

        var spec = MatrixTestSpec.CreateTemplate();
        SpecStorage.Save(SpecPath, spec);
        await stdout.WriteLineAsync($"Created spec template at {SpecPath}");
        return 0;
    }
}

public sealed class SpecListCommand : CliCommand
{
    public string SpecPath { get; init; }

    public override async Task<int> ExecuteAsync(TextWriter stdout, TextWriter stderr, CancellationToken cancellationToken)
    {
        var spec = SpecStorage.Load(SpecPath);
        await stdout.WriteLineAsync($"Spec: {SpecPath}");
        await stdout.WriteLineAsync($"Tests: {spec.Tests.Count}");

        for (int i = 0; i < spec.Tests.Count; i++)
        {
            var test = spec.Tests[i];
            await stdout.WriteLineAsync($"{i + 1}. {(test.Enabled ? "[x]" : "[ ]")} {test.Name} ({test.Type})");
        }

        return 0;
    }
}

public sealed class SpecAddCommand : CliCommand
{
    public string SpecPath { get; init; }
    public MatrixTestCase TestCase { get; init; }

    public override async Task<int> ExecuteAsync(TextWriter stdout, TextWriter stderr, CancellationToken cancellationToken)
    {
        var spec = File.Exists(SpecPath) ? SpecStorage.Load(SpecPath) : MatrixTestSpec.CreateTemplate();
        if (spec.Tests.Any(test => string.Equals(test.Name, TestCase.Name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new CliUsageException($"A test named '{TestCase.Name}' already exists in {SpecPath}.");
        }

        spec.Tests.Add(TestCase);
        SpecStorage.Save(SpecPath, spec);
        await stdout.WriteLineAsync($"Added test '{TestCase.Name}' to {SpecPath}");
        return 0;
    }
}

public sealed class RunCommand : CliCommand
{
    public string SpecPath { get; init; }
    public string OutputDirectory { get; init; }
    public bool Baseline { get; init; }
    public int? MaxRowsOverride { get; init; }
    public bool IncludeDisabled { get; init; }
    public IReadOnlyList<string> OnlyNames { get; init; } = Array.Empty<string>();

    public override async Task<int> ExecuteAsync(TextWriter stdout, TextWriter stderr, CancellationToken cancellationToken)
    {
        var spec = SpecStorage.Load(SpecPath);
        var runner = new MatrixTestRunner(spec, OutputDirectory, stdout, stderr);
        return await runner.RunAsync(Baseline, MaxRowsOverride, OnlyNames, IncludeDisabled, cancellationToken);
    }
}

public sealed class CryptoRoundTripCommand : CliCommand
{
    public string UserFolder { get; init; }
    public Guid MangaGuid { get; init; }

    public override async Task<int> ExecuteAsync(TextWriter stdout, TextWriter stderr, CancellationToken cancellationToken)
    {
        var encoded = MatrixIdCrypto.Encode(UserFolder, MangaGuid);
        var decoded = MatrixIdCrypto.Decode(encoded);

        await stdout.WriteLineAsync($"encoded={encoded}");
        await stdout.WriteLineAsync($"userFolder={decoded.Item1}");
        await stdout.WriteLineAsync($"mangaGuid={decoded.Item2}");
        return 0;
    }
}

public static class MatrixIdCrypto
{
    private const int KeySize = 256;
    private static readonly string SessionGuid = Guid.NewGuid().ToString();

    public static string Encode(string userFolder, Guid mangaGuid)
    {
        var appHash = GetAppHash();
        byte[] initVectorBytes = appHash.Item2;
        List<byte> plainBytes = new List<byte>();
        plainBytes.AddRange(mangaGuid.ToByteArray());
        plainBytes.AddRange(Encoding.UTF8.GetBytes(userFolder));

        using (PasswordDeriveBytes password = new PasswordDeriveBytes(appHash.Item1, null))
        {
            byte[] keyBytes = password.GetBytes(KeySize / 8);
            using (RijndaelManaged symmetricKey = new RijndaelManaged())
            {
                symmetricKey.Mode = CipherMode.CBC;
                using (ICryptoTransform encryptor = symmetricKey.CreateEncryptor(keyBytes, initVectorBytes))
                {
                    using (MemoryStream memoryStream = new MemoryStream())
                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                    {
                        byte[] plainTextBytes = plainBytes.ToArray();
                        cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                        cryptoStream.FlushFinalBlock();
                        return Convert.ToBase64String(memoryStream.ToArray());
                    }
                }
            }
        }
    }

    public static Tuple<string, Guid> Decode(string mxesId)
    {
        var appHash = GetAppHash();
        byte[] initVectorBytes = appHash.Item2;
        byte[] cipherTextBytes = Convert.FromBase64String(mxesId);

        using (PasswordDeriveBytes password = new PasswordDeriveBytes(appHash.Item1, null))
        {
            byte[] keyBytes = password.GetBytes(KeySize / 8);
            using (RijndaelManaged symmetricKey = new RijndaelManaged())
            {
                symmetricKey.Mode = CipherMode.CBC;
                using (ICryptoTransform decryptor = symmetricKey.CreateDecryptor(keyBytes, initVectorBytes))
                using (MemoryStream memoryStream = new MemoryStream(cipherTextBytes))
                using (CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                {
                    byte[] guidBytes = new byte[16];
                    cryptoStream.Read(guidBytes, 0, 16);
                    Guid mangaGuid = new Guid(guidBytes);

                    byte[] userFolderBytes = new byte[cipherTextBytes.Length - 16];
                    int userFolderByteCount = cryptoStream.Read(userFolderBytes, 0, userFolderBytes.Length);
                    string userFolder = Encoding.UTF8.GetString(userFolderBytes, 0, userFolderByteCount);

                    return Tuple.Create(userFolder, mangaGuid);
                }
            }
        }
    }

    private static Tuple<string, byte[]> GetAppHash()
    {
        byte[] salt = new ASCIIEncoding().GetBytes("Calypso Systems,");
        return Tuple.Create(SessionGuid, salt);
    }
}

public sealed class CliUsageException : Exception
{
    public CliUsageException(string message) : base(message)
    {
    }
}

public static class CliParser
{
    public static CliCommand Parse(string[] args)
    {
        if (args.Length == 0 || IsHelp(args[0]))
        {
            return new HelpCommand();
        }

        return args[0].ToLowerInvariant() switch
        {
            "spec" => ParseSpec(args.Skip(1).ToArray()),
            "run" => ParseRun(args.Skip(1).ToArray(), baseline: false),
            "baseline" => ParseRun(args.Skip(1).ToArray(), baseline: true),
            "crypto" => ParseCrypto(args.Skip(1).ToArray()),
            _ => throw new CliUsageException($"Unknown command '{args[0]}'."),
        };
    }

    private static CliCommand ParseSpec(string[] args)
    {
        if (args.Length == 0)
        {
            throw new CliUsageException("Missing spec subcommand.");
        }

        return args[0].ToLowerInvariant() switch
        {
            "init" => new SpecInitCommand
            {
                SpecPath = RequireOption(args, "--spec"),
                Force = HasFlag(args, "--force"),
            },
            "list" => new SpecListCommand
            {
                SpecPath = RequireOption(args, "--spec"),
            },
            "add" => new SpecAddCommand
            {
                SpecPath = RequireOption(args, "--spec"),
                TestCase = ParseTestCase(args),
            },
            _ => throw new CliUsageException($"Unknown spec subcommand '{args[0]}'."),
        };
    }

    private static CliCommand ParseRun(string[] args, bool baseline)
    {
        return new RunCommand
        {
            SpecPath = RequireOption(args, "--spec"),
            OutputDirectory = RequireOption(args, "--output"),
            Baseline = baseline,
            MaxRowsOverride = TryGetIntOption(args, "--max-rows"),
            IncludeDisabled = HasFlag(args, "--include-disabled"),
            OnlyNames = SplitCsv(GetOption(args, "--only")),
        };
    }

    private static CliCommand ParseCrypto(string[] args)
    {
        if (args.Length == 0 || !string.Equals(args[0], "roundtrip", StringComparison.OrdinalIgnoreCase))
        {
            throw new CliUsageException("Only 'crypto roundtrip' is supported.");
        }

        return new CryptoRoundTripCommand
        {
            UserFolder = RequireOption(args, "--user-folder"),
            MangaGuid = Guid.Parse(RequireOption(args, "--manga-guid")),
        };
    }

    private static MatrixTestCase ParseTestCase(string[] args)
    {
        string type = RequireOption(args, "--type").ToLowerInvariant();
        var test = new MatrixTestCase
        {
            Name = RequireOption(args, "--name"),
            Type = type,
            Enabled = true,
            HeaderRow = TryGetIntOption(args, "--header-row") ?? 1,
            HeaderRows = TryGetIntOption(args, "--header-rows") ?? 1,
            IgnoreBlankRows = TryGetBoolOption(args, "--ignore-blank-rows") ?? true,
            IgnoreTextCase = TryGetBoolOption(args, "--ignore-text-case") ?? true,
            TrimLeadingWhitespace = TryGetBoolOption(args, "--trim-leading-whitespace") ?? true,
            TrimTrailingWhitespace = TryGetBoolOption(args, "--trim-trailing-whitespace") ?? true,
            IgnoreCols = GetOption(args, "--ignore-cols"),
        };

        switch (type)
        {
            case "csv":
                test.Path = RequireOption(args, "--path");
                test.Separator = (GetOption(args, "--separator") ?? ",").Single();
                break;
            case "excel":
                test.Path = RequireOption(args, "--path");
                break;
            case "google":
                test.SheetId = RequireOption(args, "--sheet-id");
                test.Range = RequireOption(args, "--range");
                test.GoogleClientId = RequireOption(args, "--google-client-id");
                test.GoogleClientSecret = RequireOption(args, "--google-client-secret");
                break;
            default:
                throw new CliUsageException($"Unsupported test type '{type}'.");
        }

        return test;
    }

    private static bool IsHelp(string value)
    {
        return value is "-h" or "--help" or "help";
    }

    private static bool HasFlag(IReadOnlyList<string> args, string name)
    {
        return args.Any(arg => string.Equals(arg, name, StringComparison.OrdinalIgnoreCase));
    }

    private static string RequireOption(IReadOnlyList<string> args, string name)
    {
        return GetOption(args, name) ?? throw new CliUsageException($"Missing required option '{name}'.");
    }

    private static string GetOption(IReadOnlyList<string> args, string name)
    {
        for (int i = 0; i < args.Count - 1; i++)
        {
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
        }

        return null;
    }

    private static int? TryGetIntOption(IReadOnlyList<string> args, string name)
    {
        string value = GetOption(args, name);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return int.Parse(value, CultureInfo.InvariantCulture);
    }

    private static bool? TryGetBoolOption(IReadOnlyList<string> args, string name)
    {
        string value = GetOption(args, name);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return bool.Parse(value);
    }

    private static IReadOnlyList<string> SplitCsv(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Array.Empty<string>();
        }

        return value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
