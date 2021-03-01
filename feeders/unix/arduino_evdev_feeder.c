#include <stdio.h>
#include <libevdev.h>
#include <libevdev-uinput.h>

//arduino controller reader device count
//unmodified arduino code streams out data for up to 8 NES/SNES gamepads
#define DEVCOUNT 8

int main(int argc, char **argv)
{
    int err[8];

    //libevdev_uinput_write_event(uidev, EV_KEY, KEY_A, 1);
    //libevdev_uinput_write_event(uidev, EV_SYN, SYN_REPORT, 0);
    //libevdev_uinput_write_event(uidev, EV_KEY, KEY_A, 0);
    //libevdev_uinput_write_event(uidev, EV_SYN, SYN_REPORT, 0);
	
	int i;
	struct libevdev *dev[DEVCOUNT];
	struct libevdev_uinput *uidev[DEVCOUNT];
	char devname[9] = { 'A', 'r', 'd', 'u', 'P', 'a', 'd', '0', '\0' };

	FILE *arduino;
	char *serialport;
	
	//check for file path to open
	if (argc < 2)
	{
		printf("Usage: %s <filename>\n", argv[0]);
		return -1;
	}
	
	serialport = argv[1];
	
	//open file, no point in doing anything else until this is done
	arduino = fopen(serialport, "rw");

	if (arduino == 0)
	{
		printf("Failed to open %s.\n", serialport);
	}
	 
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
			printf("Failed to create device %i: %i.\n", i, err);
		}
	}
	
	do
	{
		
	}
		
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
