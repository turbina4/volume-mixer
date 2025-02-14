using System.Diagnostics;

namespace systemTray
{
    class CustomColorTable : ProfessionalColorTable
    {
        public override Color ToolStripBorder => Color.Transparent;
        public override Color MenuBorder => Color.Transparent;
        public override Color MenuItemSelected => Color.FromArgb(30, 30, 35);
        public override Color MenuItemSelectedGradientBegin => Color.FromArgb(60, 60, 70);
        public override Color MenuItemSelectedGradientEnd => Color.FromArgb(60, 60, 70);
        public override Color MenuItemBorder => Color.Transparent;
    }

    public class TrayHandler
    {
        private static NotifyIcon notifyIcon;
        private static ContextMenuStrip contextMenu;

        [STAThread]
        public static void Init()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Utwórz obiekt NotifyIcon
            notifyIcon = new NotifyIcon
            {
                Icon = new System.Drawing.Icon(Program.iconPath), // Use the path set in Program
                Visible = true
            };

            // Utwórz menu kontekstowe
            contextMenu = new ContextMenuStrip
            {
                ShowImageMargin = false,
                ShowCheckMargin = false,
                Font = new Font("Lato", 10),
                BackColor = Color.FromArgb(30, 30, 35),
                ForeColor = Color.White,
                DropShadowEnabled = true,
                Padding = new Padding(5),
                Renderer = new ToolStripProfessionalRenderer(new CustomColorTable()),
            };

            //Dodaj elementy do menu kontekstowego

            ToolStripMenuItem reloadConfigItem = new ToolStripMenuItem("Reload config", null, ReloadConfig)
            {
                Padding = new Padding(3)
            };
            ToolStripMenuItem openSerialItem = new ToolStripMenuItem("Open Serial Port", null, OpenSerialPort)
            {
                Padding = new Padding(3)
            };
            ToolStripMenuItem editConfigItem = new ToolStripMenuItem("Edit config", null, EditCfg)
            {
                Padding = new Padding(3)
            };
            ToolStripMenuItem reloadAudioDevice = new ToolStripMenuItem("Reload audio devices", null, ReloadAudioDevice)
            {
                Padding = new Padding(3)
            };
            ToolStripMenuItem exitItem = new ToolStripMenuItem("Exit", null, Exit)
            {
                Padding = new Padding(3)
            };

            contextMenu.Items.Add(new ToolStripLabel { Text = "Mixer Control Panel", Font = new Font("Lato", 10, FontStyle.Bold), Padding = new Padding(0, 10, 0, 10) });
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(editConfigItem);
            contextMenu.Items.Add(reloadConfigItem);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(openSerialItem);
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
