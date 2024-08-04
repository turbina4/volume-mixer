using Microsoft.VisualBasic.ApplicationServices;
using System.Diagnostics;

namespace systemTray
{
    public class TrayHandler
    {
        [MTAThread]
        public static void Init()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Utwórz obiekt NotifyIcon
            NotifyIcon notifyIcon = new NotifyIcon
            {
                Icon = new System.Drawing.Icon(Program.iconPath),
                //Icon = new System.Drawing.Icon("C:\\Users\\Admin\\source\\repos\\volume-mixer\\volume-mixer\\mixerLogo.ico"),

                Visible = true
            }; ; 

            // Utwórz menu kontekstowe
            ContextMenuStrip contextMenu = new ContextMenuStrip();

            // Dodaj elementy do menu kontekstowego
            ToolStripMenuItem reloadConfigItem = new ToolStripMenuItem("Reload config", null, ReloadConfig);
            ToolStripMenuItem editConfigItem = new ToolStripMenuItem("Edit config", null, EditCfg);
            ToolStripMenuItem reloadAudioDevice = new ToolStripMenuItem("Reload audio device", null, ReloadAudioDevice);
            ToolStripMenuItem exitItem = new ToolStripMenuItem("Exit", null, Exit);

            contextMenu.Items.Add(reloadConfigItem);
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
                Program.initSerial = Program.initSerialPort(Program.root.Port, Program.root.Baudrate);
            
            //MessageBox.Show($"Config Reloaded", "Reload Cfg", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private static void EditCfg(object sender, EventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(Program.configPath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas otwierania pliku \n {ex.Message}", "File Open Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void ReloadAudioDevice(object sender, EventArgs e)
        {
            Program.initAudioDevice();
        }

        private static void Exit(object sender, EventArgs e)
        {
            Application.Exit();
            Program.programExit = true;
        }
    }
}
