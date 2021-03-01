/////////////////////////////////////////////////////////////////////////////////////////////////////////
//
// This project was made by modifying the vJoy example feeder.
//
// Functionality:
//	The program starts with creating one joystick object. 
//	Then it petches the device id from the command-line and makes sure that it is within range
//	After testing that the driver is enabled it gets information about the driver
//	Gets information about the specified virtual device
//	This feeder uses only a few axes. It checks their existence and 
//	checks the number of buttons and POV Hat switches.
//	Then the feeder acquires the virtual device
//	Here starts and endless loop that feedes data into the virtual device
//
/////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

// Don't forget to add this
using vJoyInterfaceWrap;

namespace FeederDemoCS
{
	class Program
	{
		// Declaring one joystick (Device id 1) and a position structure. 
		static public vJoy joystick;
		static public vJoy.JoystickState iReport;
		static public uint id = 1;
		static public string portname = "COM1";


		static void Main(string[] args)
		{
			// Create one joystick object and a position structure.
			joystick = new vJoy();
			iReport = new vJoy.JoystickState();


			// Device ID can only be in the range 1-16
			//if (args.Length>0 && !String.IsNullOrEmpty(args[0]))
			//    id = Convert.ToUInt32(args[0]);

			for (int i = 0; i < args.Length; i++)
			{
				if (args[i] != null && args[i] != "")
				{
					switch (args[i][0])
					{
						case 'i':
							if (!UInt32.TryParse(args[i].Substring(1, args[i].Length - 1), out id))
							{
								id = 1;
							}
							break;

						case 's':
							try
							{
								portname = args[i].Substring(1, args[i].Length - 1);
							}
							catch (Exception ex)
							{
								portname = "COM1";
							}
							break;

						default:
							break;
					}
				}
			}

			Console.WriteLine("Command line arguments parsed.");
			//Console.ReadLine();

			if (id <= 0 || id > 16)
			{
				Console.WriteLine("Illegal device ID {0}\nExit!", id);
				Console.ReadLine();
				return;
			}

			string[] ports = System.IO.Ports.SerialPort.GetPortNames();
			bool portfound = false;
			for (int i = 0; i < ports.Length; i++)
			{
				Console.WriteLine(ports[i]);
				if (portname == ports[i])
				{
					portfound = true;
				}
			}
			if (!portfound)
			{
				Console.WriteLine("Port {0} not found\nExit!", id);
				Console.ReadLine();
			}

			Console.WriteLine("Device ID and port name checked.");
			//Console.ReadLine();

			// Get the driver attributes (Vendor ID, Product ID, Version Number)
			if (!joystick.vJoyEnabled())
			{
				Console.WriteLine("vJoy driver not enabled: Failed Getting vJoy attributes.\n");
				Console.ReadLine();
				return;
			}
			else
				Console.WriteLine("Vendor: {0}\nProduct :{1}\nVersion Number:{2}\n", joystick.GetvJoyManufacturerString(), joystick.GetvJoyProductString(), joystick.GetvJoySerialNumberString());

			// Get the state of the requested device
			VjdStat status = joystick.GetVJDStatus(id);
			switch (status)
			{
				case VjdStat.VJD_STAT_OWN:
					Console.WriteLine("vJoy Device {0} is already owned by this feeder\n", id);
					break;
				case VjdStat.VJD_STAT_FREE:
					Console.WriteLine("vJoy Device {0} is free\n", id);
					break;
				case VjdStat.VJD_STAT_BUSY:
					Console.WriteLine("vJoy Device {0} is already owned by another feeder\nCannot continue\n", id);
					Console.ReadLine();
					return;
				case VjdStat.VJD_STAT_MISS:
					Console.WriteLine("vJoy Device {0} is not installed or disabled\nCannot continue\n", id);
					Console.ReadLine();
					return;
				default:
					Console.WriteLine("vJoy Device {0} general error\nCannot continue\n", id);
					Console.ReadLine();
					return;
			};

			// Check which axes are supported
			bool AxisX = joystick.GetVJDAxisExist(id, HID_USAGES.HID_USAGE_X);
			bool AxisY = joystick.GetVJDAxisExist(id, HID_USAGES.HID_USAGE_Y);
			bool AxisZ = joystick.GetVJDAxisExist(id, HID_USAGES.HID_USAGE_Z);
			bool AxisRX = joystick.GetVJDAxisExist(id, HID_USAGES.HID_USAGE_RX);
			bool AxisRZ = joystick.GetVJDAxisExist(id, HID_USAGES.HID_USAGE_RZ);
			// Get the number of buttons and POV Hat switchessupported by this vJoy device
			int nButtons = joystick.GetVJDButtonNumber(id);
			int ContPovNumber = joystick.GetVJDContPovNumber(id);
			int DiscPovNumber = joystick.GetVJDDiscPovNumber(id);

			// Print results
			Console.WriteLine("\nvJoy Device {0} capabilities:\n", id);
			Console.WriteLine("Numner of buttons\t\t{0}\n", nButtons);
			Console.WriteLine("Numner of Continuous POVs\t{0}\n", ContPovNumber);
			Console.WriteLine("Numner of Descrete POVs\t\t{0}\n", DiscPovNumber);
			Console.WriteLine("Axis X\t\t{0}\n", AxisX ? "Yes" : "No");
			Console.WriteLine("Axis Y\t\t{0}\n", AxisX ? "Yes" : "No");
			Console.WriteLine("Axis Z\t\t{0}\n", AxisX ? "Yes" : "No");
			Console.WriteLine("Axis Rx\t\t{0}\n", AxisRX ? "Yes" : "No");
			Console.WriteLine("Axis Rz\t\t{0}\n", AxisRZ ? "Yes" : "No");

			// Test if DLL matches the driver
			UInt32 DllVer = 0, DrvVer = 0;
			bool match = joystick.DriverMatch(ref DllVer, ref DrvVer);
			if (match)
				Console.WriteLine("Version of Driver Matches DLL Version ({0:X})\n", DllVer);
			else
				Console.WriteLine("Version of Driver ({0:X}) does NOT match DLL Version ({1:X})\n", DrvVer, DllVer);


			// Acquire the target
			if ((status == VjdStat.VJD_STAT_OWN) || ((status == VjdStat.VJD_STAT_FREE) && (!joystick.AcquireVJD(id))))
			{
				Console.WriteLine("Failed to acquire vJoy device number {0}.\n", id);
				Console.ReadLine();
				return;
			}
			else
				Console.WriteLine("Acquired: vJoy device number {0}.\n", id);

			//Console.WriteLine("\npress enter to stat feeding");
			//Console.ReadKey(true);

			Console.WriteLine("Feeding controller inputs...");

			//int X, Y, Z, ZR, XR;
			uint count = 0;
			long maxval = 0;

			//X = 20;
			//Y = 30;
			//Z = 40;
			//XR = 60;
			//ZR = 80;

			joystick.GetVJDAxisMax(id, HID_USAGES.HID_USAGE_X, ref maxval);
			bool res;
			// Reset this device to default values
			joystick.ResetVJD(id);

			using (Process p = Process.GetCurrentProcess())
			{
				try
				{
					p.PriorityClass = ProcessPriorityClass.RealTime;
				}
				catch (Exception ex1)
				{
					Console.WriteLine("Failed to set realtime priority.");

					try
					{
						p.PriorityClass = ProcessPriorityClass.High;
					}
					catch (Exception ex2)
					{
						Console.WriteLine("Failed to set high priority. Running at normal priority.");
					}
				}

				p.ProcessorAffinity = (IntPtr)(1 << (Environment.ProcessorCount - 1));
			}

			byte[] oldinputs = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
			byte[] cosinputs = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

			using (System.IO.Ports.SerialPort port = new System.IO.Ports.SerialPort(portname, 115200, System.IO.Ports.Parity.None, 8, System.IO.Ports.StopBits.One))
			{

				port.WriteTimeout = 1000;
				port.ReadTimeout = 1000;
				port.Open();

				// Feed the device in endless loop
				while (true)
				{

					byte[] inputs = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

					try
					{
						byte[] test = new byte[1];

						do
						{
							port.Read(test, 0, 1);
						} while (test[0] != 0xFF);

						for (int i = 0; i < 16; i++)
						{
							int inbyte;

							do
							{
								inbyte = port.ReadByte();
							}
							while (inbyte < 0);

							inputs[i] = (byte)inbyte;
						}
					}
					catch (Exception ex)
					{
						Console.WriteLine("Exception reading joystick port: " + ex.Message);
					}

					//16 byte packet - supports 8 SNES-style controllers simultaneously
					//2 byte - player 1
					//2 byte - player 2
					//2 byte - player 3
					//2 byte - player 4
					//2 byte - player 5
					//2 byte - player 6
					//2 byte - player 7
					//2 byte - player 8

					for (int i = 0; i < inputs.Length; i++)
					{
						cosinputs[i] = (byte)(inputs[i] ^ oldinputs[i]);

						for (int j = 0; j < 8; j++)
						{
							//change: try to only send new status on change-of-state to reduce input events
							if ((cosinputs[i] & (1 << j)) != 0)
							{
								res = joystick.SetBtn((inputs[i] & (1 << j)) != 0, id, (uint)(((i * 8) + j) + 1));
							}
						}
					}

					//copy current over old
					oldinputs = inputs;

				} // While (Robust)
			}
		} // Main
	} // class Program
} // namespace FeederDemoCS
