#include <Arduino.h>
#include <Stepper.h>
#include <TimerOne.h>


// Master globals
void ProcessCommand(String);
// Conveyor belt globals
const uint8_t   pin_conv_pulse = 3; // PUL- pin
const uint8_t   pin_conv_dir = 6;   // DIR- pin
const uint8_t   conv_dir = 1;       // Set Direction
uint32_t conv_count = 0;
uint16_t conv_period = 100;
bool conv_reverse = false;
void ISR_DriveBelt();


// Setup all our globals and such
void setup() 
{
  // Init serial
  Serial.begin(115200);
  while (!Serial);

  // Init conveyor belt
  pinMode (pin_conv_pulse, OUTPUT);     // Pulse pin as output
  pinMode (pin_conv_dir, OUTPUT);       // Direction pin as output
  digitalWrite(pin_conv_dir,conv_dir);  // Direction pin write high

  //ENABLE
  pinMode(8,OUTPUT);
  digitalWrite(8,HIGH);
  
  Timer1.initialize(conv_period);           // Init conveyor timer with garbage
  Timer1.attachInterrupt(ISR_DriveBelt);// Attatch ISR
  Timer1.setPeriod(conv_period);
  Timer1.start();
}

// Main loop
void loop() 
{
  // Serial read
  if (Serial.available()) ProcessCommand(Serial.readStringUntil('\n'));
}

// Belt drive ISR
void ISR_DriveBelt() 
{ 
  if (conv_count == 0) return;

  digitalWrite(
    pin_conv_dir,
    conv_reverse ? 1 : 0);

  digitalWrite(
    pin_conv_pulse, 
    !digitalRead(pin_conv_pulse));

  if (--conv_count == 0) Serial.println("DONE");
}

// Process commands from Serial
void ProcessCommand(String msg)
{
  String message = "";
  String tempnum = "";
  int number = 0;

  int index = msg.indexOf(",");              // locate the first ","
  message += msg.substring(0, index).c_str(); // Extract the string from start to the ","
  tempnum += msg.substring(index + 1).c_str();        // Remove what was before the "," from the string
  number = tempnum.toInt();

  msg.replace("\r", "");
  msg.replace("\n", "");
  

  // Stop
  if (message == "STOP")
  {
    conv_count = 0;
    Serial.println("STOP");
  }

  else if (message == "FOR")
  {
    conv_count = number*100;
    conv_reverse = false;
    Serial.println("FOR " + String(number));
  }

  else if (message == "REV") 
  {
    conv_count = number*100;
    conv_reverse = true;
    Serial.println("REV " + String(number));
  }

  else 
  {
    Serial.println("NACK");
    }
}