#include <stdio.h>
#include <fcntl.h>
#include <unistd.h>
#include <termios.h>
#include <errno.h>
#include <string.h>
#include <libevdev.h>
#include <libevdev-uinput.h>

//#define serial_file
#define DEBUG
//#define READTEST

#ifndef DEBUG
#define printf ignore_printf
void ignore_printf()
{
	return;
}
#endif

//arduino controller reader device count
//unmodified arduino code streams out data for up to 8 NES/SNES gamepads
#define DEVCOUNT 8
#define GETBIT(a,b) (((a) & (1 << (b))) ? 1 : 0)
#define false 0
#define true (!(false))

#ifdef serial_file
#define getbyte fgetc
#else
unsigned char getbyte(int port)
{
	unsigned char c;
	int result;

	//printf("Reading character.\n");
	do
	{
		result = read(port, &c, 1);
	}
	while (result < 1);
	//printf("Character read: %x.\n", c);

	return c;
}
#endif

int main(int argc, char **argv)
{
    int err[8];

    int i;
	struct libevdev *dev[DEVCOUNT];
	struct libevdev_uinput *uidev[DEVCOUNT];
	char devname[9] = { 'A', 'r', 'd', 'u', 'P', 'a', 'd', '0', '\0' };

	struct termios options;

	#ifdef serial_file
	FILE *arduino;
	#else
	int arduino;
	#endif
	char *serialport;
	
	//check for file path to open
	if (argc < 2)
	{
		printf("Usage: %s <filename>\n", argv[0]);
		return -1;
	}
	
	serialport = argv[1];
	
	//open file, no point in doing anything else until this is done
	printf("Opening port %s.\n", serialport);
	#ifdef serial_file
	arduino = fopen(serialport, "rwb+");
	#else
	arduino = open(serialport, O_RDWR | O_NOCTTY | O_NDELAY | O_NONBLOCK);
	if (arduino == -1)
	{
		printf("Couldn't open serial port.\n");
		return -1;
	}
	printf("Serial port handle: %i\n", arduino);
	fcntl(arduino, F_SETFL, 0);

	printf("Changing port settings...\n");
	printf("tcgetattr() = %i errno = %i - %s\n", tcgetattr(arduino, &options), errno, strerror(errno));
	
	cfmakeraw(&options);
	cfsetspeed(&options, B115200);

	printf("tcsetattr() = %i errno = %i - %s\n", tcsetattr(arduino, TCSANOW, &options), errno, strerror(errno)); //apply settings
	
	#endif

	#ifdef serial_file
	if (arduino == 0)
	#else
	if (arduino < 0)
	#endif
	{
		printf("Failed to open %s.\n", serialport);
		return -1;
	}

	printf("Handle: %i\n", arduino);

#ifdef READTEST
	// ---test
	do
	{
		char test[18];
		int result;
	
		result = read(arduino, test, 18);
	
		printf("read result: %i - errno %i - %s\n", result, errno, strerror(errno));
	
		for(int i = 0; i < 18; i++)
		{
			//test[i] = getbyte(arduino);
			printf("%0#2x ", test[i] & 0xFF);
		}
		printf("\n");
	
	} while (true);
	// ---test
	#endif

	printf("Serial port opened, creating devices...\n");
	 
	//create pseudo-devices	
	for (i = 0; i < DEVCOUNT; i++)
	{
		dev[i] = libevdev_new();
		devname[7] = '0' + i;
		libevdev_set_name(dev[i], devname);
		libevdev_enable_event_type(dev[i], EV_KEY);
		libevdev_enable_event_code(dev[i], EV_KEY, BTN_A, NULL);
		libevdev_enable_event_code(dev[i], EV_KEY, BTN_B, NULL);
		libevdev_enable_event_code(dev[i], EV_KEY, BTN_X, NULL);
		libevdev_enable_event_code(dev[i], EV_KEY, BTN_Y, NULL);
		libevdev_enable_event_code(dev[i], EV_KEY, BTN_START, NULL);
		libevdev_enable_event_code(dev[i], EV_KEY, BTN_SELECT, NULL);
		libevdev_enable_event_code(dev[i], EV_KEY, BTN_NORTH, NULL);
		libevdev_enable_event_code(dev[i], EV_KEY, BTN_EAST, NULL);
		libevdev_enable_event_code(dev[i], EV_KEY, BTN_SOUTH, NULL);
		libevdev_enable_event_code(dev[i], EV_KEY, BTN_WEST, NULL);
		libevdev_enable_event_code(dev[i], EV_KEY, BTN_TL, NULL);
		libevdev_enable_event_code(dev[i], EV_KEY, BTN_TR, NULL);
		
		err[i] = libevdev_uinput_create_from_device(dev[i], LIBEVDEV_UINPUT_OPEN_MANAGED, &uidev[i]);
		
		if (err[i] != 0)
		{
			printf("Failed to create device %s (%i): %i.\n", devname, i, err[i]);
		}
		else
		{
			printf("Create device %s (%i)\n", devname, i);
		}
	}

	printf("Device creation finished, feeding inputs.\n");
	
	unsigned char inputs[16];
	unsigned char prv_inputs[16];
	unsigned char changed_inputs[16];

	for(int i = 0; i < 16; i++)
	{
		inputs[i] = prv_inputs[i] = changed_inputs[i] = 0;
	}

	//main loop
	do
	{
		//Packet format:
		//     start       |    gamepad 1    |...|    gamepad 8
		//76543210 76543210|76543210 76543210|...|76543210 76543210
		//00000000 11111111|BYST^v<> AXLR----|...|BYST^v<> AXLR----
		
		//printf("Waiting for input packet.\n");

		//wait for 0x00 0xFF packet start sequence
		while(getbyte(arduino) != 0x00);
		while(getbyte(arduino) != 0xFF);

		//printf("Got input packet.\n");

		//read remaining 16 bytes of packet
		read(arduino, inputs, 16);

		//get info on which inputs have changed since last read
		for (int i = 0; i < 16; i++)
		{
			changed_inputs[i] = inputs[i] ^ prv_inputs[i];
			prv_inputs[i] = inputs[i];
		}

		for (int i = 0; i < DEVCOUNT; i++)
		{
			if (GETBIT(changed_inputs[i << 1], 0))
			{
				printf("Gamepad %i - > button state: %i\n", i, GETBIT(inputs[i << 1], 0));
				libevdev_uinput_write_event(uidev[i], EV_KEY, BTN_EAST, GETBIT(inputs[i << 1], 0));
			}

			if (GETBIT(changed_inputs[i << 1], 1))
			{
				printf("Gamepad %i - < button state: %i\n", i, GETBIT(inputs[i << 1], 1));
				libevdev_uinput_write_event(uidev[i], EV_KEY, BTN_WEST, GETBIT(inputs[i << 1], 1));
			}

			if (GETBIT(changed_inputs[i << 1], 2))
			{
				printf("Gamepad %i - v button state: %i\n", i, GETBIT(inputs[i << 1], 2));
				libevdev_uinput_write_event(uidev[i], EV_KEY, BTN_SOUTH, GETBIT(inputs[i << 1], 2));
			}

			if (GETBIT(changed_inputs[i << 1], 3))
			{
				printf("Gamepad %i - ^ button state: %i\n", i, GETBIT(inputs[i << 1], 3));
				libevdev_uinput_write_event(uidev[i], EV_KEY, BTN_NORTH, GETBIT(inputs[i << 1], 3));
			}

			if (GETBIT(changed_inputs[i << 1], 4))
			{
				printf("Gamepad %i - START button state: %i\n", i, GETBIT(inputs[i << 1], 4));
				libevdev_uinput_write_event(uidev[i], EV_KEY, BTN_START, GETBIT(inputs[i << 1], 4));
			}

			if (GETBIT(changed_inputs[i << 1], 5))
			{
				printf("Gamepad %i - SELECT button state: %i\n", i, GETBIT(inputs[i << 1], 5));
				libevdev_uinput_write_event(uidev[i], EV_KEY, BTN_START, GETBIT(inputs[i << 1], 5));
			}

			if (GETBIT(changed_inputs[i << 1], 6))
			{
				printf("Gamepad %i - Y button state: %i\n", i, GETBIT(inputs[i << 1], 6));
				libevdev_uinput_write_event(uidev[i], EV_KEY, BTN_Y, GETBIT(inputs[i << 1], 6));
			}

			if (GETBIT(changed_inputs[i << 1], 7))
			{
				printf("Gamepad %i - B button state: %i\n", i, GETBIT(inputs[i << 1], 7));
				libevdev_uinput_write_event(uidev[i], EV_KEY, BTN_B, GETBIT(inputs[i << 1], 7));
			}

			if (GETBIT(changed_inputs[(i << 1) + 1], 4))
			{
				printf("Gamepad %i - R button state: %i\n", i, GETBIT(inputs[(i << 1) + 1], 4));
				libevdev_uinput_write_event(uidev[i], EV_KEY, BTN_TR, GETBIT(inputs[(i << 1) + 1], 4));
			}

			if (GETBIT(changed_inputs[(i << 1) + 1], 5))
			{
				printf("Gamepad %i - L button state: %i\n", i, GETBIT(inputs[(i << 1) + 1], 5));
				libevdev_uinput_write_event(uidev[i], EV_KEY, BTN_TL, GETBIT(inputs[(i << 1) + 1], 5));
			}

			if (GETBIT(changed_inputs[(i << 1) + 1], 6))
			{
				printf("Gamepad %i - X button state: %i\n", i, GETBIT(inputs[(i << 1) + 1], 6));
				libevdev_uinput_write_event(uidev[i], EV_KEY, BTN_X, GETBIT(inputs[(i << 1) + 1], 6));
			}

			if (GETBIT(changed_inputs[(i << 1) + 1], 7))
			{
				printf("Gamepad %i - A button state: %i\n", i, GETBIT(inputs[(i << 1) + 1], 7));
				libevdev_uinput_write_event(uidev[i], EV_KEY, BTN_A, GETBIT(inputs[(i << 1) + 1], 7));
			}
		}
	} while (true);
		
	//clean up devices if exiting
	for (i = 0; i < DEVCOUNT; i++)
	{
		if (err[i] != 0)
		{
			printf("Destroying dev %i.\n", i);
			libevdev_uinput_destroy(uidev[i]);
		}
	}
	
	printf("Terminated.\n");
}
