#include <Arduino.h>
#include <Stepper.h>
#include <HX711.h>
#include <TimerOne.h>


// Master globals
void ProcessCommand(String);
void Start(bool b = false);
void Stop(bool b = false);

// Conveyor belt globals
const uint8_t   pin_conv_pulse = 3; // PUL- pin
const uint8_t   pin_conv_dir = 2;   // DIR- pin
const uint8_t   conv_dir = 1;       // Set Direction
void SetBeltSpeed(int);
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
  
  Timer1.initialize(1000000);           // Init conveyor timer with garbage
  Timer1.attachInterrupt(ISR_DriveBelt);// Attatch ISR
  Timer1.stop();                        // Stow timer for now
}

// Main loop
void loop() 
{
  // Serial read
  if (Serial.available()) ProcessCommand(Serial.readStringUntil('\n'));
}


// Start and stop the conveyor sequence
void Start(bool announce = false)
{
  nut_current = 0;
  nut_stopped = false;
  SetBeltSpeed(2);
  if (announce) Serial.println("STARTED");
}
void Stop(bool announce = false)
{
  SetBeltSpeed(0);
  if (announce) Serial.println("STOPPED");
  delay(300);
}

// Update the belt speed
void SetBeltSpeed(int spd)
{
  switch (spd)
  {
    case 1 : 
      Timer1.setPeriod(200); 
      Serial.println("CONV 1");
      break;
    
    case 2 : 
      Timer1.setPeriod(100); 
      Serial.println("CONV 2");
      break;
    
    case 0 :
    default: 
      Timer1.stop(); 
      Serial.println("CONV 0");
      return;
  }
  Timer1.start();
}

// Belt drive ISR
void ISR_DriveBelt() { digitalWrite(pin_conv_pulse,!digitalRead(pin_conv_pulse)); }

// Process commands from Serial
void ProcessCommand(String msg)
{
  msg.replace("\r", "");
  msg.replace("\n", "");

  // Stop
  if (msg == "STOP") Stop(true);

  // Ping Pong
  else if (msg == "PING") Serial.println("PONG");

  // Conveyor belt
  else if (msg == "CONV,0") SetBeltSpeed(0);
  else if (msg == "CONV,1") SetBeltSpeed(1);
  else if (msg == "CONV,2") SetBeltSpeed(2);
  // Start the system
  else if (msg == "START") Start(true);


  // unknown
  else Serial.println("NACK");
}