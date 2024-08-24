using System.Diagnostics;
using System.Windows.Forms;

namespace systemTray
{
    public class TrayHandler
    {
        private static NotifyIcon notifyIcon;
        private static ContextMenuStrip contextMenu;

        [STAThread]
        public static void Init()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Utwórz obiekt NotifyIcon
            notifyIcon = new NotifyIcon
            {
                Icon = new System.Drawing.Icon(Program.iconPath), // Use the path set in Program
                Visible = true
            };

            // Utwórz menu kontekstowe
            contextMenu = new ContextMenuStrip();

            // Dodaj elementy do menu kontekstowego
            ToolStripMenuItem reloadConfigItem = new ToolStripMenuItem("Reload config", null, ReloadConfig);
            ToolStripMenuItem openSerialItem = new ToolStripMenuItem("Open Serial Port", null, OpenSerialPort);
            ToolStripMenuItem editConfigItem = new ToolStripMenuItem("Edit config", null, EditCfg);
            ToolStripMenuItem reloadAudioDevice = new ToolStripMenuItem("Reload audio device", null, ReloadAudioDevice);
            ToolStripMenuItem exitItem = new ToolStripMenuItem("Exit", null, Exit);

            contextMenu.Items.Add(reloadConfigItem);
            contextMenu.Items.Add(openSerialItem);
            contextMenu.Items.Add(editConfigItem);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(reloadAudioDevice);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(exitItem);

            // Przypisz menu kontekstowe do NotifyIcon
            notifyIcon.ContextMenuStrip = contextMenu;

            // Rozpocznij aplikację
            Application.Run();
        }

        private static void ReloadConfig(object sender, EventArgs e)
        {
            Program.initConfig();
            if (!Program.initSerial)
            {
                Program.initSerial = Program.initSerialPort(Program.root.Port, Program.root.Baudrate);
                Program.mainLoop();
            }
        }

        private static void OpenSerialPort(object sender, EventArgs e)
        {
            if (Program.initCfg)
                Program.initSerialPort(Program.root.Port, Program.root.Baudrate);
        }

        private static void EditCfg(object sender, EventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(Program.configPath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error while opening file \n {ex.Message}", "File Open Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void ReloadAudioDevice(object sender, EventArgs e)
        {
            Program.initAudioDevices();
        }

        private static void Exit(object sender, EventArgs e)
        {
            // Signal the main program to exit
            Program.programExit = true;
            // Trigger application exit
            Application.Exit();
        }
    }
}
