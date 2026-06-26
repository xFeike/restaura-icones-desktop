using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
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

                using (var protector = new DesktopProtector())
                {
                    protector.LoadSettings();

                    if (protector.AutoRestoreLayout)
                        protector.RestoreLayout(false);

                    using (var form = new SettingsForm(protector))
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
        private DesktopProtector protector;
        private NotifyIcon trayIcon;
        private CheckBox chkBlockScroll;
        private CheckBox chkAutoRestoreLayout;
        private CheckBox chkAutoStart;
        private Button btnSaveLayout;
        private Button btnRestoreNow;
        private Button btnDesinstalar;
        private bool reallyClosing;
        private Label lblStatus;

        public SettingsForm(DesktopProtector protector)
        {
            this.protector = protector;

            Text = "LabLock - Protecao da Area de Trabalho";
            Size = new Size(440, 380);
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
                Text = "Layout dos Icones",
                Location = new Point(20, y),
                Size = new Size(380, 18),
                Font = boldFont
            };

            y += 22;
            btnSaveLayout = new Button
            {
                Text = "Salvar layout atual dos icones",
                Location = new Point(20, y),
                Size = new Size(180, 30),
                Font = normalFont,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 123, 255),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnSaveLayout.FlatAppearance.BorderSize = 0;
            btnSaveLayout.Click += BtnSaveLayout_Click;

            btnRestoreNow = new Button
            {
                Text = "Restaurar layout agora",
                Location = new Point(210, y),
                Size = new Size(190, 30),
                Font = normalFont,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnRestoreNow.FlatAppearance.BorderSize = 0;
            btnRestoreNow.Click += BtnRestoreNow_Click;

            y += 36;
            string lastSave = protector.GetLastSaveTime();
            lblStatus = new Label
            {
                Text = !string.IsNullOrEmpty(lastSave)
                    ? "Ultimo salvamento: " + lastSave
                    : "Nenhum layout salvo ainda.",
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
                Checked = protector.AutoStart
            };
            chkAutoStart.CheckedChanged += (s, e) =>
                protector.AutoStart = chkAutoStart.Checked;

            y += 30;
            chkAutoRestoreLayout = new CheckBox
            {
                Text = "Restaurar layout salvo na inicializacao",
                Location = new Point(20, y),
                Size = new Size(380, 26),
                Font = normalFont,
                Checked = protector.AutoRestoreLayout
            };
            chkAutoRestoreLayout.CheckedChanged += (s, e) =>
                protector.AutoRestoreLayout = chkAutoRestoreLayout.Checked;

            y += 30;

            var lbl1 = new Label
            {
                Text = "Protecoes",
                Location = new Point(20, y),
                Size = new Size(380, 18),
                Font = boldFont
            };

            y += 22;
            chkBlockScroll = new CheckBox
            {
                Text = "Bloquear Ctrl+Scroll na area de trabalho",
                Location = new Point(20, y),
                Size = new Size(380, 26),
                Font = normalFont,
                Checked = protector.BlockScroll
            };
            chkBlockScroll.CheckedChanged += (s, e) =>
                protector.BlockScroll = chkBlockScroll.Checked;

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
                lbl2, btnSaveLayout, btnRestoreNow,
                lbl3, chkAutoStart, chkAutoRestoreLayout, lblStatus,
                lbl1, chkBlockScroll,
                btnDesinstalar
            });

            trayIcon = new NotifyIcon
            {
                Icon = SystemIcons.Shield,
                Text = "LabLock - Protecao da Area de Trabalho",
                Visible = true
            };
            trayIcon.DoubleClick += (s, e) => ShowFromTray();

            var menu = new ContextMenuStrip();
            menu.Items.Add("Configuracoes", null, (s, e) => ShowFromTray());
            menu.Items.Add("Restaurar layout agora", null, (s, e) => DoRestore());
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

        private void BtnSaveLayout_Click(object sender, EventArgs e)
        {
            if (protector.SaveLayout())
            {
                if (!chkAutoRestoreLayout.Checked)
                {
                    chkAutoRestoreLayout.Checked = true;
                    protector.AutoRestoreLayout = true;
                }
                SetStatus("Layout salvo com sucesso!", false);
            }
            else
            {
                SetStatus("Erro ao salvar layout.", true);
            }
            RefreshLastSaveTime();
        }

        private void BtnRestoreNow_Click(object sender, EventArgs e)
        {
            DoRestore();
        }

        private void DoRestore()
        {
            if (protector.RestoreLayout(true))
            {
                SetStatus("Layout restaurado com sucesso!", false);
                RefreshLastSaveTime();
            }
            else
            {
                SetStatus("Falha ao restaurar. Nenhum layout salvo?", true);
            }
        }

        private void RefreshLastSaveTime()
        {
            string lastSave = protector.GetLastSaveTime();
            lblStatus.Text = !string.IsNullOrEmpty(lastSave)
                ? "Ultimo salvamento: " + lastSave
                : "Nenhum layout salvo ainda.";
            lblStatus.ForeColor = Color.Gray;
        }

        private void BtnDesinstalar_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "Isso ira:\n" +
                "- Remover a inicializacao automatica\n" +
                "- Remover a restauracao automatica do layout\n" +
                "- Restaurar configuracoes originais do registro\n" +
                "- Excluir este programa\n\n" +
                "Tem certeza?",
                "Remover todas as alteracoes",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (result != DialogResult.Yes)
                return;

            protector.RemoveAll();

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
            if (!skipSave)
                protector.SaveSettings();
            Close();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!reallyClosing)
            {
                protector.SaveSettings();
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
        private const int HWND_BROADCAST = 0xFFFF;
        private const int WM_SETTINGCHANGE = 0x001A;
        private const uint GA_ROOT = 2;

        private const int LVM_GETITEMCOUNT = 0x1004;
        private const int LVM_GETITEMPOSITION = 0x1010;
        private const int LVM_SETITEMPOSITION = 0x100F;
        private const int LVM_GETITEMTEXT = 0x102D;

        private const uint PROCESS_VM_OPERATION = 0x0008;
        private const uint PROCESS_VM_READ = 0x0010;
        private const uint PROCESS_VM_WRITE = 0x0020;
        private const uint MEM_COMMIT = 0x1000;
        private const uint MEM_RESERVE = 0x2000;
        private const uint MEM_RELEASE = 0x8000;
        private const uint PAGE_READWRITE = 0x04;

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

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint msg,
            IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr FindWindow(string lpClassName,
            string lpWindowName);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent,
            IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd,
            out uint lpdwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint dwDesiredAccess,
            bool bInheritHandle, uint dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr VirtualAllocEx(IntPtr hProcess,
            IntPtr lpAddress, uint dwSize, uint flAllocationType,
            uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool VirtualFreeEx(IntPtr hProcess,
            IntPtr lpAddress, uint dwSize, uint dwFreeType);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ReadProcessMemory(IntPtr hProcess,
            IntPtr lpBaseAddress, [Out] byte[] lpBuffer, uint nSize,
            out uint lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool WriteProcessMemory(IntPtr hProcess,
            IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize,
            out uint lpNumberOfBytesWritten);

        [DllImport("shell32.dll")]
        private static extern void SHChangeNotify(uint wEventId,
            uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINTAPI
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINTAPI pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct LVITEMW
        {
            public uint mask;
            public int iItem;
            public int iSubItem;
            public uint state;
            public uint stateMask;
            public IntPtr pszText;
            public int cchTextMax;
            public int iImage;
            public IntPtr lParam;
            public int iIndent;
            public int iGroupId;
            public uint cColumns;
            public IntPtr puColumns;
            public IntPtr piColFmt;
            public int iGroup;
        }

        private const uint LVIF_TEXT = 0x0001;

        private LowLevelMouseProc mouseProc;
        private IntPtr mouseHook;

        private const string RegSettings = @"Software\LabLock\Settings";
        private const string RegRun = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string RegBags = @"Software\Microsoft\Windows\Shell\Bags\1\Desktop";
        private const string RegSavedLayout = @"Software\LabLock\SavedLayout";

        private bool blockScroll = true;
        private bool autoRestoreLayout = false;
        private bool autoStart = true;

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

        public bool SaveLayout()
        {
            try
            {
                using (var source = Registry.CurrentUser.OpenSubKey(RegBags))
                {
                    if (source == null)
                    {
                        MessageBox.Show("Nao foi possivel ler o layout do registro.",
                            "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }

                    Registry.CurrentUser.DeleteSubKeyTree(RegSavedLayout, false);
                    using (var dest = Registry.CurrentUser.CreateSubKey(RegSavedLayout))
                    {
                        CopyKey(source, dest);
                    }
                }

                SavePositionsFromListView();

                using (var dest = Registry.CurrentUser.CreateSubKey(RegSavedLayout))
                {
                    if (dest != null)
                        dest.SetValue("LastSaveTime",
                            DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
                            RegistryValueKind.String);
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao salvar layout: " + ex.Message,
                    "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        public string GetLastSaveTime()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RegSavedLayout))
                {
                    if (key == null) return null;
                    return key.GetValue("LastSaveTime", "") as string;
                }
            }
            catch { return null; }
        }

        private void SavePositionsFromListView()
        {
            IntPtr listView = FindDesktopListView();
            if (listView == IntPtr.Zero) return;

            uint pid;
            GetWindowThreadProcessId(listView, out pid);
            IntPtr hProc = OpenProcess(
                PROCESS_VM_OPERATION | PROCESS_VM_READ | PROCESS_VM_WRITE, false, pid);
            if (hProc == IntPtr.Zero) return;

            try
            {
                int count = (int)SendMessage(listView, LVM_GETITEMCOUNT,
                    IntPtr.Zero, IntPtr.Zero);
                if (count <= 0 || count > 500) return;

                uint cbPt = (uint)Marshal.SizeOf(typeof(POINTAPI));
                uint cbItem = (uint)Marshal.SizeOf(typeof(LVITEMW));
                uint cbText = 520;

                IntPtr remotePt = VirtualAllocEx(hProc, IntPtr.Zero,
                    cbPt, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
                IntPtr remoteItem = VirtualAllocEx(hProc, IntPtr.Zero,
                    cbItem + cbText, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
                if (remotePt == IntPtr.Zero || remoteItem == IntPtr.Zero)
                {
                    if (remotePt != IntPtr.Zero)
                        VirtualFreeEx(hProc, remotePt, 0, MEM_RELEASE);
                    if (remoteItem != IntPtr.Zero)
                        VirtualFreeEx(hProc, remoteItem, 0, MEM_RELEASE);
                    return;
                }

                IntPtr remoteText = new IntPtr(remoteItem.ToInt64() + cbItem);

                using (var key = Registry.CurrentUser.CreateSubKey(RegSavedLayout))
                {
                    for (int i = 0; i < count; i++)
                    {
                        SendMessage(listView, LVM_GETITEMPOSITION,
                            (IntPtr)i, remotePt);

                        byte[] bufPt = new byte[cbPt];
                        uint read;
                        if (!ReadProcessMemory(hProc, remotePt, bufPt, cbPt, out read)
                            || read != cbPt)
                            continue;

                        POINTAPI pt;
                        GCHandle hPt = GCHandle.Alloc(bufPt, GCHandleType.Pinned);
                        try
                        {
                            pt = (POINTAPI)Marshal.PtrToStructure(
                                hPt.AddrOfPinnedObject(), typeof(POINTAPI));
                        }
                        finally { hPt.Free(); }

                        LVITEMW lvi = new LVITEMW();
                        lvi.mask = LVIF_TEXT;
                        lvi.iItem = i;
                        lvi.iSubItem = 0;
                        lvi.pszText = remoteText;
                        lvi.cchTextMax = 260;

                        byte[] structBuf = new byte[cbItem];
                        IntPtr structPtr = Marshal.AllocHGlobal((int)cbItem);
                        try
                        {
                            Marshal.StructureToPtr(lvi, structPtr, false);
                            Marshal.Copy(structPtr, structBuf, 0, (int)cbItem);
                        }
                        finally { Marshal.FreeHGlobal(structPtr); }

                        uint written;
                        WriteProcessMemory(hProc, remoteItem, structBuf, cbItem, out written);

                        SendMessage(listView, LVM_GETITEMTEXT, (IntPtr)i, remoteItem);

                        byte[] textBuf = new byte[cbText];
                        if (ReadProcessMemory(hProc, remoteText, textBuf, cbText, out read)
                            && read >= 2)
                        {
                            string name = Encoding.Unicode.GetString(textBuf, 0, (int)read);
                            int nullPos = name.IndexOf('\0');
                            if (nullPos >= 0) name = name.Substring(0, nullPos);

                            string entry = name + "|" + pt.x + "|" + pt.y;
                            key.SetValue("Icon_" + i.ToString("D4"), entry,
                                RegistryValueKind.String);
                        }
                    }
                }

                VirtualFreeEx(hProc, remotePt, 0, MEM_RELEASE);
                VirtualFreeEx(hProc, remoteItem, 0, MEM_RELEASE);
            }
            finally
            {
                CloseHandle(hProc);
            }
        }

        public bool RestoreLayout(bool showFeedback)
        {
            try
            {
                using (var saveKey = Registry.CurrentUser.OpenSubKey(RegSavedLayout))
                {
                    if (saveKey == null)
                    {
                        if (showFeedback)
                            MessageBox.Show("Nenhum layout salvo encontrado.\n" +
                                "Use 'Salvar layout atual' primeiro.",
                                "Layout nao encontrado",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return false;
                    }

                    using (var dest = Registry.CurrentUser.CreateSubKey(RegBags))
                    {
                        ClearKey(dest);
                        CopyKey(saveKey, dest);
                    }
                }

                NotifyDesktopRefresh();
                BackupNewDesktopItems();
                RestorePositionsToListView();

                return true;
            }
            catch (Exception ex)
            {
                if (showFeedback)
                    MessageBox.Show("Erro ao restaurar layout: " + ex.Message,
                        "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private void BackupNewDesktopItems()
        {
            var savedNames = new HashSet<string>();
            using (var key = Registry.CurrentUser.OpenSubKey(RegSavedLayout))
            {
                if (key == null) return;
                string[] valueNames = key.GetValueNames();
                foreach (string vn in valueNames)
                {
                    if (!vn.StartsWith("Icon_")) continue;
                    string val = key.GetValue(vn, "") as string;
                    if (string.IsNullOrEmpty(val)) continue;
                    string[] parts = val.Split('|');
                    if (parts.Length == 3 && !string.IsNullOrEmpty(parts[0]))
                        savedNames.Add(parts[0]);
                }
            }
            if (savedNames.Count == 0) return;

            string desktop = Environment.GetFolderPath(
                Environment.SpecialFolder.Desktop);
            string backup = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "backup");
            try { Directory.CreateDirectory(backup); } catch { }

            foreach (string f in Directory.GetFiles(desktop))
            {
                string name = Path.GetFileName(f);
                string nameNoExt = Path.GetFileNameWithoutExtension(f);
                if (savedNames.Contains(name) || savedNames.Contains(nameNoExt))
                    continue;
                try
                {
                    string dest = Path.Combine(backup, name);
                    if (File.Exists(dest)) File.Delete(dest);
                    File.Move(f, dest);
                }
                catch { }
            }

            foreach (string d in Directory.GetDirectories(desktop))
            {
                string name = Path.GetFileName(d);
                if (name == "backup" || savedNames.Contains(name))
                    continue;
                try
                {
                    string dest = Path.Combine(backup, name);
                    if (Directory.Exists(dest))
                        Directory.Delete(dest, true);
                    Directory.Move(d, dest);
                }
                catch { }
            }
        }

        private void RestorePositionsToListView()
        {
            var savedByName = new Dictionary<string, Point>();
            var savedByIndex = new Dictionary<int, Point>();
            using (var key = Registry.CurrentUser.OpenSubKey(RegSavedLayout))
            {
                if (key == null) return;

                string[] valueNames = key.GetValueNames();
                foreach (string vn in valueNames)
                {
                    if (!vn.StartsWith("Icon_")) continue;
                    string val = key.GetValue(vn, "") as string;
                    if (string.IsNullOrEmpty(val)) continue;

                    int si = int.Parse(vn.Substring(5));
                    string[] parts = val.Split('|');
                    if (parts.Length == 3)
                    {
                        int x, y;
                        if (int.TryParse(parts[1], out x)
                            && int.TryParse(parts[2], out y))
                        {
                            if (!string.IsNullOrEmpty(parts[0]))
                                savedByName[parts[0]] = new Point(x, y);
                            else
                                savedByIndex[si] = new Point(x, y);
                        }
                    }
                }
            }

            if (savedByName.Count == 0 && savedByIndex.Count == 0) return;

            IntPtr listView = FindDesktopListView();
            if (listView == IntPtr.Zero) return;

            uint pid;
            GetWindowThreadProcessId(listView, out pid);
            IntPtr hProc = OpenProcess(
                PROCESS_VM_OPERATION | PROCESS_VM_READ | PROCESS_VM_WRITE, false, pid);
            if (hProc == IntPtr.Zero) return;

            try
            {
                int count = (int)SendMessage(listView, LVM_GETITEMCOUNT,
                    IntPtr.Zero, IntPtr.Zero);
                if (count <= 0 || count > 500) return;

                uint cbItem = (uint)Marshal.SizeOf(typeof(LVITEMW));
                uint cbText = 520;

                IntPtr remoteItem = VirtualAllocEx(hProc, IntPtr.Zero,
                    cbItem + cbText, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
                if (remoteItem == IntPtr.Zero) return;
                IntPtr remoteText = new IntPtr(remoteItem.ToInt64() + cbItem);

                for (int i = 0; i < count; i++)
                {
                    LVITEMW lvi = new LVITEMW();
                    lvi.mask = LVIF_TEXT;
                    lvi.iItem = i;
                    lvi.iSubItem = 0;
                    lvi.pszText = remoteText;
                    lvi.cchTextMax = 260;

                    byte[] structBuf = new byte[cbItem];
                    IntPtr structPtr = Marshal.AllocHGlobal((int)cbItem);
                    try
                    {
                        Marshal.StructureToPtr(lvi, structPtr, false);
                        Marshal.Copy(structPtr, structBuf, 0, (int)cbItem);
                    }
                    finally { Marshal.FreeHGlobal(structPtr); }

                    uint written;
                    WriteProcessMemory(hProc, remoteItem, structBuf, cbItem, out written);

                    SendMessage(listView, LVM_GETITEMTEXT, (IntPtr)i, remoteItem);

                    byte[] textBuf = new byte[cbText];
                    uint read;
                    if (ReadProcessMemory(hProc, remoteText, textBuf, cbText, out read)
                        && read >= 2)
                    {
                        string name = Encoding.Unicode.GetString(textBuf, 0, (int)read);
                        int nullPos = name.IndexOf('\0');
                        if (nullPos >= 0) name = name.Substring(0, nullPos);

                        if (!string.IsNullOrEmpty(name)
                            && savedByName.ContainsKey(name))
                        {
                            Point pos = savedByName[name];
                            int lp = (pos.Y << 16) | (pos.X & 0xFFFF);
                            SendMessage(listView, LVM_SETITEMPOSITION,
                                (IntPtr)i, (IntPtr)lp);
                        }
                        else if (savedByIndex.ContainsKey(i))
                        {
                            Point pos = savedByIndex[i];
                            int lp = (pos.Y << 16) | (pos.X & 0xFFFF);
                            SendMessage(listView, LVM_SETITEMPOSITION,
                                (IntPtr)i, (IntPtr)lp);
                        }
                    }
                }

                VirtualFreeEx(hProc, remoteItem, 0, MEM_RELEASE);
            }
            finally
            {
                CloseHandle(hProc);
            }
        }

        private static IntPtr FindDesktopListView()
        {
            IntPtr progman = FindWindow("Progman", null);
            if (progman != IntPtr.Zero)
            {
                IntPtr lv = FindWindowEx(progman, IntPtr.Zero, "SysListView32", null);
                if (lv == IntPtr.Zero)
                {
                    IntPtr defView = FindWindowEx(progman, IntPtr.Zero,
                        "SHELLDLL_DefView", null);
                    if (defView != IntPtr.Zero)
                        lv = FindWindowEx(defView, IntPtr.Zero, "SysListView32", null);
                }
                if (lv != IntPtr.Zero) return lv;
            }

            IntPtr workerW = IntPtr.Zero;
            while (true)
            {
                workerW = FindWindowEx(IntPtr.Zero, workerW, "WorkerW", null);
                if (workerW == IntPtr.Zero) break;

                IntPtr dv = FindWindowEx(workerW, IntPtr.Zero, "SHELLDLL_DefView", null);
                if (dv != IntPtr.Zero)
                {
                    IntPtr lv2 = FindWindowEx(dv, IntPtr.Zero, "SysListView32", null);
                    if (lv2 != IntPtr.Zero) return lv2;
                }
            }
            return IntPtr.Zero;
        }

        private static void ClearKey(RegistryKey key)
        {
            foreach (string name in key.GetValueNames())
                key.DeleteValue(name);
            foreach (string name in key.GetSubKeyNames())
                key.DeleteSubKeyTree(name);
        }

        private static void CopyKey(RegistryKey source, RegistryKey dest)
        {
            foreach (string valueName in source.GetValueNames())
            {
                dest.SetValue(valueName, source.GetValue(valueName),
                    source.GetValueKind(valueName));
            }
            foreach (string subKeyName in source.GetSubKeyNames())
            {
                using (var srcSub = source.OpenSubKey(subKeyName))
                using (var dstSub = dest.CreateSubKey(subKeyName))
                {
                    CopyKey(srcSub, dstSub);
                }
            }
        }

        private static void NotifyDesktopRefresh()
        {
            SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);
            SendMessage((IntPtr)HWND_BROADCAST, WM_SETTINGCHANGE,
                IntPtr.Zero, IntPtr.Zero);

            IntPtr progman = FindWindow("Progman", null);
            if (progman != IntPtr.Zero)
                SendMessage(progman, 0x052C, IntPtr.Zero, IntPtr.Zero);
        }

        public void RemoveAll()
        {
            SetAutoStart(false);

            if (mouseHook != IntPtr.Zero)
            {
                UnhookWindowsHookEx(mouseHook);
                mouseHook = IntPtr.Zero;
            }

            try { Registry.CurrentUser.DeleteSubKeyTree(RegSavedLayout, false); } catch { }
            try { Registry.CurrentUser.DeleteSubKeyTree(RegSettings, false); } catch { }
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

            IntPtr hwnd = WindowFromPoint(data.pt.x, data.pt.y);
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
