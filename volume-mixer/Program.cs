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

    private static CoreAudioDevice playbackDevice; // Urządzenie do odtwarzania dźwięku
    private static CoreAudioDevice captureDevice; // Urządzenie do nagrywania dźwięku

    public static string roamingAppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    public static string configPath = Path.Combine(roamingAppData, "Turbina4Software\\config.yaml");
    public static string iconPath = Path.GetFullPath("mixerLogo.ico");


    private static void Main(string[] args)
    {
        // Utwórz i uruchom wątek dla obsługi ikony w zasobniku systemowym
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
            List<float> oldValues = new List<float>();



            while (!programExit)
            {
                _serialPort.DiscardInBuffer(); // Opróżnij bufor wejściowy
                string data = _serialPort.ReadLine(); // Odczytaj linię danych z portu szeregowego

                if (data == oldData)
                    continue;

                oldData = data;

                // Konwersja odczytanych danych na listę floatów
                List<float> values = ConvertStringToFloatList(data);


                // Jeśli liczba odczytanych wartości nie jest równa root.Apps.Count, przejdź do następnej iteracji
                if (values.Count != root.Apps.Count)
                    continue;

                // Ustawienie głośności dla aplikacji na podstawie odczytanych wartości
                for (int i = 0; i < root.Apps.Count; i++)
                {
                    if (oldValues.Count == values.Count)
                    {
                        if (values[i] == oldValues[i])
                            continue;
                    }


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

                oldValues = values;
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

            // Deserializacja pliku YAML do obiektu Config
            root = deserializer.Deserialize<Config>(yaml);
            Console.WriteLine("Initalized Config");
            Console.WriteLine($"Port: {root.Port}");
            Console.WriteLine($"Baud-rate: {root.Baudrate}");
            Console.WriteLine($"Invert Sliders: {root.InvertSliders}");

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
        _serialPort = new SerialPort();
        _serialPort.BaudRate = baud; // Ustawienie szybkości transmisji
        _serialPort.DtrEnable = true; // Włączenie DTR
        _serialPort.RtsEnable = true; // Włączenie RTS

        if (comPort == "AUTO")
        {
            // Pobierz wszystkie dostępne porty COM
            string[] ports = SerialPort.GetPortNames();

            foreach (string port in ports)
            {
                if (port == "COM1")
                    continue;

                try
                {
                    _serialPort.PortName = port; // Ustawienie nazwy portu
                    _serialPort.Open(); // Otwórz port

                    Console.WriteLine("Opened: " + port);

                    _serialPort.DiscardInBuffer();
                    string response = _serialPort.ReadLine();

                    // Sprawdź, czy odpowiedź to unikalny identyfikator
                    if (response.Contains("Mx31"))
                    {
                        Console.WriteLine("Znaleziono Arduino na porcie: " + port);
                        return true;
                        // Możesz tutaj dodać kod, który będzie działać po wykryciu Arduino
                    }

                    _serialPort.Dispose();
                }

                catch (Exception ex)
                {
                    Console.WriteLine("Błąd na porcie: " + port + " - " + ex.Message);
                    MessageBox.Show($"Error while opening Serial Port ${port} \n {ex.Message}", "Serial Port Error - AUTO", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        else
        {
            _serialPort.PortName = comPort; // Ustawienie nazwy portu

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
        }

        return false; // Zwróć false, jeśli otwarcie portu się nie powiodło
    }


    public static void initAudioDevices()
    {
        // Inicjalizacja urządzeń audio
        playbackDevice = new CoreAudioController().DefaultPlaybackDevice;
        captureDevice = new CoreAudioController().DefaultCaptureDevice;

        Console.WriteLine("Initalized Audio Devices");
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
                if (!root.InvertSliders)
                    floatList.Add(value);
                else
                    floatList.Add(100.0f - value);
            }
        }
        return floatList; // Zwróć listę floatów
    }


    private static void setAppVolume(string app, float volume)
    {
        //Console.WriteLine($"{app} -> {volume}");
        if (volume < 0.0f || volume > 100.0f)
            return;

        string normalizedAppName = app.ToLower();

        if (normalizedAppName == "master")
        {
            playbackDevice.Volume = volume;
            return;
        }

        if (normalizedAppName == "mic")
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
                    continue;
                }

            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error while retrieving process: {ex.Message}"); // Informacja o błędzie
        }
    }
}
