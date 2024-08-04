# Arduino-Based Volume Mixer Controller

## Overview

This application allows you to control the volume of various applications on your computer using an Arduino and potentiometers. By adjusting the potentiometers, you can easily control the volume levels of specified applications or system sounds like master volume and microphone volume.
Components

![Mixer Photo](Media/Mixer.jpg)

Arduino Board: The microcontroller to read analog signals from the potentiometers.
Potentiometers: Analog input devices to adjust volume levels. Each potentiometer corresponds to a different application or sound control.
Serial Communication: Used to send potentiometer values from the Arduino to the computer.

## Schematic

![Schematic img](Media/Schematic.png)

## Configuration

The configuration file (config.yaml) specifies the settings for your COM port, baud rate, and the applications you want to control. Here's a sample configuration:

```yaml
# How to edit the config:
# port: Check your COM port in Device Manager
# baudrate: Same Baud Rate as the one in arduinoCode.ino
# apps: List your apps here
#       Check the application name in Task Manager -> Details
#       App names are not case sensitive
#       Special controls: master - controls master volume
#                         mic - controls your microphone volume
# group: Group multiple apps together
#        Group names are not case sensitive

port: COM7
baudrate: 57600
apps:
    - master
    - firefox
    - AMPLibraryManager
    - Discord
    - group:
          - PathOfExileSteam
          - cs2
          - GeometryDash
          - RustClient
```
## Instalation

Download `setup.exe` or `Mixer.Software.msi` and install the program.

## Usage

Connect the Arduino to your computer and upload the provided sketch.
Configure the application using the provided config.yaml file, ensuring the COM port and baud rate match those in the Arduino sketch.
Run the application on your computer. It will read the potentiometer values from the Arduino and adjust the specified application volumes accordingly.

![Options img](Media/Options.png)

## Special Controls

master: Controls the overall system volume.
mic: Controls the microphone volume.

By grouping applications together, you can control the volume of multiple applications simultaneously. This is useful for managing game audio, communication apps, and other sound sources efficiently.