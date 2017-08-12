# eLED

A C# wrapper for controlling LimitlessLED/MiLight Warm White WiFi LED Bulbs.

# Usage

Instantiate an eLED object to control the controller, supplying the IP address of the controller.

`var eLED = new eLED("192.168.1.4");`

As with all MiLight controlled bulbs, commands are sent with a zone number 1-4,
indicating the zone of the bulb set in the app. 0 for all zones.

`eLED.turnOn(1);`

Brightness can be set 0 to 100.

`eLED.setBrightness(3, 50); `

Colors are adjusted from 0 to 255 with MiLight's color 
scale. 0 is violet.

`eLED.setColor(2, 125);`

Every command is pretty straightforward: command(zone) or command(zone, value)

`eLED.whiteMode(4);`

`eLED.turnOff(0);`

`eLED.setMode(2, 3);`

`eLED.nightMode(3);`

# Controller info

This interface for the MiLight controller is built to communicate with the controller as quickly 
as possible to allow for the creation of light shows and other entertaining effects. 
