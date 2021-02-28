/////////////////////////////////////////////////////////////////////////////////////////////////////////
//
// This project demonstrates how to write a simple vJoy feeder in C#
//
// You can compile it with either #define ROBUST OR #define EFFICIENT
// The fuctionality is similar - 
// The ROBUST section demonstrate the usage of functions that are easy and safe to use but are less efficient
// The EFFICIENT ection demonstrate the usage of functions that are more efficient
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
#define ROBUST
//#define EFFICIENT

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

#if ROBUST
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
					// Set position of 4 axes
					//res = joystick.SetAxis(X, id, HID_USAGES.HID_USAGE_X);
					//res = joystick.SetAxis(Y, id, HID_USAGES.HID_USAGE_Y);
					//res = joystick.SetAxis(Z, id, HID_USAGES.HID_USAGE_Z);
					//res = joystick.SetAxis(XR, id, HID_USAGES.HID_USAGE_RX);
					//res = joystick.SetAxis(ZR, id, HID_USAGES.HID_USAGE_RZ);

					// Press/Release Buttons
					//res = joystick.SetBtn(true, id, count / 50);
					//res = joystick.SetBtn(false, id, 1 + count / 50);

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

					//if (inputs[1] != 0x55)
					//{
					//	Console.WriteLine("Tx error");
					//	Console.WriteLine(inputs[0].ToString("X2") + inputs[1].ToString("X2") + inputs[2].ToString("X2") + inputs[3].ToString("X2") + inputs[4].ToString("X2") + inputs[5].ToString("X2") + inputs[6].ToString("X2") + inputs[7].ToString("X2") + inputs[8].ToString("X2") + inputs[9].ToString("X2") + inputs[10].ToString("X2") + inputs[11].ToString("X2") + inputs[12].ToString("X2") + inputs[13].ToString("X2") + inputs[14].ToString("X2") + inputs[15].ToString("X2"));
					//}

					//System.Threading.Thread.Sleep(20);

					//for (int i = 0; i < 8; i++)
					//{
					//	Console.Write("controller ");
					//	Console.Write(i);
					//	Console.Write(": ");
					//	Console.Write((inputs[(i << 1)] & 0x01) != 0 ? ">" : "-");
					//	Console.Write((inputs[(i << 1)] & 0x02) != 0 ? "<" : "-");
					//	Console.Write((inputs[(i << 1)] & 0x04) != 0 ? "v" : "-");
					//	Console.Write((inputs[(i << 1)] & 0x08) != 0 ? "^" : "-");
					//	Console.Write((inputs[(i << 1)] & 0x10) != 0 ? "T" : "-");
					//	Console.Write((inputs[(i << 1)] & 0x20) != 0 ? "S" : "-");
					//	Console.Write((inputs[(i << 1)] & 0x40) != 0 ? "Y" : "-");
					//	Console.Write((inputs[(i << 1)] & 0x80) != 0 ? "B" : "-");
					//	Console.Write((inputs[(i << 1) + 1] & 0x01) != 0 ? "4" : "-");
					//	Console.Write((inputs[(i << 1) + 1] & 0x02) != 0 ? "3" : "-");
					//	Console.Write((inputs[(i << 1) + 1] & 0x04) != 0 ? "2" : "-");
					//	Console.Write((inputs[(i << 1) + 1] & 0x08) != 0 ? "1" : "-");
					//	Console.Write((inputs[(i << 1) + 1] & 0x10) != 0 ? "R" : "-");
					//	Console.Write((inputs[(i << 1) + 1] & 0x20) != 0 ? "L" : "-");
					//	Console.Write((inputs[(i << 1) + 1] & 0x40) != 0 ? "X" : "-");
					//	Console.Write((inputs[(i << 1) + 1] & 0x80) != 0 ? "A" : "-");
					//	Console.Write("\r\n");
					//}

					// If Continuous POV hat switches installed - make them go round
					// For high values - put the switches in neutral state
					//if (ContPovNumber>0)
					//{
					//    if ((count * 70) < 30000)
					//    {
					//        res = joystick.SetContPov(((int)count * 70), id, 1);
					//        res = joystick.SetContPov(((int)count * 70) + 2000, id, 2);
					//        res = joystick.SetContPov(((int)count * 70) + 4000, id, 3);
					//        res = joystick.SetContPov(((int)count * 70) + 6000, id, 4);
					//    }
					//    else
					//    {
					//        res = joystick.SetContPov(-1, id, 1);
					//        res = joystick.SetContPov(-1, id, 2);
					//        res = joystick.SetContPov(-1, id, 3);
					//        res = joystick.SetContPov(-1, id, 4);
					//    };
					//};

					// If Discrete POV hat switches installed - make them go round
					// From time to time - put the switches in neutral state
					//if (DiscPovNumber>0)
					//{
					//    if (count < 550)
					//    {
					//        joystick.SetDiscPov((((int)count / 20) + 0) % 4, id, 1);
					//        joystick.SetDiscPov((((int)count / 20) + 1) % 4, id, 2);
					//        joystick.SetDiscPov((((int)count / 20) + 2) % 4, id, 3);
					//        joystick.SetDiscPov((((int)count / 20) + 3) % 4, id, 4);
					//    }
					//    else
					//    {
					//        joystick.SetDiscPov(-1, id, 1);
					//        joystick.SetDiscPov(-1, id, 2);
					//        joystick.SetDiscPov(-1, id, 3);
					//        joystick.SetDiscPov(-1, id, 4);
					//    };
					//};

					/*
					for (int i = 0; i < 1; i++)
					{
						joystick.SetBtn((inputs[(i << 1)] & 0x80) != 0, id, (uint)((i * 16) + 1)); //B
						joystick.SetBtn((inputs[(i << 1)] & 0x40) != 0, id, (uint)((i * 16) + 2)); //Y
						joystick.SetBtn((inputs[(i << 1)] & 0x20) != 0, id, (uint)((i * 16) + 3)); //start
						joystick.SetBtn((inputs[(i << 1)] & 0x10) != 0, id, (uint)((i * 16) + 4)); //select
						joystick.SetBtn((inputs[(i << 1) + 1] & 0x80) != 0, id, (uint)((i * 16) + 5 + 8)); //A
						joystick.SetBtn((inputs[(i << 1) + 1] & 0x40) != 0, id, (uint)((i * 16) + 6 + 8)); //X
						joystick.SetBtn((inputs[(i << 1) + 1] & 0x20) != 0, id, (uint)((i * 16) + 7 + 8)); //L
						joystick.SetBtn((inputs[(i << 1) + 1] & 0x10) != 0, id, (uint)((i * 16) + 8 + 8)); //R

						if (ContPovNumber > i)
						{
							int povval;

							bool u, d, l, r;
							u = (inputs[(i << 1)] & 0x08) != 0;
							d = (inputs[(i << 1)] & 0x04) != 0;
							l = (inputs[(i << 1)] & 0x02) != 0;
							r = (inputs[(i << 1)] & 0x01) != 0;

							if (u && r)
							{
								povval = 4500;
							}
							else if (u && l)
							{
								povval = 31500;
							}
							else if (d && r)
							{
								povval = 13500;
							}
							else if (d && l)
							{
								povval = 22500;
							}
							else if (u)
							{
								povval = 0;
							}
							else if (d)
							{
								povval = 18000;
							}
							else if (l)
							{
								povval = 27000;
							}
							else if (r)
							{
								povval = 9000;
							}
							else
							{
								povval = -1;
							}

							joystick.SetContPov(povval, id, 1);
						}
					}
					*/

					//System.Threading.Thread.Sleep(5);
					//X += 150; if (X > maxval) X = 0;
					//Y += 250; if (Y > maxval) Y = 0;
					//Z += 350; if (Z > maxval) Z = 0;
					//XR += 220; if (XR > maxval) XR = 0;  
					//ZR += 200; if (ZR > maxval) ZR = 0;  
					//count++;

					//if (count > 640)
					//    count = 0;

				} // While (Robust)
			}

