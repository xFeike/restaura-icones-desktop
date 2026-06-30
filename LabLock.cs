using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

namespace LabLock
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            bool createdNew;
            using (var mutex = new System.Threading.Mutex(true,
                "LabLock_{B1E2F3A4-5C6D-7E8F-9A0B-1C2D3E4F5A6B}", out createdNew))
            {
                if (!createdNew)
                {
                    MessageBox.Show("LabLock ja esta em execucao.", "LabLock",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                using (var p = new DesktopProtector())
                {
                    p.LoadSettings();

                    if (p.AutoRestoreLayout)
                        p.RestoreLayout(false);

                    using (var form = new SettingsForm(p))
                    {
                        form.ShowFromTray();
                        Application.Run(form);
                    }
                }
            }
        }
    }

    class SettingsForm : Form
    {
        private DesktopProtector p;
        private NotifyIcon trayIcon;
        private CheckBox chkAutoRestore;
        private CheckBox chkAutoStart;
        private Button btnSave;
        private Button btnRestore;
        private Button btnDesinstalar;
        private bool reallyClosing;
        private Label lblStatus;
        private Label lblIconCount;

        public SettingsForm(DesktopProtector p)
        {
            this.p = p;

            Text = "LabLock - Gerenciamento do Laboratorio";
            Size = new Size(440, 400);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ShowInTaskbar = false;
            ShowIcon = true;
            Icon = SystemIcons.Shield;
            BackColor = Color.FromArgb(245, 245, 245);

            var boldFont = new Font("Segoe UI", 9, FontStyle.Bold);
            var normalFont = new Font("Segoe UI", 9);

            int y = 15;

            var lbl2 = new Label
            {
                Text = "Layout dos Icones da Area de Trabalho",
                Location = new Point(20, y),
                Size = new Size(380, 18),
                Font = boldFont
            };

            y += 22;
            btnSave = new Button
            {
                Text = "Salvar layout atual",
                Location = new Point(20, y),
                Size = new Size(180, 30),
                Font = normalFont,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 123, 255),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;

            btnRestore = new Button
            {
                Text = "Restaurar tudo agora",
                Location = new Point(210, y),
                Size = new Size(190, 30),
                Font = normalFont,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnRestore.FlatAppearance.BorderSize = 0;
            btnRestore.Click += BtnRestore_Click;

            y += 36;
            string last = p.GetLastSaveTime();
            lblStatus = new Label
            {
                Text = !string.IsNullOrEmpty(last)
                    ? "Ultimo backup: " + last
                    : "Nenhum backup salvo ainda.",
                Location = new Point(20, y),
                Size = new Size(380, 18),
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.Gray
            };

            y += 24;

            var lbl3 = new Label
            {
                Text = "Inicializacao",
                Location = new Point(20, y),
                Size = new Size(380, 18),
                Font = boldFont
            };

            y += 22;
            chkAutoStart = new CheckBox
            {
                Text = "Iniciar junto com o Windows",
                Location = new Point(20, y),
                Size = new Size(380, 26),
                Font = normalFont,
                Checked = p.AutoStart
            };
            chkAutoStart.CheckedChanged += (s, e) =>
                p.AutoStart = chkAutoStart.Checked;

            y += 30;
            chkAutoRestore = new CheckBox
            {
                Text = "Restaurar backup na inicializacao",
                Location = new Point(20, y),
                Size = new Size(380, 26),
                Font = normalFont,
                Checked = p.AutoRestoreLayout
            };
            chkAutoRestore.CheckedChanged += (s, e) =>
                p.AutoRestoreLayout = chkAutoRestore.Checked;

            y += 30;

            var lbl1 = new Label
            {
                Text = "Protecoes",
                Location = new Point(20, y),
                Size = new Size(380, 18),
                Font = boldFont
            };

            y += 22;
            var chkBlock = new CheckBox
            {
                Text = "Bloquear Ctrl+Scroll na area de trabalho",
                Location = new Point(20, y),
                Size = new Size(380, 26),
                Font = normalFont,
                Checked = p.BlockScroll
            };
            chkBlock.CheckedChanged += (s, e) =>
                p.BlockScroll = chkBlock.Checked;

            y += 30;
            var btnOrder = new Button
            {
                Text = "Ordenar icones que serao restaurados...",
                Location = new Point(20, y),
                Size = new Size(380, 28),
                Font = normalFont,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter
            };
            btnOrder.FlatAppearance.BorderSize = 0;
            btnOrder.Click += BtnOrder_Click;

            y += 34;
            lblIconCount = new Label
            {
                Text = "0 icones no backupdesktop",
                Location = new Point(20, y),
                Size = new Size(380, 16),
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.Gray
            };
            RefreshIconCount();

            y += 22;

            btnDesinstalar = new Button
            {
                Text = "Desinstalar",
                Location = new Point(ClientSize.Width - 110, ClientSize.Height - 40),
                Size = new Size(90, 26),
                Font = new Font("Segoe UI", 8),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnDesinstalar.FlatAppearance.BorderSize = 0;
            btnDesinstalar.Click += BtnDesinstalar_Click;
            btnDesinstalar.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            Controls.AddRange(new Control[] {
                lbl2, btnSave, btnRestore, lblStatus,
                lbl3, chkAutoStart, chkAutoRestore,
                lbl1, chkBlock, btnOrder, lblIconCount,
                btnDesinstalar
            });

            trayIcon = new NotifyIcon
            {
                Icon = SystemIcons.Shield,
                Text = "LabLock - Gerenciamento do Laboratorio",
                Visible = true
            };
            trayIcon.DoubleClick += (s, e) => ShowFromTray();

            var menu = new ContextMenuStrip();
            menu.Items.Add("Configuracoes", null, (s, e) => ShowFromTray());
            menu.Items.Add("Restaurar tudo agora", null, (s, e) => DoRestore());
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Sair", null, (s, e) => ExitApp(false));
            trayIcon.ContextMenuStrip = menu;
        }

        public void ShowFromTray()
        {
            if (WindowState == FormWindowState.Minimized)
                WindowState = FormWindowState.Normal;
            Show();
            Activate();
            BringToFront();
        }

        private void SetStatus(string text, bool isError)
        {
            lblStatus.Text = text;
            lblStatus.ForeColor = isError ? Color.Red : Color.Gray;
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (p.SaveLayout())
            {
                if (!chkAutoRestore.Checked)
                {
                    chkAutoRestore.Checked = true;
                    p.AutoRestoreLayout = true;
                }
                SetStatus("Backup salvo com sucesso!", false);
            }
            else
                SetStatus("Erro ao salvar backup.", true);
            RefreshLastSaveTime();
        }

        private void BtnRestore_Click(object sender, EventArgs e)
        {
            DoRestore();
        }

        private void DoRestore()
        {
            if (p.RestoreLayout(true))
            {
                SetStatus("Sistema restaurado com sucesso!", false);
                RefreshLastSaveTime();
            }
            else
                SetStatus("Falha ao restaurar. Nenhum backup salvo?", true);
        }

        private void BtnOrder_Click(object sender, EventArgs e)
        {
            string[] icons = p.GetOrderedIcons();
            if (icons.Length == 0)
            {
                MessageBox.Show("Nenhum icone encontrado em:\n" +
                    p.GetBackupDir() + "\n\n" +
                    "Coloque arquivos manualmente na pasta 'backupdesktop'.",
                    "Ordenar icones", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var dlg = new Form())
            {
                dlg.Text = "Ordenar icones para restauracao";
                dlg.Size = new Size(340, 420);
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.ShowInTaskbar = false;
                dlg.Icon = SystemIcons.Shield;

                var lb = new ListBox
                {
                    Location = new Point(10, 10),
                    Size = new Size(200, 340),
                    SelectionMode = SelectionMode.One
                };
                foreach (string f in icons)
                    lb.Items.Add(Path.GetFileName(f));
                if (lb.Items.Count > 0) lb.SelectedIndex = 0;

                var btnUp = new Button
                {
                    Text = "/\\",
                    Location = new Point(220, 10),
                    Size = new Size(40, 30)
                };
                btnUp.Click += (se, ev) =>
                {
                    int i = lb.SelectedIndex;
                    if (i > 0)
                    {
                        object tmp = lb.Items[i];
                        lb.Items[i] = lb.Items[i - 1];
                        lb.Items[i - 1] = tmp;
                        lb.SelectedIndex = i - 1;
                    }
                };

                var btnDown = new Button
                {
                    Text = "\\/",
                    Location = new Point(265, 10),
                    Size = new Size(40, 30)
                };
                btnDown.Click += (se, ev) =>
                {
                    int i = lb.SelectedIndex;
                    if (i >= 0 && i < lb.Items.Count - 1)
                    {
                        object tmp = lb.Items[i];
                        lb.Items[i] = lb.Items[i + 1];
                        lb.Items[i + 1] = tmp;
                        lb.SelectedIndex = i + 1;
                    }
                };

                var btnOk = new Button
                {
                    Text = "Salvar ordem",
                    Location = new Point(10, 360),
                    Size = new Size(100, 28)
                };
                btnOk.Click += (se, ev) =>
                {
                    var ordered = new List<string>();
                    foreach (object item in lb.Items)
                        ordered.Add((string)item);
                    string backupDir = p.GetBackupDir();
                    var fullPaths = new List<string>();
                    foreach (string name in ordered)
                        fullPaths.Add(Path.Combine(backupDir, name));
                    p.SaveIconOrder(fullPaths.ToArray());
                    RefreshIconCount();
                    dlg.Close();
                };

                var btnCancel = new Button
                {
                    Text = "Cancelar",
                    Location = new Point(120, 360),
                    Size = new Size(100, 28)
                };
                btnCancel.Click += (se, ev) => dlg.Close();

                dlg.Controls.AddRange(new Control[] {
                    lb, btnUp, btnDown, btnOk, btnCancel
                });

                dlg.ShowDialog(this);
            }
        }

        private void RefreshIconCount()
        {
            string[] icons = p.GetOrderedIcons();
            string orderFile = Path.Combine(p.GetBackupDir(), ".order");
            bool hasOrder = File.Exists(orderFile);
            lblIconCount.Text = icons.Length + " icone(s) no backupdesktop"
                + (hasOrder ? " (ordem personalizada)" : "");
        }

        private void RefreshLastSaveTime()
        {
            string last = p.GetLastSaveTime();
            lblStatus.Text = !string.IsNullOrEmpty(last)
                ? "Ultimo backup: " + last
                : "Nenhum backup salvo ainda.";
            lblStatus.ForeColor = Color.Gray;
        }

        private void BtnDesinstalar_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "Isso ira:\n" +
                "- Remover a inicializacao automatica\n" +
                "- Remover a restauracao automatica\n" +
                "- Excluir este programa\n\n" +
                "Tem certeza?",
                "Desinstalar",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (result != DialogResult.Yes) return;

            p.RemoveAll();

            string exePath = Application.ExecutablePath;
            string batPath = Path.Combine(Path.GetTempPath(),
                "del_LabLock_" + Guid.NewGuid().ToString("N").Substring(0, 8) + ".bat");
            try
            {
                File.WriteAllText(batPath,
                    "@echo off\r\n" +
                    "timeout /t 2 /nobreak > nul\r\n" +
                    "del /f /q \"" + exePath + "\"\r\n" +
                    "del /f /q \"" + batPath + "\"\r\n");
                Process.Start(new ProcessStartInfo("cmd.exe", "/c \"" + batPath + "\"")
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true
                });
            }
            catch { }

            ExitApp(true);
        }

        private void ExitApp(bool skipSave)
        {
            reallyClosing = true;
            trayIcon.Visible = false;
            if (!skipSave) p.SaveSettings();
            Close();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!reallyClosing)
            {
                p.SaveSettings();
                e.Cancel = true;
                Hide();
                return;
            }
            base.OnFormClosing(e);
        }

        protected override void OnResize(EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
                Hide();
            base.OnResize(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && trayIcon != null)
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    class DesktopProtector : IDisposable
    {
        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        private const int WH_MOUSE_LL = 14;
        private const int WM_MOUSEWHEEL = 0x020A;
        private const int VK_CONTROL = 0x11;
        private const uint GA_ROOT = 2;

        private const uint SHCNE_ASSOCCHANGED = 0x08000000;
        private const uint SHCNF_IDLIST = 0x0000;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SetWindowsHookEx(int idHook,
            LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk,
            int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(int x, int y);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd,
            StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);

        [DllImport("shell32.dll")]
        private static extern void SHChangeNotify(uint wEventId,
            uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public int x;
            public int y;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        private LowLevelMouseProc mouseProc;
        private IntPtr mouseHook;

        private const string RegSettings = @"Software\LabLock\Settings";
        private const string RegRun = @"Software\Microsoft\Windows\CurrentVersion\Run";

        private bool blockScroll = true;
        private bool autoRestoreLayout = false;
        private bool autoStart = true;

        private string Desktop
        {
            get { return Environment.GetFolderPath(Environment.SpecialFolder.Desktop); }
        }

        private string Documents
        {
            get { return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments); }
        }

        private string BackupDir
        {
            get
            {
                string exeDir = Path.GetDirectoryName(Application.ExecutablePath);
                return Path.Combine(exeDir, "backupdesktop");
            }
        }

        private string UserBackupDir
        {
            get { return Path.Combine(Documents, "Backup"); }
        }

        public bool BlockScroll
        {
            get { return blockScroll; }
            set
            {
                if (blockScroll == value) return;
                blockScroll = value;
                ApplyHookState();
                SaveSettings();
            }
        }

        public bool AutoRestoreLayout
        {
            get { return autoRestoreLayout; }
            set
            {
                if (autoRestoreLayout == value) return;
                autoRestoreLayout = value;
                SaveSettings();
            }
        }

        public bool AutoStart
        {
            get { return autoStart; }
            set
            {
                if (autoStart == value) return;
                autoStart = value;
                SetAutoStart(value);
                SaveSettings();
            }
        }

        public void LoadSettings()
        {
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(RegSettings))
                {
                    if (key != null)
                    {
                        blockScroll = ((int)key.GetValue("BlockScroll", 1)) == 1;
                        autoRestoreLayout = ((int)key.GetValue("AutoRestoreLayout", 0)) == 1;
                        autoStart = ((int)key.GetValue("AutoStart", 1)) == 1;
                    }
                }
            }
            catch { }
        }

        public void SaveSettings()
        {
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(RegSettings))
                {
                    if (key != null)
                    {
                        key.SetValue("BlockScroll", blockScroll ? 1 : 0, RegistryValueKind.DWord);
                        key.SetValue("AutoRestoreLayout", autoRestoreLayout ? 1 : 0, RegistryValueKind.DWord);
                        key.SetValue("AutoStart", autoStart ? 1 : 0, RegistryValueKind.DWord);
                        key.Flush();
                    }
                }
            }
            catch { }
        }

        public DesktopProtector()
        {
            mouseHook = SetupMouseHook();
        }

        private IntPtr SetupMouseHook()
        {
            if (!blockScroll) return IntPtr.Zero;
            mouseProc = MouseHookCallback;
            return SetWindowsHookEx(WH_MOUSE_LL, mouseProc, IntPtr.Zero, 0);
        }

        private void ApplyHookState()
        {
            if (blockScroll && mouseHook == IntPtr.Zero)
            {
                mouseProc = MouseHookCallback;
                mouseHook = SetWindowsHookEx(WH_MOUSE_LL, mouseProc, IntPtr.Zero, 0);
            }
            else if (!blockScroll && mouseHook != IntPtr.Zero)
            {
                UnhookWindowsHookEx(mouseHook);
                mouseHook = IntPtr.Zero;
            }
        }

        public string GetLastSaveTime()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RegSettings))
                {
                    if (key == null) return null;
                    return key.GetValue("LastSaveTime", "") as string;
                }
            }
            catch { return null; }
        }

        public bool SaveLayout()
        {
            try
            {
                Directory.CreateDirectory(UserBackupDir);

                BackupFolderToFlat(Desktop, UserBackupDir, "backupdesktop");

                string downloads = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Downloads");
                if (Directory.Exists(downloads))
                    BackupFolderToFlat(downloads, UserBackupDir, null);

                string pics = Environment.GetFolderPath(
                    Environment.SpecialFolder.MyPictures);
                if (Directory.Exists(pics))
                    BackupFolderToFlat(pics, UserBackupDir, null);

                string music = Environment.GetFolderPath(
                    Environment.SpecialFolder.MyMusic);
                if (Directory.Exists(music))
                    BackupFolderToFlat(music, UserBackupDir, null);

                string videos = Environment.GetFolderPath(
                    Environment.SpecialFolder.MyVideos);
                if (Directory.Exists(videos))
                    BackupFolderToFlat(videos, UserBackupDir, null);

                CopyFilesFlatRecursive(Documents, UserBackupDir, "Backup");

                using (var key = Registry.CurrentUser.CreateSubKey(RegSettings))
                {
                    if (key != null)
                    {
                        key.SetValue("LastSaveTime",
                            DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
                            RegistryValueKind.String);
                        key.Flush();
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao salvar backup: " + ex.Message,
                    "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private static void BackupFolderToFlat(string src, string dst, string exclude)
        {
            foreach (string f in Directory.GetFiles(src))
            {
                string name = Path.GetFileName(f);
                if (exclude == null || !string.Equals(name, exclude, StringComparison.OrdinalIgnoreCase))
                    SafeCopyFile(f, Path.Combine(dst, name));
            }
        }

        private static void CopyFilesFlatRecursive(string src, string dst, string exclude)
        {
            foreach (string f in Directory.GetFiles(src))
            {
                string name = Path.GetFileName(f);
                if (!string.Equals(name, exclude, StringComparison.OrdinalIgnoreCase))
                    SafeCopyFile(f, Path.Combine(dst, name));
            }
            foreach (string d in Directory.GetDirectories(src))
            {
                string name = Path.GetFileName(d);
                if (!string.Equals(name, exclude, StringComparison.OrdinalIgnoreCase))
                {
                    try { CopyFilesFlatRecursive(d, dst, exclude); }
                    catch { }
                }
            }
        }

        private static void SafeCopyFile(string src, string dst)
        {
            try
            {
                if ((File.GetAttributes(src) & FileAttributes.Directory) == 0)
                    File.Copy(src, dst, true);
            }
            catch { }
        }

        public bool RestoreLayout(bool showFeedback)
        {
            try
            {
                if (!Directory.Exists(BackupDir))
                {
                    if (showFeedback)
                        MessageBox.Show("Nenhum backup encontrado em:\n" +
                            BackupDir + "\n\n" +
                            "Coloque os icones manualmente na pasta 'backupdesktop' " +
                            "ao lado do executavel e tente novamente.",
                            "Backup nao encontrado",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return false;
                }

                SaveLayout();

                WipeFolder(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
                WipeFolder(
                    Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        "Downloads"));
                WipeFolder(Environment.GetFolderPath(
                    Environment.SpecialFolder.MyPictures));
                WipeFolder(Environment.GetFolderPath(
                    Environment.SpecialFolder.MyMusic));
                WipeFolder(Environment.GetFolderPath(
                    Environment.SpecialFolder.MyVideos));
                WipeFolderPreserving(Documents, new[] { "Backup" });

                string[] ordered = GetOrderedIcons();
            foreach (string f in ordered)
            {
                string dest = Path.Combine(Desktop, Path.GetFileName(f));
                File.Copy(f, dest, true);
            }

                PurgeBackupExecutables(UserBackupDir);

                SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);

                return true;
            }
            catch (Exception ex)
            {
                if (showFeedback)
                    MessageBox.Show("Erro ao restaurar: " + ex.Message,
                        "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private static readonly string[] BlockedExts = {
            ".lnk", ".exe", ".msi", ".ps1", ".bat" };

        private static void PurgeBackupExecutables(string path)
        {
            if (!Directory.Exists(path)) return;

            foreach (string f in Directory.GetFiles(path))
            {
                string ext = Path.GetExtension(f);
                foreach (string b in BlockedExts)
                {
                    if (string.Equals(ext, b, StringComparison.OrdinalIgnoreCase))
                    {
                        TryDeleteFile(f);
                        break;
                    }
                }
            }
            foreach (string d in Directory.GetDirectories(path))
            {
                try { PurgeBackupExecutables(d); }
                catch { }
            }
        }

        private static void WipeFolder(string path)
        {
            if (!Directory.Exists(path)) return;

            foreach (string f in Directory.GetFiles(path))
                TryDeleteFile(f);
            foreach (string d in Directory.GetDirectories(path))
                TryDeleteDir(d);
        }

        private static void WipeFolderPreserving(string path, string[] preserve)
        {
            if (!Directory.Exists(path)) return;

            foreach (string f in Directory.GetFiles(path))
            {
                string name = Path.GetFileName(f);
                if (!IsPreserved(name, preserve))
                    TryDeleteFile(f);
            }
            foreach (string d in Directory.GetDirectories(path))
            {
                string name = Path.GetFileName(d);
                if (!IsPreserved(name, preserve))
                    TryDeleteDir(d);
            }
        }

        private static bool IsPreserved(string name, string[] preserve)
        {
            foreach (string p in preserve)
                if (string.Equals(name, p, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        private static void TryDeleteFile(string path)
        {
            try
            {
                File.SetAttributes(path, FileAttributes.Normal);
                File.Delete(path);
            }
            catch { }
        }

        private static void TryDeleteDir(string path)
        {
            try
            {
                WipeFolder(path);
                Directory.Delete(path, false);
            }
            catch { }
        }

        public string GetBackupDir()
        {
            return BackupDir;
        }

        public string[] GetOrderedIcons()
        {
            if (!Directory.Exists(BackupDir)) return new string[0];

            string[] files = Directory.GetFiles(BackupDir);
            string orderFile = Path.Combine(BackupDir, ".order");

            if (File.Exists(orderFile))
            {
                var order = new List<string>();
                foreach (string line in File.ReadAllLines(orderFile))
                {
                    string name = line.Trim();
                    if (string.IsNullOrEmpty(name)) continue;
                    string full = Path.Combine(BackupDir, name);
                    if (File.Exists(full))
                        order.Add(full);
                }
                foreach (string f in files)
                {
                    string name = Path.GetFileName(f);
                    if (name.StartsWith(".")) continue;
                    if (!order.Contains(f))
                        order.Add(f);
                }
                return order.ToArray();
            }

            return files;
        }

        public void SaveIconOrder(string[] orderedFiles)
        {
            if (!Directory.Exists(BackupDir)) return;
            string orderFile = Path.Combine(BackupDir, ".order");
            var lines = new List<string>();
            foreach (string f in orderedFiles)
                lines.Add(Path.GetFileName(f));
            File.WriteAllLines(orderFile, lines);
        }

        public void RemoveAll()
        {
            SetAutoStart(false);

            if (mouseHook != IntPtr.Zero)
            {
                UnhookWindowsHookEx(mouseHook);
                mouseHook = IntPtr.Zero;
            }

            try { Registry.CurrentUser.DeleteSubKeyTree(RegSettings, false); } catch { }
            try
            {
                if (Directory.Exists(BackupDir))
                    Directory.Delete(BackupDir, true);
            }
            catch { }
        }

        private void SetAutoStart(bool enable)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RegRun, true))
                {
                    if (key != null)
                    {
                        if (enable)
                            key.SetValue("LabLock", Application.ExecutablePath);
                        else
                            key.DeleteValue("LabLock", false);
                    }
                }
            }
            catch { }
        }

        private static bool CursorOverDesktop(IntPtr lParam)
        {
            MSLLHOOKSTRUCT data = (MSLLHOOKSTRUCT)
                Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));

            IntPtr hwnd = WindowFromPoint(data.x, data.y);
            if (hwnd == IntPtr.Zero) return false;

            IntPtr root = GetAncestor(hwnd, GA_ROOT);
            if (root == IntPtr.Zero) return false;

            var sb = new StringBuilder(256);
            GetClassName(root, sb, 256);
            string cls = sb.ToString();
            return cls == "Progman" || cls == "WorkerW";
        }

        private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (uint)wParam == WM_MOUSEWHEEL && blockScroll)
            {
                if ((GetAsyncKeyState(VK_CONTROL) & 0x8000) != 0 &&
                    CursorOverDesktop(lParam))
                {
                    return (IntPtr)1;
                }
            }
            return CallNextHookEx(mouseHook, nCode, wParam, lParam);
        }

        public void Dispose()
        {
            if (mouseHook != IntPtr.Zero)
            {
                UnhookWindowsHookEx(mouseHook);
                mouseHook = IntPtr.Zero;
            }
        }
    }
}
