using System.Diagnostics;
using System.IO.Ports;
using System.Runtime.InteropServices;
using volume_mixer;
using AudioSwitcher.AudioApi.CoreAudio;
using AudioSwitcher.AudioApi.Session;
using Newtonsoft.Json;
using yamlConfig;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

class Program
{
    public static bool initSerial = false;
    public static bool initCfg = false;
    public static bool programExit = false;

    private static SerialPort _serialPort; // Obiekt do komunikacji przez port szeregowy
    public static Config root; // Obiekt do przechowywania konfiguracji

    private static CoreAudioDevice playbackDevice; // Urządzenie do odtwarzania dźwięku
    private static CoreAudioDevice captureDevice; // Urządzenie do nagrywania dźwięku

    public static string roamingAppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    public static string configPath = Path.Combine(roamingAppData, "Turbina4Software\\config.yaml");
    public static string iconPath = Path.GetFullPath("mixerLogo.ico");

    private static List<object> parsedApps = new List<object>();
    private static string parsedAppsString = "";

    static string serialBuffer = ""; // Bufor na niepełne linie

    static float[] processedData = new float[5];
    static float[] oldProcessedData = new float[5];

    static uint activeAppPID = 0;


    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();


    private static void Main(string[] args)
    {
        Logger.logSpecial("----------------------> Mixer Software <----------------------");

        // Utwórz i uruchom wątek dla obsługi ikony w zasobniku systemowym
        Thread trayHandlerThread = new Thread(() =>
        {
            systemTray.TrayHandler.Init();
        });

        // Ustaw wątek jako STA (Single-Threaded Apartment)
        trayHandlerThread.SetApartmentState(ApartmentState.STA);
        trayHandlerThread.Start();

        // Inicjalizacja
        initAudioDevices();
        initCfg = initConfig();
        if (initCfg)
            initSerial = initSerialPort(root.Port, root.Baudrate);
    }


    private static void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
    {
        try
        {
            // Odczytanie wszystkich dostępnych danych
            string data = _serialPort.ReadExisting();
            serialBuffer += data; // Dodanie do bufora

            // Przetwarzanie danych
            int newLineIndex;
            while ((newLineIndex = serialBuffer.IndexOf('\n')) != -1)
            {
                string fullLine = serialBuffer.Substring(0, newLineIndex).Trim(); // Pobierz całą linię
                serialBuffer = serialBuffer.Substring(newLineIndex + 1); // Usuń ją z bufora

                Logger.logInfo($"Received Data: ", fullLine);

                processSerialData(fullLine);
            }
        }
        catch (Exception ex)
        {
            Logger.logError($"Błąd odczytu -> {ex.Message}");
        }
    }


    private static void processSerialData(string data)
    {
        string[] split = data.Split(':');
        if (split.Length != 2) return;

        int index = int.Parse(split[0]);
        float value = float.Parse(split[1]);

        oldProcessedData[index] = processedData[index];

        if (!root.InvertSliders) processedData[index] = value;
        else processedData[index] = 100.0f - value;

        if (processedData[index] != oldProcessedData[index])
        {
            if (parsedApps[index] is List<object> appList)
                foreach (string app in appList)
                    switch (app.ToLower())
                    {
                        case "master":
                            setMasterVolume(processedData[index]);
                            break;
                        case "mic":
                            setMicVolume(processedData[index]);
                            break;
                        case "activewindow":
                            setActiveWindowVolume(activeAppPID, processedData[index]);
                            break;
                        default:
                            setAppVolume(app, processedData[index]);
                            break;
                    }

            else if (parsedApps[index] is string app)
                switch (app.ToLower())
                {
                    case "master":
                        setMasterVolume(processedData[index]);
                        break;
                    case "mic":
                        setMicVolume(processedData[index]);
                        break;
                    case "activewindow":
                        setActiveWindowVolume(activeAppPID, processedData[index]);
                        break;
                    default:
                        setAppVolume(app, processedData[index]);
                        break;
                }
        }


        //TODO: Auto Reconnect
    }


    private static void setAppVolume(string app, float volume)
    {
        if (volume < 0.0f || volume > 100.0f)
            return;

        string normalizedAppName = app.ToLower();

        try
        {
            // Iteracja przez sesje audio
            foreach (IAudioSession session in playbackDevice.SessionController.All())
            {
                Process session_process = Process.GetProcessById(session.ProcessId);
                string sessName = session_process.ProcessName.ToLower();

                if (normalizedAppName == sessName)
                    session.Volume = volume;
            }
        }
        catch (Exception ex)
        {
            Logger.logError($"Error while retrieving process -> {ex.Message}");
        }
    }


    private static void setMasterVolume(float volume)
    {
        if (volume < 0.0f || volume > 100.0f)
            return;

        playbackDevice.Volume = volume;
    }


