using System.Text;
using MatrixEase.Manga.Utility;

namespace MatrixEase.Tester;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        MatrixEaseLicense.OverrideLicense();

        try
        {
            var command = CliParser.Parse(args);
            return await command.ExecuteAsync(Console.Out, Console.Error, CancellationToken.None);
        }
        catch (CliUsageException excp)
        {
            await Console.Error.WriteLineAsync(excp.Message);
            await Console.Error.WriteLineAsync();
            await Console.Error.WriteLineAsync(CliHelp.Text);
            return 2;
        }
        catch (Exception excp)
        {
            await Console.Error.WriteLineAsync(excp.ToString());
            return 1;
        }
    }
}
