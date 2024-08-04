using NAudio.CoreAudioApi;
using System.Diagnostics;
using System.IO.Ports;
using yamlConfig;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

//Propreties -> Output type -> [Windows Application]

class Program
{
    public static bool initSerial = false;
    public static bool initAudio = false;
    public static bool initCfg = false;
    public static bool programExit = false;

    private static SerialPort _serialPort; // Obiekt do komunikacji przez port szeregowy
    public static Config root; // Obiekt do przechowywania konfiguracji

    private static MMDeviceEnumerator deviceEnumerator;
    private static MMDevice renderDevice;
    private static MMDevice captureDevice;
    private static AudioSessionManager sessionManager;

    private static String oldData = "";

    public static string roamingAppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    public static string configPath = Path.Combine(roamingAppData, "Turbina4Software\\config.yaml");
    public static string iconPath = Path.GetFullPath("mixerLogo.ico");


    private static void Main(string[] args)
    {

        // Inicjalizacja SystemTray w oddzielnym wątku
        Thread trayHandlerThread = new Thread(() =>
        {
            systemTray.TrayHandler.Init();
        });

        // Ustaw wątek jako MTA (Multi-Threaded Apartment)
        trayHandlerThread.SetApartmentState(ApartmentState.MTA);
        trayHandlerThread.Start();

        // Inicjalizacja konfiguracji z pliku YAML
        initCfg = initConfig();


        // Inicjalizacja portu szeregowego
        if (initCfg)
            initSerial = initSerialPort(root.Port, root.Baudrate);

        // Inicjalizacja urządzenia audio
        initAudio = initAudioDevice();

        mainLoop();
    }

    public static void mainLoop()
    {
        if (initSerial)
        {
            // Główna pętla programu

            while (!programExit)
            {
                _serialPort.DiscardInBuffer(); // Opróżnij bufor wejściowy
                string data = _serialPort.ReadLine(); // Odczytaj linię danych z portu szeregowego

                if (data == oldData)
                    continue;

                oldData = data;
                Console.WriteLine(data);

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
                        // Jeśli aplikacja to słownik, iteruj przez jego elementy
                        foreach (var group in groupDict)
                        {
                            foreach (var groupName in (List<object>)group.Value)
                            {
                                setAppVolume(groupName.ToString(), values[i]); // Ustaw głośność dla aplikacji w grupie
                            }
                        }
                    }
                }

                Thread.Sleep(100); // Krótkie opóźnienie przed kolejnym odczytem
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


    private static List<float> ConvertStringToFloatList(string input)
    {
        // Konwersja ciągu znaków na listę floatów
        string[] parts = input.Split('|'); // Rozdzielenie danych na części
        List<float> floatList = new List<float>();

        foreach (var part in parts)
        {
            if (float.TryParse(part, out float value)) // Próbuj parsować ciąg do float
            {
                floatList.Add(value / 100); // Dodanie wartości do listy, dzieląc przez 100
            }
        }
        return floatList; // Zwróć listę floatów
    }


    public static bool initAudioDevice()
    {
        try
        {
            deviceEnumerator = new MMDeviceEnumerator();
            renderDevice = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            captureDevice = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Multimedia);
            sessionManager = renderDevice.AudioSessionManager;

            Console.WriteLine("Initalized deviceEnumerator");

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error initializing audio device: " + ex.Message);
            MessageBox.Show($"Error during Audio device initialization \n {ex}", "Audio Initialize Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

        }

        return false;
    }

    private static void setAppVolume(string app, float volume)
    {
        if (!initAudio || volume < 0.0f || volume > 1.0f)
            return;

        sessionManager.RefreshSessions();

        try
        {
            // Iteracja przez sesje audio
            for (int i = 0; i < sessionManager.Sessions.Count; i++)
            {
                if (app.ToLower() == "master")
                {
                    renderDevice.AudioEndpointVolume.MasterVolumeLevelScalar = volume;
                    return;
                }

                if (app.ToLower() == "mic")
                {
                    captureDevice.AudioEndpointVolume.MasterVolumeLevelScalar = volume;
                    return;
                }

                AudioSessionControl session = sessionManager.Sessions[i];
                int processId = Convert.ToInt32(session.GetProcessID);


                // Sprawdź, czy proces to {app}
                if (Process.GetProcessById(processId).ProcessName.ToLower().Equals(app.ToLower(), StringComparison.OrdinalIgnoreCase))
                {
                    // Ustaw głośność na {volume}
                    session.SimpleAudioVolume.Volume = volume;

                    //Console.WriteLine($"Głośność aplikacji {app} ustawiona na {volume * 100}%"); // Debug info
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
