uint8_t inputdata[18];

//#define DEBUG

#define Latch 2
#define Clock 3
#define Ctrl1 4
#define Ctrl2 5
#define Ctrl3 6
#define Ctrl4 7
#define Ctrl5 8
#define Ctrl6 9
#define Ctrl7 10
#define Ctrl8 11
#define Spare1 12
#define Spare2 13

#define LatchDelay 24
#define ClockPulseWidth 24

void setup()
{
  delay(1000);
  pinMode(Latch, OUTPUT);
  pinMode(Clock, OUTPUT);
  pinMode(Ctrl1, INPUT_PULLUP);
  pinMode(Ctrl2, INPUT_PULLUP);
  pinMode(Ctrl3, INPUT_PULLUP);
  pinMode(Ctrl4, INPUT_PULLUP);
  pinMode(Ctrl5, INPUT_PULLUP);
  pinMode(Ctrl6, INPUT_PULLUP);
  pinMode(Ctrl7, INPUT_PULLUP);
  pinMode(Ctrl8, INPUT_PULLUP);
  pinMode(Spare1, INPUT_PULLUP);
  pinMode(Spare2, INPUT_PULLUP);

  Serial.begin(115200, SERIAL_8N2);
}

void LatchInputs()
{
  //toggle latch
  digitalWrite(Latch, HIGH);
  #ifdef DEBUG
  //Serial.print("latch high\r\n");
  //delay(1000);
  #endif
  delayMicroseconds(LatchDelay);
  digitalWrite(Latch, LOW);
  #ifdef DEBUG
  //Serial.print("latch low\r\n");
  //delay(1000);
  #endif
  delayMicroseconds(LatchDelay);
}

void ClockInData()
{
  digitalWrite(Clock, LOW);
  #ifdef DEBUG
  //Serial.print("clock low\r\n");
  //delay(1000);
  #endif
  delayMicroseconds(ClockPulseWidth);
  digitalWrite(Clock, HIGH);
  #ifdef DEBUG
  //Serial.print("clock high\r\n");
  //delay(1000);
  #endif
  delayMicroseconds(ClockPulseWidth);
}

void loop()
{
  //NES/SNES controller to USB adapter code
  //supports 8 controllers
  //sends back to PC as 1=button pressed, 0=not pressed
  //one pair of bytes is sent for each controller
  //total of 16 bytes
  //has a 1 second startup delay, must use this to reset and sync on PC end

  //first, clear the input buffer of any data
  inputdata[0] = 0x00;
  inputdata[1] = 0xFF;
  for (int i = 2; i < 18; i++)
  {
    //inputdata[i] = 0;
  }

#ifdef DEBUG
  Serial.print("------------\r\n");
#endif

  LatchInputs();

  //shift in 16 bits of data from all 8 ports
  //could do only 12, but doing all 16 for max compatibility
  for (int i = 0; i < 8; i++)
  {
    inputdata[2] = (inputdata[2] << 1) | (digitalRead(Ctrl1) ? 0 : 1);
    inputdata[4] = (inputdata[4] << 1) | (digitalRead(Ctrl2) ? 0 : 1);
    inputdata[6] = (inputdata[6] << 1) | (digitalRead(Ctrl3) ? 0 : 1);
    inputdata[8] = (inputdata[8] << 1) | (digitalRead(Ctrl4) ? 0 : 1);
    inputdata[10] = (inputdata[10] << 1) | (digitalRead(Ctrl5) ? 0 : 1);
    inputdata[12] = (inputdata[12] << 1) | (digitalRead(Ctrl6) ? 0 : 1);
    inputdata[14] = (inputdata[14] << 1) | (digitalRead(Ctrl7) ? 0 : 1);
    inputdata[16] = (inputdata[16] << 1) | (digitalRead(Ctrl8) ? 0 : 1);
    ClockInData();
  }

  for (int i = 0; i < 8; i++)
  {
    inputdata[3] = (inputdata[3] << 1) | (digitalRead(Ctrl1) ? 0 : 1);
    inputdata[5] = (inputdata[5] << 1) | (digitalRead(Ctrl2) ? 0 : 1);
    inputdata[7] = (inputdata[7] << 1) | (digitalRead(Ctrl3) ? 0 : 1);
    inputdata[9] = (inputdata[9] << 1) | (digitalRead(Ctrl4) ? 0 : 1);
    inputdata[11] = (inputdata[11] << 1) | (digitalRead(Ctrl5) ? 0 : 1);
    inputdata[13] = (inputdata[13] << 1) | (digitalRead(Ctrl6) ? 0 : 1);
    inputdata[15] = (inputdata[15] << 1) | (digitalRead(Ctrl7) ? 0 : 1);
    inputdata[17] = (inputdata[17] << 1) | (digitalRead(Ctrl8) ? 0 : 1);
    ClockInData();
  }
 

  //send raw data to PC via serial to USB
  #ifndef DEBUG
  Serial.write(inputdata, 18);
  #endif

  //wait a bit to give the PC time to read it
  #ifdef DEBUG
  for (int i = 0; i < 8; i++)
  {
    Serial.print("controller ");
    Serial.print(i);
    Serial.print(": ");
    Serial.print(inputdata[(i<<1)+2] & 0x01 ? "B" : "-");
    Serial.print(inputdata[(i<<1)+2] & 0x02 ? "Y" : "-");
    Serial.print(inputdata[(i<<1)+2] & 0x04 ? "S" : "-");
    Serial.print(inputdata[(i<<1)+2] & 0x08 ? "T" : "-");
    Serial.print(inputdata[(i<<1)+2] & 0x10 ? "^" : "-");
    Serial.print(inputdata[(i<<1)+2] & 0x20 ? "v" : "-");
    Serial.print(inputdata[(i<<1)+2] & 0x40 ? "<" : "-");
    Serial.print(inputdata[(i<<1)+2] & 0x80 ? ">" : "-");
    Serial.print(inputdata[(i<<1)+3] & 0x01 ? "A" : "-");
    Serial.print(inputdata[(i<<1)+3] & 0x02 ? "X" : "-");
    Serial.print(inputdata[(i<<1)+3] & 0x04 ? "L" : "-");
    Serial.print(inputdata[(i<<1)+3] & 0x08 ? "R" : "-");
    Serial.print(inputdata[(i<<1)+3] & 0x10 ? "x" : "-");
    Serial.print(inputdata[(i<<1)+3] & 0x20 ? "x" : "-");
    Serial.print(inputdata[(i<<1)+3] & 0x40 ? "x" : "-");
    Serial.print(inputdata[(i<<1)+3] & 0x80 ? "x" : "-");
    Serial.print("\r\n");
  }
  
  delay(1000);
  #else
  delay(2);
  #endif
}
