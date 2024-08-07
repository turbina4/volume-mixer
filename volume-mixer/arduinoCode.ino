const int NUM_SLIDERS = 5;
const int analogInputs[NUM_SLIDERS] = { A0, A1, A2, A3, A4 }; //Change it to analog pins on your arduino

int analogSliderValues[NUM_SLIDERS];

unsigned long previousMillisSendData = 0;
const long intervalSendData = 50;

void setup() {
  for (int i = 0; i < NUM_SLIDERS; i++) {
    pinMode(analogInputs[i], INPUT);
  }

  Serial.begin(57600);
}

void loop() {
  unsigned long currentMillis = millis();

  if (currentMillis - previousMillisSendData >= intervalSendData) {
    previousMillisSendData = currentMillis;
    updateSliderValues();
    sendSliderValues();
  }
}

void updateSliderValues() {
  for (int i = 0; i < NUM_SLIDERS; i++) {
    analogSliderValues[i] = map(analogRead(analogInputs[i]), 0, 1020, 0, 100) ;
  }
}

void sendSliderValues() {
  String builtString = String("Mx31|");

  for (int i = 0; i < NUM_SLIDERS; i++) {
    builtString += String((int)analogSliderValues[i]);

    if (i < NUM_SLIDERS - 1) {
      builtString += String("|");
    }
  }

  Serial.println(builtString);
}