    private static void setMicVolume(float volume)
    {
        if (volume < 0.0f || volume > 100.0f)
            return;

        captureDevice.Volume = volume;
    }


    private static void setActiveWindowVolume(uint pid, float volume)
    {
        if (volume < 0.0f || volume > 100.0f)
            return;

        IntPtr hwnd = GetForegroundWindow(); // Pobranie uchwytu do aktywnego okna
        GetWindowThreadProcessId(hwnd, out uint active_app_pid); // Pobranie PID procesu związanego z aktywnym oknem
        activeAppPID = active_app_pid;

        try
        {
            // Iteracja przez sesje audio
            foreach (IAudioSession session in playbackDevice.SessionController.All())
            {
                string sessionName = Process.GetProcessById(session.ProcessId).ProcessName.ToLower();

                if (!parsedAppsString.Contains(sessionName))
                    if (session.ProcessId == activeAppPID)
                        session.Volume = volume;
            }
        }
        catch (Exception ex)
        {
            Logger.logError($"Error while retrieving process -> {ex.Message}");
        }
    }


    public static bool initConfig()
    {
        initCfg = false;

        try
        {
            // Odczytanie pliku konfiguracyjnego YAML
            var yaml = File.ReadAllText(configPath);
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            // Deserializacja pliku YAML do obiektu Config
            root = deserializer.Deserialize<Config>(yaml);
            Logger.logInfo($"Port: ", root.Port);
            Logger.logInfo($"Baud-rate: ", root.Baudrate.ToString());
            Logger.logInfo($"Invert Sliders: ", root.InvertSliders.ToString());
            Logger.logInit("Config");

            // Dodanie aplikacji do listy
            parsedApps.Clear();
            foreach (object app in root.Apps)
            {
                if (app is Dictionary<object, object> groupDict)
                    foreach (object key in groupDict.Keys)
                        parsedApps.Add(groupDict[key]);
                else
                    parsedApps.Add(app);
            }


            parsedAppsString = JsonConvert.SerializeObject(parsedApps).ToLower();
            Logger.logInfo("Parsed Apps: ", parsedAppsString.Substring(1, parsedAppsString.Length - 2));

            return true;
        }
        catch (Exception ex)
        {
            Logger.logError($"Error while initializing config -> {ex.Message}");
            MessageBox.Show($"Error while initializing config \n {ex.Message}", "Config Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        return false;
    }


    public static bool initSerialPort(string comPort, int baud)
    {
        _serialPort = new SerialPort();
        _serialPort.BaudRate = baud; // Ustawienie szybkości transmisji
        _serialPort.DtrEnable = true; // Włączenie DTR
        _serialPort.RtsEnable = true; // Włączenie RTS
        _serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);

        if (comPort == "AUTO")
        {
            // Pobierz wszystkie dostępne porty COM
            string[] ports = SerialPort.GetPortNames();

            Logger.logInfo("Avaliable ports: ", string.Join(", ", ports));

            foreach (string port in ports)
            {
                if (port == "COM1")
                    continue;

                try
                {
                    _serialPort.PortName = port; // Ustawienie nazwy portu
                    _serialPort.Open(); // Otwórz port
                    _serialPort.DiscardInBuffer();

                    Logger.logInfo("Found device on: ", port);
                    Logger.logInfo("Opened: ", port);

                    Logger.logInit("Serial Port");

                    return true;
                }

                catch (Exception ex)
                {
                    Logger.logError($"Error while opening port -> {ex.Message}");
                    return false;
                    //MessageBox.Show($"Error while opening Serial Port ${port} \n {ex.Message}", "Serial Port Error - AUTO", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        else
        {
            _serialPort.PortName = comPort;

            try
            {
                _serialPort.Open();

                Logger.logInfo("Opened: ", comPort);
                Logger.logInit("Serial Port");
                return true;
            }
            catch (Exception ex)
            {
                Logger.logError($"Error while opening port -> {ex.Message}");
                //MessageBox.Show($"Error while opening Serial Port \n {ex.Message}", "Serial Port Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

            }
        }

        return false; // Zwróć false, jeśli otwarcie portu się nie powiodło
    }


    public static void initAudioDevices()
    {
        if (playbackDevice != null)
            playbackDevice.Dispose();

        if (captureDevice != null)
            captureDevice.Dispose();


        // Inicjalizacja urządzeń audio
        playbackDevice = new CoreAudioController().DefaultPlaybackDevice;
        captureDevice = new CoreAudioController().DefaultCaptureDevice;

        Logger.logInfo("Playback device: ", playbackDevice.FullName);
        Logger.logInfo("Capture device: ", captureDevice.FullName);
        Logger.logInit("Audio Devices");
    }
}
