using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace EmberfallInstaller
{
    internal static class Program
    {
        internal const string Executable = "Emberfall.exe";
        [STAThread]
        private static int Main(string[] args)
        {
            if (args.Length == 1 && args[0] == "--verify-payload")
            {
                try
                {
                    using (Stream stream = Payload())
                    using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read)) Validator.Validate(archive, null);
                    return 0;
                }
                catch (Exception error) { Console.Error.WriteLine(error.Message); return 1; }
            }
            if (args.Length == 2 && args[0] == "--install-dir")
            {
                try
                {
                    if (!Environment.Is64BitOperatingSystem) throw new IOException("该游戏需要 64 位 Windows 10 或 Windows 11。");
                    string destination = Validator.Destination(args[1]);
                    string warning = InstallEngine.Install(destination, null);
                    Console.WriteLine("Installed: " + destination);
                    if (!string.IsNullOrEmpty(warning)) Console.Error.WriteLine(warning);
                    return 0;
                }
                catch (Exception error) { Console.Error.WriteLine(error.Message); return 1; }
            }
            if (args.Length != 0)
            {
                Console.Error.WriteLine("Usage: Emberfall-Setup.exe [--verify-payload | --install-dir <absolute local directory>]");
                return 2;
            }
            if (!Environment.Is64BitOperatingSystem)
            {
                MessageBox.Show("该游戏需要 64 位 Windows 10 或 Windows 11。", "无法安装", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return 1;
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new InstallerWindow());
            return 0;
        }
        internal static Stream Payload()
        {
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Emberfall.Payload.zip");
            if (stream == null) throw new InvalidDataException("安装包缺少游戏文件，请重新获取安装器。");
            return stream;
        }
    }

    internal static class Validator
    {
        internal static bool DirectoryEntry(ZipArchiveEntry entry)
        {
            return entry.FullName.EndsWith("/", StringComparison.Ordinal) || entry.FullName.EndsWith("\\", StringComparison.Ordinal);
        }
        internal static string RelativePath(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw) || Path.IsPathRooted(raw) || raw.IndexOf(':') >= 0) throw new InvalidDataException("安装包包含不安全的绝对路径。");
            string[] parts = raw.Replace('\\', '/').TrimEnd('/').Split('/');
            foreach (string part in parts)
            {
                if (string.IsNullOrWhiteSpace(part) || part == "." || part == ".." || part.EndsWith(".", StringComparison.Ordinal) || part.EndsWith(" ", StringComparison.Ordinal) || part.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                    throw new InvalidDataException("安装包包含不安全的文件路径。");
                string stem = part.Split('.')[0].ToUpperInvariant();
                if (stem == "CON" || stem == "PRN" || stem == "AUX" || stem == "NUL" || (stem.Length == 4 && (stem.StartsWith("COM", StringComparison.Ordinal) || stem.StartsWith("LPT", StringComparison.Ordinal)) && stem[3] >= '1' && stem[3] <= '9'))
                    throw new InvalidDataException("安装包包含 Windows 保留文件名。");
            }
            return string.Join(Path.DirectorySeparatorChar.ToString(), parts);
        }
        internal static string Target(string destination, string relative)
        {
            string root = Path.GetFullPath(destination).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string target = Path.GetFullPath(Path.Combine(root, relative));
            if (!target.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("文件路径超出所选安装目录。");
            return target;
        }
        internal static void NoReparsePoints(string path)
        {
            string current = Path.GetFullPath(path);
            while (!string.IsNullOrEmpty(current))
            {
                if ((File.Exists(current) || Directory.Exists(current)) && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                    throw new IOException("安装路径不能包含符号链接或目录联接，请选择普通文件夹。");
                string parent = Path.GetDirectoryName(current);
                if (string.Equals(parent, current, StringComparison.OrdinalIgnoreCase)) break;
                current = parent;
            }
        }
        internal static string Destination(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) throw new IOException("请选择安装目录。");
            string requested = input.Trim();
            if (!Path.IsPathRooted(requested) || requested.Length < 3 || requested[1] != ':' || (requested[2] != Path.DirectorySeparatorChar && requested[2] != Path.AltDirectorySeparatorChar))
                throw new IOException("请选择本机磁盘上的绝对安装路径，例如 E:\\EMBERFALL。");
            string destination = Path.GetFullPath(requested).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string root = Path.GetPathRoot(destination).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (string.Equals(destination, root, StringComparison.OrdinalIgnoreCase)) throw new IOException("请选择专用游戏文件夹，不要安装到磁盘根目录。");
            if (File.Exists(destination)) throw new IOException("安装目录已被同名文件占用。");
            string saves = Path.GetFullPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "LocalLow", "EmberfallStudio", "Emberfall"));
            if (string.Equals(destination, saves, StringComparison.OrdinalIgnoreCase) || destination.StartsWith(saves + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) || saves.StartsWith(destination + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new IOException("该目录保存个人冒险进度，请选择其他安装目录。");
            NoReparsePoints(destination);
            return destination;
        }
        internal static List<ZipArchiveEntry> Validate(ZipArchive archive, Action<int, string> progress)
        {
            var entries = new List<ZipArchiveEntry>();
            var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            bool game = false, engine = false, data = false;
            long total = 0;
            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                string key = RelativePath(entry.FullName);
                if (!paths.Add(key)) throw new InvalidDataException("安装包包含重复文件：" + entry.FullName);
                if (!DirectoryEntry(entry)) files.Add(key);
                if (((entry.ExternalAttributes >> 16) & 0xF000) == 0xA000 || (entry.ExternalAttributes & (int)FileAttributes.ReparsePoint) != 0)
                    throw new InvalidDataException("安装包不能包含符号链接。");
                if (entry.Length < 0 || entry.Length > 8L * 1024 * 1024 * 1024) throw new InvalidDataException("安装包文件长度无效。");
                total = checked(total + entry.Length);
                if (total > 24L * 1024 * 1024 * 1024) throw new InvalidDataException("安装包超出允许大小。");
                if (!DirectoryEntry(entry) && string.Equals(key, Program.Executable, StringComparison.OrdinalIgnoreCase)) game = entry.Length > 0;
                if (!DirectoryEntry(entry) && string.Equals(key, "UnityPlayer.dll", StringComparison.OrdinalIgnoreCase)) engine = entry.Length > 0;
                if (!DirectoryEntry(entry) && key.StartsWith("Emberfall_Data" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) data = true;
                if (Path.GetFileName(key).StartsWith("emberfall-save", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("发行包不应包含个人存档。");
                entries.Add(entry);
            }
            if (!game || !engine || !data) throw new InvalidDataException("安装包不完整：缺少 Emberfall.exe、UnityPlayer.dll 或 Emberfall_Data。");
            foreach (string key in paths)
            {
                string parent = Path.GetDirectoryName(key);
                while (!string.IsNullOrEmpty(parent))
                {
                    if (files.Contains(parent)) throw new InvalidDataException("安装包的文件与文件夹路径冲突。");
                    parent = Path.GetDirectoryName(parent);
                }
            }
            byte[] buffer = new byte[128 * 1024];
            for (int i = 0; i < entries.Count; i++)
            {
                ZipArchiveEntry entry = entries[i];
                if (!DirectoryEntry(entry))
                {
                    long actual = 0;
                    using (Stream input = entry.Open())
                    {
                        int count;
                        while ((count = input.Read(buffer, 0, buffer.Length)) > 0) actual = checked(actual + count);
                    }
                    if (actual != entry.Length) throw new InvalidDataException("安装文件损坏：" + entry.FullName);
                }
                if (progress != null) progress((i + 1) * 100 / Math.Max(1, entries.Count), "正在检查游戏文件…");
            }
            return entries;
        }
    }

    internal static class InstallEngine
    {
        private sealed class ChangedFile
        {
            internal string Target, Backup;
            internal bool Installed;
        }

        internal static void GameClosed()
        {
            Process[] games = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(Program.Executable));
            try { if (games.Length != 0) throw new IOException("请先关闭所有正在运行的星烬纪元窗口，再进行安装或更新；安装器不会自动终止游戏。"); }
            finally { foreach (Process process in games) process.Dispose(); }
        }

        internal static string Install(string input, Action<int, string> progress)
        {
            using (var gate = new Mutex(false, "Local\\EmberfallInstaller"))
            {
                bool acquired = false;
                try
                {
                    try { acquired = gate.WaitOne(0); }
                    catch (AbandonedMutexException) { acquired = true; }
                    if (!acquired) throw new IOException("另一个星烬纪元安装程序正在运行，请等待它完成。");
                    return InstallFiles(input, progress);
                }
                finally { if (acquired) gate.ReleaseMutex(); }
            }
        }

        private static string InstallFiles(string input, Action<int, string> progress)
        {
            string destination = Validator.Destination(input);
            GameClosed();
            // A non-empty unrelated directory is never treated as an installation.
            if (Directory.Exists(destination))
            {
                bool nonempty;
                using (IEnumerator<string> files = Directory.EnumerateFileSystemEntries(destination).GetEnumerator()) nonempty = files.MoveNext();
                if (nonempty && (!File.Exists(Path.Combine(destination, Program.Executable)) || !File.Exists(Path.Combine(destination, "UnityPlayer.dll")) || !Directory.Exists(Path.Combine(destination, "Emberfall_Data"))))
                    throw new IOException("所选目录不是完整的星烬纪元安装目录。请使用空文件夹，或选择已有游戏的安装目录。");
            }
            string parent = Path.GetDirectoryName(destination);
            string transaction = Path.Combine(parent, ".Emberfall-install-" + Guid.NewGuid().ToString("N"));
            string staged = Path.Combine(transaction, "staged");
            string backup = Path.Combine(transaction, "backup");
            var changed = new List<ChangedFile>();
            var createdDirectories = new List<string>();
            try
            {
                using (Stream payload = Program.Payload())
                using (ZipArchive archive = new ZipArchive(payload, ZipArchiveMode.Read))
                {
                    List<ZipArchiveEntry> entries = Validator.Validate(archive, delegate(int percent, string message) { Report(progress, percent / 5, message); });
                    foreach (ZipArchiveEntry entry in entries)
                    {
                        string target = Validator.Target(destination, Validator.RelativePath(entry.FullName));
                        Validator.NoReparsePoints(target);
                        if (Validator.DirectoryEntry(entry) ? File.Exists(target) : Directory.Exists(target))
                            throw new IOException("安装目标的文件与文件夹冲突：" + target);
                        if (!Validator.DirectoryEntry(entry) && File.Exists(target))
                        {
                            if ((File.GetAttributes(target) & FileAttributes.ReadOnly) != 0) throw new IOException("游戏文件为只读，无法更新：" + target);
                            using (var writable = new FileStream(target, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
                        }
                    }
                    CreateDirectories(parent, createdDirectories);
                    if (Directory.Exists(transaction) || File.Exists(transaction)) throw new IOException("安装临时目录已被占用，请重试。");
                    Directory.CreateDirectory(staged);
                    Directory.CreateDirectory(backup);
                    byte[] buffer = new byte[128 * 1024];
                    for (int i = 0; i < entries.Count; i++)
                    {
                        ZipArchiveEntry entry = entries[i];
                        string target = Validator.Target(staged, Validator.RelativePath(entry.FullName));
                        if (Validator.DirectoryEntry(entry)) Directory.CreateDirectory(target);
                        else
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(target));
                            using (Stream source = entry.Open())
                            using (var output = new FileStream(target, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                            {
                                int count;
                                while ((count = source.Read(buffer, 0, buffer.Length)) > 0) output.Write(buffer, 0, count);
                                output.Flush(true);
                                if (output.Length != entry.Length) throw new InvalidDataException("临时游戏文件长度校验失败：" + entry.FullName);
                            }
                        }
                        Report(progress, 20 + (i + 1) * 45 / entries.Count, "正在准备：" + entry.FullName);
                    }
                    // Existing files remain untouched until the complete archive is staged.
                    GameClosed();
                    Validator.Destination(destination);
                    CreateDirectories(destination, createdDirectories);
                    for (int i = 0; i < entries.Count; i++)
                    {
                        GameClosed();
                        ZipArchiveEntry entry = entries[i];
                        string relative = Validator.RelativePath(entry.FullName);
                        string target = Validator.Target(destination, relative);
                        Validator.NoReparsePoints(target);
                        if (Validator.DirectoryEntry(entry)) CreateDirectories(target, createdDirectories);
                        else
                        {
                            CreateDirectories(Path.GetDirectoryName(target), createdDirectories);
                            var change = new ChangedFile { Target = target };
                            if (File.Exists(target))
                            {
                                string saved = Validator.Target(backup, relative);
                                Directory.CreateDirectory(Path.GetDirectoryName(saved));
                                File.Move(target, saved);
                                change.Backup = saved;
                            }
                            changed.Add(change);
                            File.Move(Validator.Target(staged, relative), target);
                            change.Installed = true;
                        }
                        Report(progress, 65 + (i + 1) * 34 / entries.Count, "正在安装：" + entry.FullName);
                    }
                }
            }
            catch (Exception error)
            {
                string rollbackError = Rollback(changed, createdDirectories);
                string cleanupError = "";
                if (string.IsNullOrEmpty(rollbackError))
                {
                    try { DeleteTransaction(transaction, parent); }
                    catch (Exception cleanup) { cleanupError = " 临时目录保留在 " + transaction + "：" + cleanup.Message; }
                }
                throw new IOException("安装未完成。" + (string.IsNullOrEmpty(rollbackError) ? "本次游戏文件变更已回滚。" : "部分文件自动回滚失败；请保留备份目录 " + backup + "。" + rollbackError) + "\n" + error.Message + cleanupError, error);
            }
            try { DeleteTransaction(transaction, parent); }
            catch (Exception error) { return "游戏已安装成功，但旧文件备份尚未清理：" + transaction + "。" + error.Message; }
            Report(progress, 100, "安装完成");
            return "";
        }

        private static void CreateDirectories(string path, List<string> created)
        {
            Validator.NoReparsePoints(path);
            if (Directory.Exists(path)) return;
            string parent = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(parent)) CreateDirectories(parent, created);
            Directory.CreateDirectory(path);
            created.Add(path);
        }

        private static string Rollback(List<ChangedFile> changed, List<string> createdDirectories)
        {
            string errors = "";
            for (int i = changed.Count - 1; i >= 0; i--)
            {
                ChangedFile file = changed[i];
                try
                {
                    Validator.NoReparsePoints(file.Target);
                    if (file.Installed && File.Exists(file.Target)) File.Delete(file.Target);
                    if (file.Backup != null)
                    {
                        Validator.NoReparsePoints(file.Backup);
                        File.Move(file.Backup, file.Target);
                    }
                }
                catch (Exception error) { errors += "\n" + file.Target + "：" + error.Message; }
            }
            // Remove only empty folders created by this attempt; unrelated files survive.
            for (int i = createdDirectories.Count - 1; i >= 0; i--)
            {
                try
                {
                    Validator.NoReparsePoints(createdDirectories[i]);
                    if (Directory.Exists(createdDirectories[i]) && Directory.GetFileSystemEntries(createdDirectories[i]).Length == 0) Directory.Delete(createdDirectories[i], false);
                }
                catch { /* An empty directory cleanup failure does not lose game files. */ }
            }
            return errors;
        }

        private static void DeleteTransaction(string path, string parent)
        {
            string absolute = Path.GetFullPath(path);
            string parentPrefix = Path.GetFullPath(parent).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!absolute.StartsWith(parentPrefix, StringComparison.OrdinalIgnoreCase) || !Path.GetFileName(absolute).StartsWith(".Emberfall-install-", StringComparison.Ordinal))
                throw new IOException("拒绝清理安装临时目录之外的路径。");
            if (!Directory.Exists(absolute)) return;
            var pending = new Queue<string>();
            pending.Enqueue(absolute);
            while (pending.Count > 0)
            {
                string current = pending.Dequeue();
                Validator.NoReparsePoints(current);
                foreach (string entry in Directory.GetFileSystemEntries(current))
                {
                    Validator.NoReparsePoints(entry);
                    if (Directory.Exists(entry)) pending.Enqueue(entry);
                }
            }
            Directory.Delete(absolute, true);
        }

        private static void Report(Action<int, string> progress, int percent, string message)
        {
            if (progress != null) progress(percent, message);
        }
    }

    internal sealed class InstallerWindow : Form
    {
        private readonly TextBox destinationBox;
        private readonly Button browseButton, installButton, launchButton, closeButton;
        private readonly CheckBox shortcutBox;
        private readonly ProgressBar progressBar;
        private readonly Label status;
        private readonly BackgroundWorker worker;
        private bool installing;
        private string installedDirectory;

        internal InstallerWindow()
        {
            Text = "星烬纪元 · 安装程序";
            ClientSize = new Size(650, 452);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Microsoft YaHei UI", 9F);
            BackColor = Color.FromArgb(20, 29, 43);
            ForeColor = Color.FromArgb(226, 235, 245);
            AddLabel("EMBERFALL", 28, 22, 550, 40, 23F, Color.FromArgb(246, 202, 125));
            AddLabel("星烬纪元 · Windows 桌面版", 30, 67, 570, 27, 12F, ForeColor);
            AddLabel("安装游戏后即可离线游玩，无需安装 Unity 编辑器。", 30, 106, 580, 25, 9F, Color.LightSteelBlue);
            AddLabel("安装位置", 30, 148, 500, 23, 10F, ForeColor);
            destinationBox = new TextBox { Left = 30, Top = 178, Width = 478, Height = 28, Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Emberfall") };
            Controls.Add(destinationBox);
            browseButton = AddButton("浏览…", 520, 176, 100, 31);
            browseButton.Click += Browse;
            shortcutBox = new CheckBox { Left = 30, Top = 221, Width = 400, Height = 25, Text = "创建桌面快捷方式", Checked = true, ForeColor = ForeColor };
            Controls.Add(shortcutBox);
            AddLabel("存档与游戏文件分开保存。更新安装只覆盖游戏文件，\n不会附带、覆盖或删除个人存档。安装前请先关闭正在运行的游戏。", 30, 258, 590, 50, 9F, Color.LightSteelBlue);
            progressBar = new ProgressBar { Left = 30, Top = 319, Width = 590, Height = 14 };
            Controls.Add(progressBar);
            status = AddLabel("准备就绪 · 默认位置无需管理员权限", 30, 341, 590, 28, 9F, Color.FromArgb(154, 217, 199));
            installButton = AddButton("安装游戏", 280, 391, 110, 34);
            installButton.Click += BeginInstall;
            launchButton = AddButton("启动游戏", 395, 391, 110, 34);
            launchButton.Enabled = false;
            launchButton.Click += Launch;
            closeButton = AddButton("关闭", 510, 391, 110, 34);
            closeButton.Click += delegate { Close(); };
            FormClosing += delegate(object sender, FormClosingEventArgs e) { if (installing) e.Cancel = true; };
            worker = new BackgroundWorker { WorkerReportsProgress = true };
            worker.DoWork += Install;
            worker.ProgressChanged += delegate(object sender, ProgressChangedEventArgs e) { progressBar.Value = Math.Max(0, Math.Min(100, e.ProgressPercentage)); status.Text = Convert.ToString(e.UserState); };
            worker.RunWorkerCompleted += Finished;
        }
        private Label AddLabel(string text, int x, int y, int width, int height, float size, Color color)
        {
            var label = new Label { Text = text, Left = x, Top = y, Width = width, Height = height, ForeColor = color, Font = new Font("Microsoft YaHei UI", size) };
            Controls.Add(label);
            return label;
        }
        private Button AddButton(string text, int x, int y, int width, int height)
        {
            var button = new Button { Text = text, Left = x, Top = y, Width = width, Height = height, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(39, 61, 79), ForeColor = Color.FromArgb(230, 239, 247) };
            button.FlatAppearance.BorderColor = Color.FromArgb(72, 109, 128);
            Controls.Add(button);
            return button;
        }
        private void Browse(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog { Description = "选择星烬纪元的安装文件夹", ShowNewFolderButton = true, SelectedPath = destinationBox.Text })
                if (dialog.ShowDialog(this) == DialogResult.OK) destinationBox.Text = dialog.SelectedPath;
        }
        private void BeginInstall(object sender, EventArgs e)
        {
            try
            {
                string destination = Validator.Destination(destinationBox.Text);
                InstallEngine.GameClosed();
                installing = true;
                Interactive(false);
                status.Text = "正在检查安装包…";
                worker.RunWorkerAsync(new InstallRequest { Destination = destination, Shortcut = shortcutBox.Checked });
            }
            catch (Exception error)
            {
                installing = false;
                Interactive(true);
                MessageBox.Show(this, error.Message, "无法开始安装", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        private void Install(object sender, DoWorkEventArgs e)
        {
            InstallRequest request = (InstallRequest)e.Argument;
            request.Warning = InstallEngine.Install(request.Destination, delegate(int percent, string message) { worker.ReportProgress(percent, message); });
            e.Result = request;
        }
        private void Finished(object sender, RunWorkerCompletedEventArgs e)
        {
            installing = false;
            Interactive(true);
            if (e.Error != null)
            {
                status.Text = "安装未完成，可以修正问题后重新安装。";
                MessageBox.Show(this, e.Error.Message + "\n\n个人存档未作更改。若提示保留备份，请保留该目录。", "安装未完成", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            InstallRequest result = (InstallRequest)e.Result;
            installedDirectory = result.Destination;
            if (!string.IsNullOrEmpty(result.Warning)) MessageBox.Show(this, result.Warning, "安装完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
            if (result.Shortcut)
            {
                try { DesktopShortcut(installedDirectory); }
                catch (Exception error) { MessageBox.Show(this, "游戏安装成功，但快捷方式未能创建。请从安装目录运行 Emberfall.exe。\n\n" + error.Message, "安装完成", MessageBoxButtons.OK, MessageBoxIcon.Information); }
            }
            progressBar.Value = 100;
            launchButton.Enabled = true;
            installButton.Enabled = false;
            status.Text = "安装完成 · 点击“启动游戏”开始冒险";
        }
        private static void DesktopShortcut(string directory)
        {
            Type shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType == null) throw new InvalidOperationException("系统未提供快捷方式组件。");
            object shell = null, shortcut = null;
            try
            {
                shell = Activator.CreateInstance(shellType);
                string link = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "星烬纪元.lnk");
                shortcut = shellType.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shell, new object[] { link });
                Type type = shortcut.GetType();
                type.InvokeMember("TargetPath", BindingFlags.SetProperty, null, shortcut, new object[] { Path.Combine(directory, Program.Executable) });
                type.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, shortcut, new object[] { directory });
                type.InvokeMember("Description", BindingFlags.SetProperty, null, shortcut, new object[] { "星烬纪元 · 3D 即时战斗角色扮演游戏" });
                type.InvokeMember("IconLocation", BindingFlags.SetProperty, null, shortcut, new object[] { Path.Combine(directory, Program.Executable) + ",0" });
                type.InvokeMember("Save", BindingFlags.InvokeMethod, null, shortcut, null);
            }
            finally
            {
                if (shortcut != null && Marshal.IsComObject(shortcut)) Marshal.FinalReleaseComObject(shortcut);
                if (shell != null && Marshal.IsComObject(shell)) Marshal.FinalReleaseComObject(shell);
            }
        }
        private void Launch(object sender, EventArgs e)
        {
            try { Process.Start(new ProcessStartInfo(Path.Combine(installedDirectory, Program.Executable)) { WorkingDirectory = installedDirectory, UseShellExecute = true }); Close(); }
            catch (Exception error) { MessageBox.Show(this, error.Message, "无法启动游戏", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        }
        private void Interactive(bool enabled)
        {
            destinationBox.Enabled = browseButton.Enabled = shortcutBox.Enabled = installButton.Enabled = closeButton.Enabled = enabled;
            launchButton.Enabled = enabled && installedDirectory != null;
        }
        private sealed class InstallRequest { internal string Destination; internal bool Shortcut; internal string Warning; }
    }
}
