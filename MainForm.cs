using Microsoft.Win32;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace CS2Checker;

public sealed class MainForm : Form
{
    private readonly Panel sidebar = new();
    private readonly Panel content = new();
    private readonly Button scanButton = new();
    private readonly Button reportButton = new();
    private readonly ProgressBar progress = new();
    private readonly Label status = new();
    private readonly Label resultTitle = new();
    private readonly Label resultText = new();
    private readonly RichTextBox log = new();
    private readonly Label processesValue = new();
    private readonly Label filesValue = new();
    private readonly Label startupValue = new();
    private readonly Label hashValue = new();

    private readonly List<string> reportLines = new();
    private string? lastReportPath;

    private static readonly string[] Keywords =
    {
        "aimbot", "wallhack", "triggerbot", "esp", "cheat",
        "injector", "loader", "skinchanger", "external", "internal"
    };

    private static readonly HashSet<string> KnownSha256 = new(StringComparer.OrdinalIgnoreCase);

    public MainForm()
    {
        Text = "CS2 Checker";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(950, 620);
        Size = new Size(1100, 720);
        BackColor = Color.FromArgb(11, 13, 17);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10F);

        BuildUi();
    }

    private void BuildUi()
    {
        sidebar.Dock = DockStyle.Left;
        sidebar.Width = 235;
        sidebar.BackColor = Color.FromArgb(17, 20, 26);
        Controls.Add(sidebar);

        var logo = new Label
        {
            Text = "CS2\\nCHECKER",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 20, FontStyle.Bold),
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Top,
            Height = 110
        };
        sidebar.Controls.Add(logo);

        var version = new Label
        {
            Text = "v2.0 • Windows",
            ForeColor = Color.FromArgb(140, 148, 160),
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Top,
            Height = 30
        };
        sidebar.Controls.Add(version);

        scanButton.Text = "▶  НАЧАТЬ ПРОВЕРКУ";
        scanButton.Dock = DockStyle.Top;
        scanButton.Height = 55;
        scanButton.Margin = new Padding(15);
        scanButton.FlatStyle = FlatStyle.Flat;
        scanButton.FlatAppearance.BorderSize = 0;
        scanButton.BackColor = Color.FromArgb(42, 128, 255);
        scanButton.ForeColor = Color.White;
        scanButton.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        scanButton.Click += async (_, _) => await RunScanAsync();
        sidebar.Controls.Add(scanButton);

        reportButton.Text = "▣  ОТКРЫТЬ ПОСЛЕДНИЙ ОТЧЁТ";
        reportButton.Dock = DockStyle.Top;
        reportButton.Height = 48;
        reportButton.FlatStyle = FlatStyle.Flat;
        reportButton.FlatAppearance.BorderSize = 0;
        reportButton.BackColor = Color.FromArgb(28, 33, 42);
        reportButton.ForeColor = Color.White;
        reportButton.Click += (_, _) =>
        {
            if (lastReportPath != null && File.Exists(lastReportPath))
                Process.Start(new ProcessStartInfo(lastReportPath) { UseShellExecute = true });
            else
                MessageBox.Show("Сначала выполните проверку.", "CS2 Checker",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
        };
        sidebar.Controls.Add(reportButton);

        var info = new Label
        {
            Text = "Проверка не вмешивается\\nв игру и не удаляет файлы.\\n\\nСовпадение — это повод\\nдля дополнительной проверки,\\nа не автоматическое доказательство чита.",
            ForeColor = Color.FromArgb(135, 143, 155),
            Dock = DockStyle.Bottom,
            Height = 170,
            Padding = new Padding(18),
            AutoSize = false
        };
        sidebar.Controls.Add(info);

        content.Dock = DockStyle.Fill;
        content.Padding = new Padding(28);
        Controls.Add(content);

        var header = new Label
        {
            Text = "Проверка компьютера",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 24, FontStyle.Bold),
            Dock = DockStyle.Top,
            Height = 55
        };
        content.Controls.Add(header);

        status.Text = "Готов к проверке";
        status.ForeColor = Color.FromArgb(150, 158, 170);
        status.Dock = DockStyle.Top;
        status.Height = 35;
        content.Controls.Add(status);

        progress.Dock = DockStyle.Top;
        progress.Height = 18;
        progress.Style = ProgressBarStyle.Continuous;
        content.Controls.Add(progress);

        var cards = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 115,
            ColumnCount = 4,
            RowCount = 1,
            Padding = new Padding(0, 18, 0, 0)
        };
        for (int i = 0; i < 4; i++) cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        content.Controls.Add(cards);

        cards.Controls.Add(Card("ПРОЦЕССЫ", processesValue), 0, 0);
        cards.Controls.Add(Card("ФАЙЛЫ", filesValue), 1, 0);
        cards.Controls.Add(Card("АВТОЗАПУСК", startupValue), 2, 0);
        cards.Controls.Add(Card("ХЭШИ", hashValue), 3, 0);

        resultTitle.Text = "Результат: ожидание проверки";
        resultTitle.ForeColor = Color.White;
        resultTitle.Font = new Font("Segoe UI", 15, FontStyle.Bold);
        resultTitle.Dock = DockStyle.Top;
        resultTitle.Height = 42;
        content.Controls.Add(resultTitle);

        resultText.Text = "Нажмите «Начать проверку».";
        resultText.ForeColor = Color.FromArgb(170, 178, 190);
        resultText.Dock = DockStyle.Top;
        resultText.Height = 40;
        content.Controls.Add(resultText);

        log.Dock = DockStyle.Fill;
        log.ReadOnly = true;
        log.BackColor = Color.FromArgb(17, 20, 26);
        log.ForeColor = Color.FromArgb(220, 224, 230);
        log.BorderStyle = BorderStyle.None;
        log.Font = new Font("Consolas", 9.5F);
        log.Margin = new Padding(0, 12, 0, 0);
        content.Controls.Add(log);
    }

    private Control Card(string title, Label value)
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(20, 24, 31), Margin = new Padding(5) };
        var t = new Label
        {
            Text = title,
            ForeColor = Color.FromArgb(125, 135, 150),
            Dock = DockStyle.Top,
            Height = 28,
            Padding = new Padding(12, 8, 0, 0),
            Font = new Font("Segoe UI", 8.5F, FontStyle.Bold)
        };
        value.Text = "—";
        value.ForeColor = Color.White;
        value.Font = new Font("Segoe UI", 18, FontStyle.Bold);
        value.Dock = DockStyle.Fill;
        value.Padding = new Padding(12, 0, 0, 0);
        panel.Controls.Add(value);
        panel.Controls.Add(t);
        return panel;
    }

    private async Task RunScanAsync()
    {
        scanButton.Enabled = false;
        reportButton.Enabled = false;
        progress.Value = 0;
        log.Clear();
        reportLines.Clear();

        void Write(string s)
        {
            log.AppendText(s + Environment.NewLine);
            reportLines.Add(s);
        }

        try
        {
            status.Text = "Выполняется полная проверка...";
            resultTitle.Text = "Результат: проверка выполняется";
            resultText.Text = "Анализируем систему и установку CS2...";

            Write("==================================================");
            Write("CS2 CHECKER v2.0");
            Write($"Дата: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Write($"Пользователь: {Environment.UserName}");
            Write("==================================================");
            Write("");

            progress.Value = 10;
            int suspiciousProcesses = ScanProcesses(Write);
            processesValue.Text = suspiciousProcesses.ToString();

            progress.Value = 35;
            int startup = ScanStartup(Write);
            startupValue.Text = startup.ToString();

            progress.Value = 55;
            string? cs2 = FindCs2();
            int suspiciousFiles = 0;
            if (cs2 != null)
            {
                Write($"[CS2] {cs2}");
                suspiciousFiles = ScanFiles(cs2, Write);
            }
            else
            {
                Write("[ИНФО] CS2 автоматически не найдена.");
            }
            filesValue.Text = suspiciousFiles.ToString();

            progress.Value = 75;
            int hashMatches = await ScanHashesAsync(cs2, Write);
            hashValue.Text = hashMatches.ToString();

            progress.Value = 100;
            Write("");
            Write("Проверка завершена.");
            Write("Важно: подозрительное совпадение не является доказательством чита.");

            resultTitle.Text = suspiciousProcesses + suspiciousFiles + hashMatches + startup > 0
                ? "Результат: обнаружены совпадения для проверки"
                : "Результат: подозрительных совпадений не найдено";
            resultText.Text = "Проверьте подробности в журнале и сохранённом отчёте.";
            status.Text = "Проверка завершена";

            lastReportPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                $"CS2_Check_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
            await File.WriteAllTextAsync(lastReportPath, string.Join(Environment.NewLine, reportLines), Encoding.UTF8);
            Write($"Отчёт: {lastReportPath}");
        }
        catch (Exception ex)
        {
            Write($"[ОШИБКА] {ex.Message}");
            status.Text = "Проверка завершилась с ошибкой";
        }
        finally
        {
            scanButton.Enabled = true;
            reportButton.Enabled = true;
        }
    }

    private static int ScanProcesses(Action<string> write)
    {
        int found = 0;
        write("1. ПРОЦЕССЫ");
        foreach (var p in Process.GetProcesses().OrderBy(x => x.ProcessName))
        {
            try
            {
                if (Keywords.Any(k => p.ProcessName.Contains(k, StringComparison.OrdinalIgnoreCase)))
                {
                    found++;
                    write($"[ПОДОЗРИТЕЛЬНО] {p.ProcessName} | PID {p.Id}");
                }
            }
            catch { }
        }
        write($"Найдено совпадений: {found}");
        write("");
        return found;
    }

    private static int ScanStartup(Action<string> write)
    {
        int found = 0;
        write("2. АВТОЗАПУСК");

        found += CheckStartup(Registry.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Run", write);
        found += CheckStartup(Registry.LocalMachine, "Software\\Microsoft\\Windows\\CurrentVersion\\Run", write);

        write($"Найдено совпадений: {found}");
        write("");
        return found;
    }

    private static int CheckStartup(RegistryKey root, string subkey, Action<string> write)
    {
        int found = 0;
        try
        {
            using var key = root.OpenSubKey(subkey);
            if (key == null) return 0;

            foreach (var name in key.GetValueNames())
            {
                var value = key.GetValue(name)?.ToString() ?? "";
                if (Keywords.Any(k => name.Contains(k, StringComparison.OrdinalIgnoreCase) ||
                                      value.Contains(k, StringComparison.OrdinalIgnoreCase)))
                {
                    found++;
                    write($"[ПОДОЗРИТЕЛЬНО] {name} = {value}");
                }
            }
        }
        catch { }
        return found;
    }

    private static string? FindCs2()
    {
        var roots = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
        };

        foreach (var root in roots.Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            var p = Path.Combine(root, "Steam", "steamapps", "common", "Counter-Strike Global Offensive");
            if (Directory.Exists(p)) return p;
        }

        return null;
    }

    private static int ScanFiles(string root, Action<string> write)
    {
        int found = 0;
        write("3. ФАЙЛЫ CS2");

        try
        {
            foreach (var file in Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories).Take(5000))
            {
                var name = Path.GetFileName(file);
                if (Keywords.Any(k => name.Contains(k, StringComparison.OrdinalIgnoreCase)))
                {
                    found++;
                    write($"[ПОДОЗРИТЕЛЬНОЕ ИМЯ] {file}");
                }
            }
        }
        catch (UnauthorizedAccessException)
        {
            write("[ИНФО] Часть папок недоступна.");
        }

        write($"Найдено совпадений: {found}");
        write("");
        return found;
    }

    private static async Task<int> ScanHashesAsync(string? root, Action<string> write)
    {
        int found = 0;
        write("4. SHA-256");

        if (root == null)
        {
            write("[ИНФО] Проверка хэшей пропущена.");
            return 0;
        }

        foreach (var file in Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
            .Where(f => f.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ||
                        f.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            .Take(250))
        {
            try
            {
                await using var stream = File.OpenRead(file);
                using var sha = SHA256.Create();
                var hash = Convert.ToHexString(await sha.ComputeHashAsync(stream));

                if (KnownSha256.Contains(hash))
                {
                    found++;
                    write($"[СОВПАДЕНИЕ ХЭША] {file} | {hash}");
                }
            }
            catch { }
        }

        write($"Совпадений хэшей: {found}");
        write("");
        return found;
    }
}
