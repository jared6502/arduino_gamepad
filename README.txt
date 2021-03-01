--------------------------------------------------
Simple Gamepad Interface Using an Arduino
--------------------------------------------------

This is a set of simple programs for attaching SNES controllers to a computer.

/sketch - the code that runs in the Arduino
/feeders - programs that run on the computer end to feed button press events to the OS

--------------------------------------------------
Arduino Sketch
--------------------------------------------------

Button name reference:
A - A
B - B
X - X
Y - Y
S - Select
T - Start
^ - Up
v - Down
< - Left
> - Right
L - L shoulder button
R - R shoulder button

	NES and SNES controllers are very simple devices. They consist of one or two (depending on controller type and age) shift register ICs with pullup resistors on the inputs that are shorted by carbon pads every time a button is pressed. They have three signals of importance - latch, clock, and data. These control the shift register IC(s) within the controller. A high pulse on the latch input stores the current state of the buttons into the shift register. After latching the current button state, data is shifted out bit by bit on the low-to-high transition of the clock signal. Each bit shifted out corresponds to the state of a button. NES controllers only have 8 inputs and will only transmit 8 bits, while SNES controllers transmit 12.

Button order:
     0123456789ABCDEF
NES  ABST^v<>--------
SNES BYST^v<>AXLR----


Example of timing - SNES gamepad, B, Y, and right held at the same time - note low = pressed

        ___
latch _|   |____________________________________________________________________________________________________
      _______    __    __    __    __    __    __    __    __    __    __    __    __    __    __    __
clock        |__|  |__|  |__|  |__|  |__|  |__|  |__|  |__|  |__|  |__|  |__|  |__|  |__|  |__|  |__|  |________
        |B       |Y    |S    |T    |^    |v    |<    |>    |A    |X    |L    |R    |-    |-    |-    |-
                        _____________________________       ____________________________________________________
data  xx_______________|                             |_____|

	While the button order above may seem like a significant change between NES and SNES (A/B becomes B/Y), it makes sense due to the physical button layout. It also means that NES and SNES controllers are interchangeable whenever the extra buttons are not needed, such as in games like Super Mario.

NES
 ^
< > - - B A
 v

SNES
  L         R
  ^         X
<   > - - Y   A
  v         B

  These bit streams are gathered from up to 8 different controllers using only ten I/O pins by sharing the clock and latch signals across all controller ports. Once data is gathered from all controllers, it is streamed out through the serial port. A "packet" of input data normally starts with 0x00 0xFF, as this is not a button combination that is possible on the majority of NES/SNES gamepads and their clones. It is then followed by eight 16-bit words of data in the following format:
  
FEDCBA98 76543210
----RLXA ><v^TSYB

In this case, the bits are 1 = pressed, 0 = not pressed, which is the opposite of the raw input signal state.

--------------------------------------------------
Feeders
--------------------------------------------------

/feeders/windows
Contains a simple feeder program made from the vJoy feeder example. It reads the button inputs from the serial port and feeds them to the vJoy driver. It requires the installation of the vJoy driver to function. Building and using it may require installing a specific version of the driver and/or SDK. This feeder was originally built and tested with vJoy 2.1.8 on both Windows 7 and Windows 10.

/feeders/unix
Contains a simple C program that reads the button inputs from the serial port and feeds them to the OS using libevdev. 
