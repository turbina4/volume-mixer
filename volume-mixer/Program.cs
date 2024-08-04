using AudioSwitcher.AudioApi.CoreAudio;
using AudioSwitcher.AudioApi.Session;
using System.Diagnostics;
using System.IO.Ports;
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

    private static CoreAudioDevice playbackDevice;
    private static CoreAudioDevice captureDevice;

    public static string roamingAppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    public static string configPath = Path.Combine(roamingAppData, "Turbina4Software\\config.yaml");
    public static string iconPath = Path.GetFullPath("mixerLogo.ico");


    private static void Main(string[] args)
    {
        Thread trayHandlerThread = new Thread(() =>
        {
            systemTray.TrayHandler.Init();
        });

        // Ustaw wątek jako MTA (Multi-Threaded Apartment)
        trayHandlerThread.SetApartmentState(ApartmentState.MTA);
        trayHandlerThread.Start();

        // Inicjalizacja
        initCfg = initConfig();
        if (initCfg)
            initSerial = initSerialPort(root.Port, root.Baudrate);
        initAudioDevices();

        mainLoop();
    }

    public static void mainLoop()
    {
        if (initSerial)
        {
            String oldData = "";

            while (!programExit)
            {
                _serialPort.DiscardInBuffer(); // Opróżnij bufor wejściowy
                string data = _serialPort.ReadLine(); // Odczytaj linię danych z portu szeregowego

                if (data == oldData)
                    continue;

                oldData = data;

                //Console.WriteLine(data);

                // Konwersja odczytanych danych na listę floatów
                List<float> values = ConvertStringToFloatList(data);

                // Jeśli liczba odczytanych wartości nie jest równa root.Apps.Count, przejdź do następnej iteracji
                if (values.Count != root.Apps.Count)
                    continue;

                // Ustawienie głośności dla aplikacji na podstawie odczytanych wartości
                for (int i = 0; i < root.Apps.Count; i++)
                {
                    if (root.Apps[i] is string appName)
                    {
                        setAppVolume(appName, values[i]); // Ustaw głośność dla aplikacji
                    }

                    else if (root.Apps[i] is Dictionary<object, object> groupDict)
                    {
                        var groupNames = groupDict.Values.OfType<List<object>>()
                                     .SelectMany(group => group)
                                     .ToList();

                        foreach (var groupName in groupNames)
                        {
                            setAppVolume(groupName.ToString(), values[i]); // Ustaw głośność
                        }
                    }
                }

                Thread.Sleep(125); // Krótkie opóźnienie przed kolejnym odczytem
            }
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

            // Deserializacja pliku YAML do obiektu Root
            root = deserializer.Deserialize<Config>(yaml);
            Console.WriteLine("Initalized Config");
            Console.WriteLine($"Port: {root.Port}");
            Console.WriteLine($"Baudrate: {root.Baudrate}");

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            MessageBox.Show($"Error while initializing config \n {ex.Message}", "Config Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }


        return false;
    }


    public static bool initSerialPort(string comPort, int baud)
    {
        // Inicjalizacja portu szeregowego
        _serialPort = new SerialPort();
        _serialPort.PortName = comPort; // Ustawienie nazwy portu
        _serialPort.BaudRate = baud; // Ustawienie szybkości transmisji

        _serialPort.DtrEnable = true; // Włączenie DTR
        _serialPort.RtsEnable = true; // Włączenie RTS

        try
        {
            _serialPort.Open(); // Otwarcie portu
            Console.WriteLine("Initalized Serial Port");
            return true; // Zwróć true, jeśli otwarcie się powiodło
        }
        catch (Exception ex)
        {
            _serialPort.Close(); // Zamknięcie portu w przypadku błędu
            Console.WriteLine($"Failed to open Serial Port"); // Informacja o błędzie
            MessageBox.Show($"Error while opening Serial Port \n {ex.Message}", "Serial Port Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

        }

        return false; // Zwróć false, jeśli otwarcie portu się nie powiodło
    }


    public static void DisposeSerialPort()
    {
        if (_serialPort != null)
        {
            _serialPort.Close();
            _serialPort.Dispose();
            _serialPort = null;
        }
    }

    public static void initAudioDevices()
    {
        playbackDevice = new CoreAudioController().DefaultPlaybackDevice;
        captureDevice = new CoreAudioController().DefaultCaptureDevice;
    }

    
    private static List<float> ConvertStringToFloatList(string input)
    {

        // Konwersja ciągu znaków na listę floatów
        string[] parts = input.Split('|'); // Rozdzielenie danych na części
        List<float> floatList = new List<float>();

        foreach (var part in parts)
        {
            if (float.TryParse(part, out float value)) // Próbuj parsować ciąg do float
            {
                floatList.Add(value); // Dodanie wartości do listy, dzieląc przez 100
            }
        }
        return floatList; // Zwróć listę floatów
    }


    private static void setAppVolume(string app, float volume)
    {
        if (volume < 0.0f || volume > 100.0f)
            return;

        string normalizedAppName = app.ToLower();

        if (normalizedAppName == "master")
        {
            playbackDevice.Volume = volume;
            return;
        }

        if (app.ToLower() == "mic")
        {
            captureDevice.Volume = volume;
            return;
        }

        try
        {
            // Iteracja przez sesje audio
            foreach (IAudioSession session in playbackDevice.SessionController.All())
            {
                Process session_process = Process.GetProcessById(session.ProcessId);
                string sessName = session_process.ProcessName.ToLower();

                if (normalizedAppName == sessName)
                {
                    session.Volume = volume;
                    return; // Zakończ, gdy głośność została ustawiona
                }

            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error while retrieving process: {ex.Message}"); // Informacja o błędzie
        }
    }
}