#endif // ROBUST
#if EFFICIENT

            byte[] pov = new byte[4];

      while (true)
            {
            iReport.bDevice = (byte)id;
            iReport.AxisX = X;
            iReport.AxisY = Y;
            iReport.AxisZ = Z;
            iReport.AxisZRot = ZR;
            iReport.AxisXRot = XR;

            // Set buttons one by one
            iReport.Buttons = (uint)(0x1 <<  (int)(count / 20));

		if (ContPovNumber>0)
		{
			// Make Continuous POV Hat spin
			iReport.bHats		= (count*70);
			iReport.bHatsEx1	= (count*70)+3000;
			iReport.bHatsEx2	= (count*70)+5000;
			iReport.bHatsEx3	= 15000 - (count*70);
			if ((count*70) > 36000)
			{
				iReport.bHats =    0xFFFFFFFF; // Neutral state
                iReport.bHatsEx1 = 0xFFFFFFFF; // Neutral state
                iReport.bHatsEx2 = 0xFFFFFFFF; // Neutral state
                iReport.bHatsEx3 = 0xFFFFFFFF; // Neutral state
			};
		}
		else
		{
			// Make 5-position POV Hat spin
			
			pov[0] = (byte)(((count / 20) + 0)%4);
            pov[1] = (byte)(((count / 20) + 1) % 4);
            pov[2] = (byte)(((count / 20) + 2) % 4);
            pov[3] = (byte)(((count / 20) + 3) % 4);

			iReport.bHats		= (uint)(pov[3]<<12) | (uint)(pov[2]<<8) | (uint)(pov[1]<<4) | (uint)pov[0];
			if ((count) > 550)
				iReport.bHats = 0xFFFFFFFF; // Neutral state
		};

        /*** Feed the driver with the position packet - is fails then wait for input then try to re-acquire device ***/
        if (!joystick.UpdateVJD(id, ref iReport))
        {
            Console.WriteLine("Feeding vJoy device number {0} failed - try to enable device then press enter\n", id);
            Console.ReadKey(true);
            joystick.AcquireVJD(id);
        }

        System.Threading.Thread.Sleep(20);
        count++;
        if (count > 640) count = 0;

        X += 150; if (X > maxval) X = 0;
        Y += 250; if (Y > maxval) Y = 0;
        Z += 350; if (Z > maxval) Z = 0;
        XR += 220; if (XR > maxval) XR = 0;
        ZR += 200; if (ZR > maxval) ZR = 0;  
         
      }; // While

#endif // EFFICIENT

		} // Main
	} // class Program
} // namespace FeederDemoCS
