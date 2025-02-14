// Uncomment if you want to enable LEDS
// #define ENABLE_LEDS 

const int NUM_SLIDERS = 5;
const int analogInputs[NUM_SLIDERS] = { A10, A3, A2, A1, A0 };  //Change to your pinout


#ifdef ENABLE_LEDS
#include <FastLED.h>

#define NUM_LEDS 6
#define DATA_PIN 9
CRGB leds[NUM_LEDS];

const long intervalChangeLEDS = 20;
unsigned long previousMillisChangeLEDS = 0;

void setColor(int r, int g, int b) {
  for (int i = 0; i < NUM_LEDS; i++) {
    leds[i] = CRGB(r, g, b);
  }
  FastLED.show();
}
#endif


const int hysteresis = 2;

const long intervalSendData = 100;
unsigned long previousMillisSendData = 0;

int analogSliderValues[NUM_SLIDERS];
int analogSliderValuesOldData[NUM_SLIDERS] = { -1 };


void setup() {
  for (int i = 0; i < NUM_SLIDERS; i++) {
    pinMode(analogInputs[i], INPUT);
  }

#ifdef ENABLE_LEDS
  FastLED.addLeds<WS2812B, DATA_PIN, GRB>(leds, NUM_LEDS);
  FastLED.setBrightness(255);
#endif

  Serial.begin(115200);

  while (!Serial)
#ifdef ENABLE_LEDS
    setColor(255, 0, 0);
#endif

  sendFirstValues();
}


void loop() {
  unsigned long currentMillis = millis();

  if (currentMillis - previousMillisSendData >= intervalSendData) {
    previousMillisSendData = currentMillis;
    updateSliderValues();
    sendSliderValues();
  }


#ifdef ENABLE_LEDS
  if (currentMillis - previousMillisChangeLEDS >= intervalChangeLEDS) {
    previousMillisChangeLEDS = currentMillis;
    static uint8_t startIndex = 0;
    fill_rainbow(leds, NUM_LEDS, startIndex, 10);
    FastLED.show();
    startIndex++;
  }
#endif
}


void updateSliderValues() {
  for (int i = 0; i < NUM_SLIDERS; i++) {
    analogSliderValues[i] = map(analogRead(analogInputs[i]), 0, 1020, 0, 100);
  }
}


void sendSliderValues() {
  String builtString = "";

  for (int i = 0; i < NUM_SLIDERS; i++) {
    int currentValue = analogSliderValues[i];
    int oldValue = analogSliderValuesOldData[i];

    bool isSignificantChange = abs(currentValue - oldValue) >= hysteresis;

    bool isEdgeValueChanged = (currentValue == 0 || currentValue == 100) && currentValue != oldValue;

    if (oldValue == -1 || isSignificantChange || isEdgeValueChanged) {
      builtString += String(i) + ":" + String(currentValue) + "\n";
      analogSliderValuesOldData[i] = currentValue;
    }
  }

  if (builtString.length() > 0)
    Serial.print(builtString);
}


void sendFirstValues() {
  String builtString = "";

  for (int i = 0; i < NUM_SLIDERS; i++) {
    int currentValue = analogSliderValues[i];
    builtString += String(i) + ":" + String(currentValue) + "\n";
    analogSliderValuesOldData[i] = currentValue;
  }

  if (builtString.length() > 0)
    Serial.print(builtString);
}
