namespace MatrixEase.Tester;

public static class CliHelp
{
    public const string Text =
@"MatrixEase Tester CLI

Usage:
  matrixease-tester spec init --spec <file>
  matrixease-tester spec list --spec <file>
  matrixease-tester spec add --spec <file> --type <csv|excel|google> --name <name> [options]
  matrixease-tester run --spec <file> --output <dir> [--only <name1,name2>] [--max-rows <n>]
  matrixease-tester baseline --spec <file> --output <dir> [--only <name1,name2>] [--max-rows <n>]
  matrixease-tester crypto roundtrip --user-folder <id> --manga-guid <guid>

Spec add options:
  CSV/Excel:
    --path <file>
    --separator <char>         CSV only, default ','
  Google:
    --sheet-id <id>
    --range <a1-range>
    --google-client-id <id>
    --google-client-secret <secret>

Common options:
  --header-row <n>             default 1
  --header-rows <n>            default 1
  --ignore-cols <csv>
  --include-disabled           run disabled tests too
  --force                      overwrite on spec init

JSON spec:
  The CLI stores tests in a JSON file with explicit named fields and defaults.
  Use 'spec init' to create a template.";
}
