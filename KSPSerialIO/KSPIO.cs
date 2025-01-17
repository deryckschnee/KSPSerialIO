
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Threading;
using Microsoft.Win32;
using System.Runtime.InteropServices;

using System.Text.RegularExpressions;
using OpenNETCF.IO.Ports;
using UnityEngine;
using KSP.IO;
using KSP.UI.Screens;

namespace KSPSerialIO
{
    #region Structs
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct VesselData
    {
        public byte id;              //1
        public float AP;             //2
        public float PE;             //3
        public float SemiMajorAxis;  //4
        public float SemiMinorAxis;  //5
        public float VVI;            //6
        public float e;              //7
        public float inc;            //8
        public float G;              //9
        public int TAp;              //10
        public int TPe;              //11
        public float TrueAnomaly;    //12
        public float Density;        //13
        public int period;           //14
        public float RAlt;           //15
        public float Alt;            //16
        public float Vsurf;          //17
        public float Lat;            //18
        public float Lon;            //19
        public float LiquidFuelTot;  //20
        public float LiquidFuel;     //21
        public float OxidizerTot;    //22
        public float Oxidizer;       //23
        public float EChargeTot;     //24
        public float ECharge;        //25
        public float MonoPropTot;    //26
        public float MonoProp;       //27
        public float IntakeAirTot;   //28
        public float IntakeAir;      //29
        public float SolidFuelTot;   //30
        public float SolidFuel;      //31
        public float XenonGasTot;    //32
        public float XenonGas;       //33
        public float LiquidFuelTotS; //34
        public float LiquidFuelS;    //35
        public float OxidizerTotS;   //36
        public float OxidizerS;      //37
        public uint MissionTime;   //38
        public float deltaTime;      //39
        public float VOrbit;         //40
        public uint MNTime;        //41
        public float MNDeltaV;       //42
        public float Pitch;          //43
        public float Roll;           //44
        public float Heading;        //45
        public ushort ActionGroups;  //46  status bit order:SAS, RCS, Light, Gear, Brakes, Abort, Custom01 - 10 
        public byte SOINumber;       //47  SOI Number (decimal format: sun-planet-moon e.g. 130 = kerbin, 131 = mun)
        public byte MaxOverHeat;     //48  Max part overheat (% percent)
        public float MachNumber;     //49
        public float IAS;            //50  Indicated Air Speed
        public byte CurrentStage;    //51  Current stage number
        public byte TotalStage;      //52  TotalNumber of stages
        public float TargetDist;     //53  Distance to targeted vessel (m)
        public float TargetV;        //54  Target vessel relative velocity (m/s)
        public byte NavballSASMode;  //55  Combined byte for navball target mode and SAS mode
                                     // First four bits indicate AutoPilot mode:
                                     // 0 SAS is off  //1 = Regular Stability Assist //2 = Prograde
                                     // 3 = RetroGrade //4 = Normal //5 = Antinormal //6 = Radial In
                                     // 7 = Radial Out //8 = Target //9 = Anti-Target //10 = Maneuver node
                                     // Last 4 bits set navball mode. (0=ignore,1=ORBIT,2=SURFACE,3=TARGET)
        public short ProgradePitch;  //56 Pitch   Of the Prograde Vector;  int_16 ***Changed: now fix point, actual angle = angle/50*** used to be (-0x8000(-360 degrees) to 0x7FFF(359.99ish degrees)); 
        public short ProgradeHeading;//57 Heading Of the Prograde Vector;  see above for range   (Prograde vector depends on navball mode, eg Surface/Orbit/Target)
        public short ManeuverPitch;  //58 Pitch   Of the Maneuver Vector;  see above for range;  (0 if no Maneuver node)
        public short ManeuverHeading;//59 Heading Of the Maneuver Vector;  see above for range;  (0 if no Maneuver node)
        public short TargetPitch;    //60 Pitch   Of the Target   Vector;  see above for range;  (0 if no Target)
        public short TargetHeading;  //61 Heading Of the Target   Vector;  see above for range;  (0 if no Target)
        public short NormalHeading;  //62 Normal Of the Prograde Vector;  see above for range;  (Pitch of the Heading Vector is always 0)
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct HandShakePacket
    {
        public byte id;
        public byte M1;
        public byte M2;
        public byte M3;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ControlPacket
    {
        public byte id;
        public byte MainControls;                  //SAS RCS Lights Gear Brakes Precision Abort Stage 
        public byte Mode;                          //0 = stage, 1 = docking, 2 = map
        public ushort ControlGroup;                //control groups 1-10 in 2 bytes
        public byte NavballSASMode;                //AutoPilot mode (See above for AutoPilot modes)(Ignored if the equal to zero or out of bounds (>10)) //Navball mode
        public byte AdditionalControlByte1;
        public short Pitch;                        //-1000 -> 1000
        public short Roll;                         //-1000 -> 1000
        public short Yaw;                          //-1000 -> 1000
        public short TX;                           //-1000 -> 1000
        public short TY;                           //-1000 -> 1000
        public short TZ;                           //-1000 -> 1000
        public short WheelSteer;                   //-1000 -> 1000
        public short Throttle;                     // 0 -> 1000
        public short WheelThrottle;                // 0 -> 1000
    };

    public struct VesselControls
    {
        public bool SAS;
        public bool RCS;
        public bool Lights;
        public bool Gear;
        public bool Brakes;
        public bool Precision;
        public bool Abort;
        public bool Stage;
        public int Mode;
        public int SASMode;
        public int SpeedMode;
        public bool[] ControlGroup;
        public float Pitch;
        public float Roll;
        public float Yaw;
        public float TX;
        public float TY;
        public float TZ;
        public float WheelSteer;
        public float Throttle;
        public float WheelThrottle;
    };

    public struct IOResource
    {
        public float Max;
        public float Current;
    }

    public struct NavHeading
    {
        public float Pitch, Heading;
        /*  public NavHeading(float Pitch, float Heading)
          {
              this.Pitch = Pitch;
              this.Heading = Heading;
          }*/
    }

    #endregion

    enum EnumAG : int
    {
        SAS,
        RCS,
        Light,
        Gear,
        Brakes,
        Abort,
        Custom01,
        Custom02,
        Custom03,
        Custom04,
        Custom05,
        Custom06,
        Custom07,
        Custom08,
        Custom09,
        Custom10,
    };

    [KSPAddon(KSPAddon.Startup.MainMenu, false)]
    public class SettingsNStuff : MonoBehaviour
    {

        public static PluginConfiguration cfg = PluginConfiguration.CreateForType<SettingsNStuff>();
        public static string DefaultPort;
        public static double refreshrate;
        public static int HandshakeDelay;
        public static int HandshakeDisable;
        public static int BaudRate;
        // Throttle and axis controls have the following settings:
        // 0: The internal value (supplied by KSP) is always used.
        // 1: The external value (read from serial packet) is always used.
        // 2: If the internal value is not zero use it, otherwise use the external value.
        // 3: If the external value is not zero use it, otherwise use the internal value.        
        public static int PitchEnable;
        public static int RollEnable;
        public static int YawEnable;
        public static int TXEnable;
        public static int TYEnable;
        public static int TZEnable;
        public static int WheelSteerEnable;
        public static int ThrottleEnable;
        public static int WheelThrottleEnable;
        public static double SASTol;

        void Awake()
        {
            //cfg["refresh"] = 0.08;
            //cfg["DefaultPort"] = "COM1";
            //cfg["HandshakeDelay"] = 2500;
            print("KSPSerialIO: Loading settings...");

            cfg.load();
            DefaultPort = cfg.GetValue<string>("DefaultPort");
            print("KSPSerialIO: Default Port = " + DefaultPort);

            refreshrate = cfg.GetValue<double>("refresh");
            print("KSPSerialIO: Refreshrate = " + refreshrate.ToString());

            BaudRate = cfg.GetValue<int>("BaudRate");
            print("KSPSerialIO: BaudRate = " + BaudRate.ToString());

            HandshakeDelay = cfg.GetValue<int>("HandshakeDelay");
            print("KSPSerialIO: Handshake Delay = " + HandshakeDelay.ToString());

            HandshakeDisable = cfg.GetValue<int>("HandshakeDisable");
            print("KSPSerialIO: Handshake Disable = " + HandshakeDisable.ToString());

            PitchEnable = cfg.GetValue<int>("PitchEnable");
            print("KSPSerialIO: Pitch Enable = " + PitchEnable.ToString());

            RollEnable = cfg.GetValue<int>("RollEnable");
            print("KSPSerialIO: Roll Enable = " + RollEnable.ToString());

            YawEnable = cfg.GetValue<int>("YawEnable");
            print("KSPSerialIO: Yaw Enable = " + YawEnable.ToString());

            TXEnable = cfg.GetValue<int>("TXEnable");
            print("KSPSerialIO: Translate X Enable = " + TXEnable.ToString());

            TYEnable = cfg.GetValue<int>("TYEnable");
            print("KSPSerialIO: Translate Y Enable = " + TYEnable.ToString());

            TZEnable = cfg.GetValue<int>("TZEnable");
            print("KSPSerialIO: Translate Z Enable = " + TZEnable.ToString());

            WheelSteerEnable = cfg.GetValue<int>("WheelSteerEnable");
            print("KSPSerialIO: Wheel Steering Enable = " + WheelSteerEnable.ToString());

            ThrottleEnable = cfg.GetValue<int>("ThrottleEnable");
            print("KSPSerialIO: Throttle Enable = " + ThrottleEnable.ToString());

            WheelThrottleEnable = cfg.GetValue<int>("WheelThrottleEnable");
            print("KSPSerialIO: Wheel Throttle Enable = " + WheelThrottleEnable.ToString());

            SASTol = cfg.GetValue<double>("SASTol");
            print("KSPSerialIO: SAS Tol = " + SASTol.ToString());
        }
    }

    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class KSPSerialPort : MonoBehaviour
    {
        public static SerialPort Port;
        public static string PortNumber;
        public static string Win32PortName;
        public static bool DisplayFound = false;
        public static bool ControlReceived = false;

        public static VesselData VData;
        public static ControlPacket CPacket;
        private static HandShakePacket HPacket;

        public static VesselControls VControls = new VesselControls();
        public static VesselControls VControlsOld = new VesselControls();

        private static byte[] buffer = new byte[255];
        private static byte rx_len;
        private static byte rx_array_inx;
        private static int structSize;
        private static byte id = 255;

        private const byte HSPid = 0, VDid = 1, Cid = 101; //hard coded values for packet IDS


        public static void sendPacket(object anything)
        {
            byte[] Payload = StructureToByteArray(anything);
            byte header1 = 0xBE;
            byte header2 = 0xEF;
            byte size = (byte)Payload.Length;
            byte checksum = size;

            byte[] Packet = new byte[size + 4];

            //Packet = [header][size][payload][checksum];
            //Header = [Header1=0xBE][Header2=0xEF]
            //size = [payload.length (0-255)]

            for (int i = 0; i < size; i++)
            {
                checksum ^= Payload[i];
            }

            Payload.CopyTo(Packet, 3);
            Packet[0] = header1;
            Packet[1] = header2;
            Packet[2] = size;
            Packet[Packet.Length - 1] = checksum;

            Port.Write(Packet, 0, Packet.Length);
        }

        private void Begin()
        {
            Port = new SerialPort(Win32PortName, SettingsNStuff.BaudRate, Parity.None, 8, StopBits.One);
            //Port = new SerialPort();
            Port.ReceivedBytesThreshold = 3;
            Port.ReceivedEvent += Port_ReceivedEvent;
        }

        //these are copied from the intarwebs, converts struct to byte array
        private static byte[] StructureToByteArray(object obj)
        {
            int len = Marshal.SizeOf(obj);
            byte[] arr = new byte[len];
            IntPtr ptr = Marshal.AllocHGlobal(len);
            Marshal.StructureToPtr(obj, ptr, true);
            Marshal.Copy(ptr, arr, 0, len);
            Marshal.FreeHGlobal(ptr);
            return arr;
        }

        private static object ByteArrayToStructure(byte[] bytearray, object obj)
        {
            int len = Marshal.SizeOf(obj);

            IntPtr i = Marshal.AllocHGlobal(len);

            Marshal.Copy(bytearray, 0, i, len);

            obj = Marshal.PtrToStructure(i, obj.GetType());

            Marshal.FreeHGlobal(i);

            return obj;
        }
        /*
          private static T ReadUsingMarshalUnsafe<T>(byte[] data) where T : struct
          {
          unsafe
          {
          fixed (byte* p = &data[0])
          {
          return (T)Marshal.PtrToStructure(new IntPtr(p), typeof(T));
          }
          }
          }
        */
        void initializeDataPackets()
        {
            VData = new VesselData();
            VData.id = VDid;

            HPacket = new HandShakePacket();
            HPacket.id = HSPid;
            HPacket.M1 = 1;
            HPacket.M2 = 2;
            HPacket.M3 = 3;

            CPacket = new ControlPacket();

            VControls.ControlGroup = new Boolean[11];
            VControlsOld.ControlGroup = new Boolean[11];
        }

        void Awake()
        {
            if (DisplayFound)
            {
                Debug.Log("KSPSerialIO: running...");
                Begin();
            }
            else
            {
                Debug.Log("KSPSerialIO: Version 0.19.3b");
                Debug.Log("KSPSerialIO: Getting serial ports...");
                Debug.Log("KSPSerialIO: Output packet size: " + Marshal.SizeOf(VData).ToString() + "/255");
                initializeDataPackets();

                try
                {
                    //Use registry hack to get a list of serial ports until we get system.io.ports
                    RegistryKey SerialCOMSKey = Registry.LocalMachine.OpenSubKey(@"HARDWARE\\DEVICEMAP\\SERIALCOMM\\");

                    Begin();

                    //print("KSPSerialIO: receive threshold " + Port.ReceivedBytesThreshold.ToString());

                    if (SerialCOMSKey == null)
                    {
                        Debug.Log("KSPSerialIO: Dude do you even win32 serial port??");
                    }
                    else
                    {
                        String[] realports = SerialCOMSKey.GetValueNames();  // get list of all serial devices
                        String[] names = new string[realports.Length + 1];   // make a new list with 1 extra, we put the default port first
                        realports.CopyTo(names, 1);

                        Debug.Log("KSPSerialIO: Found " + names.Length.ToString() + " serial ports");

                        //look through all found ports for our display
                        int j = 0;

                        foreach (string PortName in names)
                        {
                            if (j == 0)  // try default port first
                            {
                                PortNumber = SettingsNStuff.DefaultPort;
                                Debug.Log("KSPSerialIO: trying default port " + PortNumber);
                            }
                            else
                            {
                                PortNumber = (string)SerialCOMSKey.GetValue(PortName);
                                Debug.Log("KSPSerialIO: trying port " + PortName + " - " + PortNumber);
                            }

                            Win32PortName = @"\\.\" + PortNumber;   // add @"\\.\" to PortNumber to have win10 compatibility for com ports > 9 --- Jimbofarrar

                            Port.PortName = Win32PortName;

                            //Port.PortName = PortNumber; // Original ---Zitron

                            j++;

                            if (!Port.IsOpen)
                            {
                                try
                                {
                                    Port.Open();
                                }
                                catch (Exception e)
                                {
                                    Debug.Log("Error opening serial port " + Win32PortName + ": " + e.Message);
                                }

                                //secret handshake
                                if (Port.IsOpen && (SettingsNStuff.HandshakeDisable == 0))
                                {
                                    Thread.Sleep(SettingsNStuff.HandshakeDelay);
                                    //Port.DiscardOutBuffer();
                                    //Port.DiscardInBuffer();

                                    sendPacket(HPacket);

                                    //wait for reply
                                    int k = 0;

                                    while (Port.BytesToRead == 0 && k < 15 && !DisplayFound)
                                    {
                                        Thread.Sleep(100);
                                        k++;
                                    }

                                    Port.Close();
                                    if (DisplayFound)
                                    {
                                        Debug.Log("KSPSerialIO: found KSP Display at " + Win32PortName);
                                        break;
                                    }
                                    else
                                    {
                                        Debug.Log("KSPSerialIO: KSP Display not found");
                                    }
                                }
                                else if (Port.IsOpen && (SettingsNStuff.HandshakeDisable == 1))
                                {
                                    DisplayFound = true;
                                    Debug.Log("KSPSerialIO: Handshake disabled, using " + Win32PortName);
                                    break;
                                }
                            }
                            else
                            {
                                Debug.Log("KSPSerialIO: " + Win32PortName + "is already being used.");
                            }
                        }
                    }

                }
                catch (Exception e)
                {
                    print(e.Message);
                }
            }
        }

        private string readline()
        {
            string result = null;
            char c;
            int j = 0;

            c = (char)Port.ReadByte();
            while (c != '\n' && j < 255)
            {
                result += c;
                c = (char)Port.ReadByte();
                j++;
            }
            return result;
        }

        private void Port_ReceivedEvent(object sender, SerialReceivedEventArgs e)
        {
            while (Port.BytesToRead > 0)
            {
                if (processCOM())
                {
                    switch (id)
                    {
                        case HSPid:
                            HPacket = (HandShakePacket)ByteArrayToStructure(buffer, HPacket);
                            Invoke("HandShake", 0);

                            if ((HPacket.M1 == 3) && (HPacket.M2 == 1) && (HPacket.M3 == 4))
                            {
                                DisplayFound = true;
                            }

                            else
                            {
                                DisplayFound = false;
                            }
                            break;
                        case Cid:
                            VesselControls();
                            //Invoke("VesselControls", 0);
                            break;
                        default:
                            Invoke("Unimplemented", 0);
                            break;
                    }
                }
            }
        }

        private static bool processCOM()
        {
            byte calc_CS;

            if (rx_len == 0)
            {
                while (Port.ReadByte() != 0xBE)
                {
                    if (Port.BytesToRead == 0)
                        return false;
                }

                if (Port.ReadByte() == 0xEF)
                {
                    rx_len = (byte)Port.ReadByte();
                    id = (byte)Port.ReadByte();
                    rx_array_inx = 1;

                    switch (id)
                    {
                        case HSPid:
                            structSize = Marshal.SizeOf(HPacket);
                            break;
                        case Cid:
                            structSize = Marshal.SizeOf(CPacket);
                            break;
                    }

                    //make sure the binary structs on both Arduino and plugin are the same size.
                    if (rx_len != structSize || rx_len == 0)
                    {
                        SizeWrong(rx_len, structSize);                         //Debug option ==================
                        rx_len = 0;
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            else
            {
                while (Port.BytesToRead > 0 && rx_array_inx <= rx_len)
                {
                    buffer[rx_array_inx++] = (byte)Port.ReadByte();
                }
                buffer[0] = id;

                if (rx_len == (rx_array_inx - 1))
                {
                    //seem to have got whole message
                    //last uint8_t is CS
                    calc_CS = rx_len;
                    for (int i = 0; i < rx_len; i++)
                    {
                        calc_CS ^= buffer[i];
                    }

                    if (calc_CS == buffer[rx_array_inx - 1])
                    {//CS good
                        rx_len = 0;
                        rx_array_inx = 1;

                        CheckSumPass();                         //Debug option ==================
                        return true;
                    }
                    else
                    {
                        //failed checksum, need to clear this out anyway
                        rx_len = 0;
                        rx_array_inx = 1;

                        CheckSumFail();                         //Debug option ==================
                        return false;
                    }
                }
            }

            return false;
        }

        private void HandShake()
        {
            Debug.Log("KSPSerialIO: Handshake received - " + HPacket.M1.ToString() + HPacket.M2.ToString() + HPacket.M3.ToString());
        }

        private static void SizeWrong(int R, int P)
        {
            Debug.Log("KSPSerialIO: Packet Size ERROR - " + R.ToString() + "/" + P.ToString());
        }

        private static void CheckSumFail()
        {
            Debug.Log("KSPSerialIO: CS FAIL - " + BitConverter.ToString(buffer));
        }

        private static void CheckSumPass()
        {
            Debug.Log("KSPSerialIO: CS PASS - " + BitConverter.ToString(buffer));
        }

        private void VesselControls()
        {
            CPacket = (ControlPacket)ByteArrayToStructure(buffer, CPacket);

            VControls.SAS = BitMathByte(CPacket.MainControls, 7);
            VControls.RCS = BitMathByte(CPacket.MainControls, 6);
            VControls.Lights = BitMathByte(CPacket.MainControls, 5);
            VControls.Gear = BitMathByte(CPacket.MainControls, 4);
            VControls.Brakes = BitMathByte(CPacket.MainControls, 3);
            VControls.Precision = BitMathByte(CPacket.MainControls, 2);
            VControls.Abort = BitMathByte(CPacket.MainControls, 1);
            VControls.Stage = BitMathByte(CPacket.MainControls, 0);
            VControls.Pitch = (float)CPacket.Pitch / 1000.0F;
            VControls.Roll = (float)CPacket.Roll / 1000.0F;
            VControls.Yaw = (float)CPacket.Yaw / 1000.0F;
            VControls.TX = (float)CPacket.TX / 1000.0F;
            VControls.TY = (float)CPacket.TY / 1000.0F;
            VControls.TZ = (float)CPacket.TZ / 1000.0F;
            VControls.WheelSteer = (float)CPacket.WheelSteer / 1000.0F;
            VControls.Throttle = (float)CPacket.Throttle / 1000.0F;
            VControls.WheelThrottle = (float)CPacket.WheelThrottle / 1000.0F;
            VControls.SASMode = (int)CPacket.NavballSASMode & 0x0F;
            VControls.SpeedMode = (int)(CPacket.NavballSASMode >> 4);

            for (int j = 1; j <= 10; j++)
            {
                VControls.ControlGroup[j] = BitMathUshort(CPacket.ControlGroup, j);
            }

            ControlReceived = true;
            //Debug.Log("KSPSerialIO: ControlPacket received");
        }

        private Boolean BitMathByte(byte x, int n)
        {
            return ((x >> n) & 1) == 1;
        }

        private Boolean BitMathUshort(ushort x, int n)
        {
            return ((x >> n) & 1) == 1;
        }

        private void Unimplemented()
        {
            Debug.Log("KSPSerialIO: Packet id unimplemented");
        }

        private static void debug()
        {
            Debug.Log(Port.BytesToRead.ToString() + "BTR");
        }


        public static void ControlStatus(int n, bool s)
        {
            if (s)
                VData.ActionGroups |= (UInt16)(1 << n);       // forces nth bit of x to be 1.  all other bits left alone.
            else
                VData.ActionGroups &= (UInt16)~(1 << n);      // forces nth bit of x to be 0.  all other bits left alone.
        }

        void OnDestroy()
        {
            if (KSPSerialPort.Port.IsOpen)
            {
                KSPSerialPort.Port.Close();
                Port.ReceivedEvent -= Port_ReceivedEvent;
                Debug.Log("KSPSerialIO: Port closed");
            }
        }
    }

    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class KSPSerialIO : MonoBehaviour
    {
        private double lastUpdate = 0.0f;
        private double deltaT = 1.0f;
        private double missionTime = 0;
        private double missionTimeOld = 0;
        private double theTime = 0;

        public double refreshrate = 1.0f;
        public static Vessel ActiveVessel = new Vessel();

        public Guid VesselIDOld;

        IOResource TempR = new IOResource();

        // private static Boolean wasSASOn = false;

        private ScreenMessageStyle KSPIOScreenStyle = ScreenMessageStyle.UPPER_RIGHT;

        void Awake()
        {
            ScreenMessages.PostScreenMessage("IO awake", 10f, KSPIOScreenStyle);
            refreshrate = SettingsNStuff.refreshrate;
        }

        void Start()
        {
            if (KSPSerialPort.DisplayFound)
            {
                if (!KSPSerialPort.Port.IsOpen)
                {
                    ScreenMessages.PostScreenMessage("Starting serial port " + KSPSerialPort.Win32PortName, 10f, KSPIOScreenStyle);

                    try
                    {
                        KSPSerialPort.Port.PortName = KSPSerialPort.Win32PortName;
                        KSPSerialPort.Port.Open();
                        Thread.Sleep(SettingsNStuff.HandshakeDelay);
                    }
                    catch (Exception e)
                    {
                        ScreenMessages.PostScreenMessage("Error opening serial port " + KSPSerialPort.Win32PortName, 10f, KSPIOScreenStyle);
                        ScreenMessages.PostScreenMessage(e.Message, 10f, KSPIOScreenStyle);
                    }
                }
                else
                {
                    ScreenMessages.PostScreenMessage("Using serial port " + KSPSerialPort.Win32PortName, 10f, KSPIOScreenStyle);

                    if (SettingsNStuff.HandshakeDisable == 1)
                        ScreenMessages.PostScreenMessage("Handshake disabled");
                }

                Thread.Sleep(200);

                ActiveVessel.OnPostAutopilotUpdate -= AxisInput;
                ActiveVessel = FlightGlobals.ActiveVessel;
                ActiveVessel.OnPostAutopilotUpdate += AxisInput;

                //sync inputs at start
                ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.RCS, KSPSerialPort.VControls.RCS);
                ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.SAS, KSPSerialPort.VControls.SAS);
                ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.Light, KSPSerialPort.VControls.Lights);
                ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.Gear, KSPSerialPort.VControls.Gear);
                ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, KSPSerialPort.VControls.Brakes);
                ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.Abort, KSPSerialPort.VControls.Abort);
                ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.Stage, KSPSerialPort.VControls.Stage);

                ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.Custom01, KSPSerialPort.VControls.ControlGroup[1]);
                ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.Custom02, KSPSerialPort.VControls.ControlGroup[2]);
                ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.Custom03, KSPSerialPort.VControls.ControlGroup[3]);
                ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.Custom04, KSPSerialPort.VControls.ControlGroup[4]);
                ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.Custom05, KSPSerialPort.VControls.ControlGroup[5]);
                ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.Custom06, KSPSerialPort.VControls.ControlGroup[6]);
                ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.Custom07, KSPSerialPort.VControls.ControlGroup[7]);
                ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.Custom08, KSPSerialPort.VControls.ControlGroup[8]);
                ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.Custom09, KSPSerialPort.VControls.ControlGroup[9]);
                ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.Custom10, KSPSerialPort.VControls.ControlGroup[10]);

                /*
                ActiveVessel.OnFlyByWire -= new FlightInputCallback(AxisInput);
                ActiveVessel.OnFlyByWire += new FlightInputCallback(AxisInput);
                
                ActiveVessel.OnPostAutopilotUpdate -= AxisInput;
                ActiveVessel.OnPostAutopilotUpdate += AxisInput;
                */
            }
            else
            {
                ScreenMessages.PostScreenMessage("No display found", 10f, KSPIOScreenStyle);
            }
        }

        void Update()
        {
            if (FlightGlobals.ActiveVessel != null && KSPSerialPort.Port.IsOpen)
            {
                //Debug.Log("KSPSerialIO: 1");
                //If the current active vessel is not what we were using, we need to remove controls from the old 
                //vessel and attache it to the current one
                if (ActiveVessel.id != FlightGlobals.ActiveVessel.id)
                {
                    ActiveVessel.OnPostAutopilotUpdate -= AxisInput;
                    ActiveVessel = FlightGlobals.ActiveVessel;
                    ActiveVessel.OnPostAutopilotUpdate += AxisInput;
                    //sync some inputs on vessel switch
                    ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.RCS, KSPSerialPort.VControls.RCS);
                    ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.SAS, KSPSerialPort.VControls.SAS);
                    Debug.Log("KSPSerialIO: ActiveVessel changed");
                }
                else
                {
                    ActiveVessel = FlightGlobals.ActiveVessel;
                }

                #region outputs
                theTime = Time.unscaledTime;
                if ((theTime - lastUpdate) > refreshrate)
                {
                    //Debug.Log("KSPSerialIO: 2");

                    lastUpdate = theTime;

                    List<Part> ActiveEngines = new List<Part>();
                    ActiveEngines = GetListOfActivatedEngines(ActiveVessel);

                    KSPSerialPort.VData.AP = (float)ActiveVessel.orbit.ApA;
                    KSPSerialPort.VData.PE = (float)ActiveVessel.orbit.PeA;
                    KSPSerialPort.VData.SemiMajorAxis = (float)ActiveVessel.orbit.semiMajorAxis;
                    KSPSerialPort.VData.SemiMinorAxis = (float)ActiveVessel.orbit.semiMinorAxis;
                    KSPSerialPort.VData.e = (float)ActiveVessel.orbit.eccentricity;
                    KSPSerialPort.VData.inc = (float)ActiveVessel.orbit.inclination;
                    KSPSerialPort.VData.VVI = (float)ActiveVessel.verticalSpeed;
                    KSPSerialPort.VData.G = (float)ActiveVessel.geeForce;
                    KSPSerialPort.VData.TAp = (int)Math.Round(ActiveVessel.orbit.timeToAp);
                    KSPSerialPort.VData.TPe = (int)Math.Round(ActiveVessel.orbit.timeToPe);
                    KSPSerialPort.VData.Density = (float)ActiveVessel.atmDensity;
                    KSPSerialPort.VData.TrueAnomaly = (float)ActiveVessel.orbit.trueAnomaly;
                    KSPSerialPort.VData.period = (int)Math.Round(ActiveVessel.orbit.period);

                    //Debug.Log("KSPSerialIO: 3");
                    double ASL = ActiveVessel.mainBody.GetAltitude(ActiveVessel.CoM);
                    double AGL = (ASL - ActiveVessel.terrainAltitude);

                    if (AGL < ASL)
                        KSPSerialPort.VData.RAlt = (float)AGL;
                    else
                        KSPSerialPort.VData.RAlt = (float)ASL;

                    KSPSerialPort.VData.Alt = (float)ASL;
                    KSPSerialPort.VData.Vsurf = (float)ActiveVessel.srfSpeed;
                    KSPSerialPort.VData.Lat = (float)ActiveVessel.latitude;
                    KSPSerialPort.VData.Lon = (float)ActiveVessel.longitude;

                    TempR = GetResourceTotal(ActiveVessel, "LiquidFuel");
                    KSPSerialPort.VData.LiquidFuelTot = TempR.Max;
                    KSPSerialPort.VData.LiquidFuel = TempR.Current;

                    KSPSerialPort.VData.LiquidFuelTotS = (float)ProspectForResourceMax("LiquidFuel", ActiveEngines);
                    KSPSerialPort.VData.LiquidFuelS = (float)ProspectForResource("LiquidFuel", ActiveEngines);

                    TempR = GetResourceTotal(ActiveVessel, "Oxidizer");
                    KSPSerialPort.VData.OxidizerTot = TempR.Max;
                    KSPSerialPort.VData.Oxidizer = TempR.Current;

                    KSPSerialPort.VData.OxidizerTotS = (float)ProspectForResourceMax("Oxidizer", ActiveEngines);
                    KSPSerialPort.VData.OxidizerS = (float)ProspectForResource("Oxidizer", ActiveEngines);

                    TempR = GetResourceTotal(ActiveVessel, "ElectricCharge");
                    KSPSerialPort.VData.EChargeTot = TempR.Max;
                    KSPSerialPort.VData.ECharge = TempR.Current;
                    TempR = GetResourceTotal(ActiveVessel, "MonoPropellant");
                    KSPSerialPort.VData.MonoPropTot = TempR.Max;
                    KSPSerialPort.VData.MonoProp = TempR.Current;
                    TempR = GetResourceTotal(ActiveVessel, "IntakeAir");
                    KSPSerialPort.VData.IntakeAirTot = TempR.Max;
                    KSPSerialPort.VData.IntakeAir = TempR.Current;
                    TempR = GetResourceTotal(ActiveVessel, "SolidFuel");
                    KSPSerialPort.VData.SolidFuelTot = TempR.Max;
                    KSPSerialPort.VData.SolidFuel = TempR.Current;
                    TempR = GetResourceTotal(ActiveVessel, "XenonGas");
                    KSPSerialPort.VData.XenonGasTot = TempR.Max;
                    KSPSerialPort.VData.XenonGas = TempR.Current;

                    missionTime = ActiveVessel.missionTime;
                    deltaT = missionTime - missionTimeOld;
                    missionTimeOld = missionTime;

                    KSPSerialPort.VData.MissionTime = (UInt32)Math.Round(missionTime);
                    KSPSerialPort.VData.deltaTime = (float)deltaT;

                    KSPSerialPort.VData.VOrbit = (float)ActiveVessel.orbit.GetVel().magnitude;

                    KSPSerialPort.VData.MNTime = 0;
                    KSPSerialPort.VData.MNDeltaV = 0;
                    
                    KSPSerialPort.VData.TargetDist = 0;
                    KSPSerialPort.VData.TargetV = 0;

                    //Debug.Log("KSPSerialIO: 5");

                    Vector3d CoM, north, up, east;
                    Quaternion rotationSurface;
                    CoM = ActiveVessel.CoM;
                    up = (CoM - ActiveVessel.mainBody.position).normalized;
                    north = Vector3d.Exclude(up, (ActiveVessel.mainBody.position + ActiveVessel.mainBody.transform.up * (float)ActiveVessel.mainBody.Radius) - CoM).normalized;
                    east = Vector3d.Cross(up, north);
                    rotationSurface = Quaternion.LookRotation(north, up);
                    Vector3d attitude = Quaternion.Inverse(Quaternion.Euler(90, 0, 0) * Quaternion.Inverse(ActiveVessel.GetTransform().rotation) * rotationSurface).eulerAngles;

                    KSPSerialPort.VData.Roll = (float)((attitude.z > 180) ? (attitude.z - 360.0) : attitude.z);
                    KSPSerialPort.VData.Pitch = (float)((attitude.x > 180) ? (360.0 - attitude.x) : -attitude.x);
                    KSPSerialPort.VData.Heading = (float)attitude.y;

                    Vector3d prograde = new Vector3d(0, 0, 0);
                    switch (FlightGlobals.speedDisplayMode)
                    {
                        case FlightGlobals.SpeedDisplayModes.Surface:
                            prograde = ActiveVessel.srf_velocity.normalized;
                            break;
                        case FlightGlobals.SpeedDisplayModes.Orbit:
                            prograde = ActiveVessel.obt_velocity.normalized;
                            break;
                        case FlightGlobals.SpeedDisplayModes.Target:
                            prograde = FlightGlobals.ship_tgtVelocity;
                            break;
                    }

                    NavHeading zeroHeading; zeroHeading.Pitch = zeroHeading.Heading = 0;
                    NavHeading Prograde = WorldVecToNavHeading(up, north, east, prograde), Target = zeroHeading, Maneuver = zeroHeading;

                    KSPSerialPort.VData.ProgradeHeading = FloatAngleToFixed_16(Prograde.Heading);
                    KSPSerialPort.VData.ProgradePitch = FloatAngleToFixed_16(Prograde.Pitch);

                    if (TargetExists())
                    {
                        KSPSerialPort.VData.TargetDist = (float)Vector3.Distance(FlightGlobals.fetch.VesselTarget.GetVessel().transform.position, ActiveVessel.transform.position);
                        KSPSerialPort.VData.TargetV = (float)FlightGlobals.ship_tgtVelocity.magnitude;
                        Target = WorldVecToNavHeading(up, north, east, ActiveVessel.targetObject.GetTransform().position - ActiveVessel.transform.position);
                    }
                    KSPSerialPort.VData.TargetHeading = FloatAngleToFixed_16(Target.Heading);
                    KSPSerialPort.VData.TargetPitch = FloatAngleToFixed_16(Target.Pitch);

                    KSPSerialPort.VData.NormalHeading = FloatAngleToFixed_16(WorldVecToNavHeading(up, north, east, Vector3d.Cross(ActiveVessel.obt_velocity.normalized, up)).Heading);

                    if (ActiveVessel.patchedConicSolver != null)
                    {
                        if (ActiveVessel.patchedConicSolver.maneuverNodes != null)
                        {
                            if (ActiveVessel.patchedConicSolver.maneuverNodes.Count > 0)
                            {
                                KSPSerialPort.VData.MNTime = (UInt32)Math.Round(ActiveVessel.patchedConicSolver.maneuverNodes[0].UT - Planetarium.GetUniversalTime());
                                KSPSerialPort.VData.MNDeltaV = (float)ActiveVessel.patchedConicSolver.maneuverNodes[0].GetBurnVector(ActiveVessel.patchedConicSolver.maneuverNodes[0].patch).magnitude; //Added JS

                                Maneuver = WorldVecToNavHeading(up, north, east, ActiveVessel.patchedConicSolver.maneuverNodes[0].GetBurnVector(ActiveVessel.patchedConicSolver.maneuverNodes[0].patch));
                            }
                        }
                    }
                    KSPSerialPort.VData.ManeuverHeading = FloatAngleToFixed_16(Maneuver.Heading);
                    KSPSerialPort.VData.ManeuverPitch = FloatAngleToFixed_16(Maneuver.Pitch);

                    KSPSerialPort.ControlStatus((int)EnumAG.SAS, ActiveVessel.ActionGroups[KSPActionGroup.SAS]);
                    KSPSerialPort.ControlStatus((int)EnumAG.RCS, ActiveVessel.ActionGroups[KSPActionGroup.RCS]);
                    KSPSerialPort.ControlStatus((int)EnumAG.Light, ActiveVessel.ActionGroups[KSPActionGroup.Light]);
                    KSPSerialPort.ControlStatus((int)EnumAG.Gear, ActiveVessel.ActionGroups[KSPActionGroup.Gear]);
                    KSPSerialPort.ControlStatus((int)EnumAG.Brakes, ActiveVessel.ActionGroups[KSPActionGroup.Brakes]);
                    KSPSerialPort.ControlStatus((int)EnumAG.Abort, ActiveVessel.ActionGroups[KSPActionGroup.Abort]);
                    KSPSerialPort.ControlStatus((int)EnumAG.Custom01, ActiveVessel.ActionGroups[KSPActionGroup.Custom01]);
                    KSPSerialPort.ControlStatus((int)EnumAG.Custom02, ActiveVessel.ActionGroups[KSPActionGroup.Custom02]);
                    KSPSerialPort.ControlStatus((int)EnumAG.Custom03, ActiveVessel.ActionGroups[KSPActionGroup.Custom03]);
                    KSPSerialPort.ControlStatus((int)EnumAG.Custom04, ActiveVessel.ActionGroups[KSPActionGroup.Custom04]);
                    KSPSerialPort.ControlStatus((int)EnumAG.Custom05, ActiveVessel.ActionGroups[KSPActionGroup.Custom05]);
                    KSPSerialPort.ControlStatus((int)EnumAG.Custom06, ActiveVessel.ActionGroups[KSPActionGroup.Custom06]);
                    KSPSerialPort.ControlStatus((int)EnumAG.Custom07, ActiveVessel.ActionGroups[KSPActionGroup.Custom07]);
                    KSPSerialPort.ControlStatus((int)EnumAG.Custom08, ActiveVessel.ActionGroups[KSPActionGroup.Custom08]);
                    KSPSerialPort.ControlStatus((int)EnumAG.Custom09, ActiveVessel.ActionGroups[KSPActionGroup.Custom09]);
                    KSPSerialPort.ControlStatus((int)EnumAG.Custom10, ActiveVessel.ActionGroups[KSPActionGroup.Custom10]);

                    if (ActiveVessel.orbit.referenceBody != null)
                    {
                        KSPSerialPort.VData.SOINumber = GetSOINumber(ActiveVessel.orbit.referenceBody.name);
                    }

                    KSPSerialPort.VData.MaxOverHeat = GetMaxOverHeat(ActiveVessel);
                    KSPSerialPort.VData.MachNumber = (float)ActiveVessel.mach;
                    KSPSerialPort.VData.IAS = (float)ActiveVessel.indicatedAirSpeed;

                    KSPSerialPort.VData.CurrentStage = (byte)StageManager.CurrentStage;
                    KSPSerialPort.VData.TotalStage = (byte)StageManager.StageCount;

                    KSPSerialPort.VData.NavballSASMode = (byte)(((int)FlightGlobals.speedDisplayMode + 1) << 4); //get navball speed display mode
                    if (ActiveVessel.ActionGroups[KSPActionGroup.SAS])
                    {
                        KSPSerialPort.VData.NavballSASMode = (byte)(((int)FlightGlobals.ActiveVessel.Autopilot.Mode + 1) | KSPSerialPort.VData.NavballSASMode);
                    }

                    #region debugjunk

                    /*
                    Debug.Log("KSPSerialIO: Stage " + KSPSerialPort.VData.CurrentStage.ToString() + ' ' +
                        KSPSerialPort.VData.TotalStage.ToString()); 
                    Debug.Log("KSPSerialIO: Overheat " + KSPSerialPort.VData.MaxOverHeat.ToString());
                    Debug.Log("KSPSerialIO: Mach " + KSPSerialPort.VData.MachNumber.ToString());
                    Debug.Log("KSPSerialIO: IAS " + KSPSerialPort.VData.IAS.ToString());
                    
                    Debug.Log("KSPSerialIO: SOI " + ActiveVessel.orbit.referenceBody.name + KSPSerialPort.VData.SOINumber.ToString());
                    
                    ScreenMessages.PostScreenMessage(KSPSerialPort.VData.OxidizerS.ToString() + "/" + KSPSerialPort.VData.OxidizerTotS +
                        "   " + KSPSerialPort.VData.Oxidizer.ToString() + "/" + KSPSerialPort.VData.OxidizerTot);
                    */
                    //KSPSerialPort.VData.Roll = Mathf.Atan2(2 * (x * y + w * z), w * w + x * x - y * y - z * z) * 180 / Mathf.PI;
                    //KSPSerialPort.VData.Pitch = Mathf.Atan2(2 * (y * z + w * x), w * w - x * x - y * y + z * z) * 180 / Mathf.PI;
                    //KSPSerialPort.VData.Heading = Mathf.Asin(-2 * (x * z - w * y)) *180 / Mathf.PI;
                    //Debug.Log("KSPSerialIO: Roll    " + KSPSerialPort.VData.Roll.ToString());
                    //Debug.Log("KSPSerialIO: Pitch   " + KSPSerialPort.VData.Pitch.ToString());
                    //Debug.Log("KSPSerialIO: Heading " + KSPSerialPort.VData.Heading.ToString());
                    //Debug.Log("KSPSerialIO: VOrbit" + KSPSerialPort.VData.VOrbit.ToString());
                    //ScreenMessages.PostScreenMessage(ActiveVessel.ActionGroups[KSPActionGroup.RCS].ToString());
                    //Debug.Log("KSPSerialIO: MNTime" + KSPSerialPort.VData.MNTime.ToString() + " MNDeltaV" + KSPSerialPort.VData.MNDeltaV.ToString());
                    //Debug.Log("KSPSerialIO: Time" + KSPSerialPort.VData.MissionTime.ToString() + " Delta Time" + KSPSerialPort.VData.deltaTime.ToString());
                    //Debug.Log("KSPSerialIO: Throttle = " + KSPSerialPort.CPacket.Throttle.ToString());
                    //ScreenMessages.PostScreenMessage(KSPSerialPort.VData.Fuelp.ToString());
                    //ScreenMessages.PostScreenMessage(KSPSerialPort.VData.RAlt.ToString());
                    //KSPSerialPort.Port.WriteLine("Success!");
                    /*
                    ScreenMessages.PostScreenMessage(KSPSerialPort.VData.LiquidFuelS.ToString() + "/" + KSPSerialPort.VData.LiquidFuelTotS +
                        "   " + KSPSerialPort.VData.LiquidFuel.ToString() + "/" + KSPSerialPort.VData.LiquidFuelTot);
                    
                    ScreenMessages.PostScreenMessage("MNTime " + KSPSerialPort.VData.MNTime.ToString() + " MNDeltaV " + KSPSerialPort.VData.MNDeltaV.ToString());
                    ScreenMessages.PostScreenMessage("TargetDist " + KSPSerialPort.VData.TargetDist.ToString() + " TargetV " + KSPSerialPort.VData.TargetV.ToString());
                     */
                    #endregion
                    KSPSerialPort.sendPacket(KSPSerialPort.VData);
                } //end refresh
                #endregion
                #region inputs
                if (KSPSerialPort.ControlReceived)
                {
                    /*
                    ScreenMessages.PostScreenMessage("Nav Mode " + KSPSerialPort.CPacket.NavballSASMode.ToString());
                    
                     ScreenMessages.PostScreenMessage("SAS: " + KSPSerialPort.VControls.SAS.ToString() +
                     ", RCS: " + KSPSerialPort.VControls.RCS.ToString() +
                     ", Lights: " + KSPSerialPort.VControls.Lights.ToString() +
                     ", Gear: " + KSPSerialPort.VControls.Gear.ToString() +
                     ", Brakes: " + KSPSerialPort.VControls.Brakes.ToString() +
                     ", Precision: " + KSPSerialPort.VControls.Precision.ToString() +
                     ", Abort: " + KSPSerialPort.VControls.Abort.ToString() +
                     ", Stage: " + KSPSerialPort.VControls.Stage.ToString(), 10f, KSPIOScreenStyle);
                    
                     Debug.Log("KSPSerialIO: SAS: " + KSPSerialPort.VControls.SAS.ToString() +
                     ", RCS: " + KSPSerialPort.VControls.RCS.ToString() +
                     ", Lights: " + KSPSerialPort.VControls.Lights.ToString() +
                     ", Gear: " + KSPSerialPort.VControls.Gear.ToString() +
                     ", Brakes: " + KSPSerialPort.VControls.Brakes.ToString() +
                     ", Precision: " + KSPSerialPort.VControls.Precision.ToString() +
                     ", Abort: " + KSPSerialPort.VControls.Abort.ToString() +
                     ", Stage: " + KSPSerialPort.VControls.Stage.ToString());
                     */

                    //if (FlightInputHandler.RCSLock != KSPSerialPort.VControls.RCS)
                    if (KSPSerialPort.VControls.RCS != KSPSerialPort.VControlsOld.RCS)
                    {
                        ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.RCS, KSPSerialPort.VControls.RCS);
                        KSPSerialPort.VControlsOld.RCS = KSPSerialPort.VControls.RCS;
                        //ScreenMessages.PostScreenMessage("RCS: " + KSPSerialPort.VControls.RCS.ToString(), 10f, KSPIOScreenStyle);
                    }

                    //if (ActiveVessel.ctrlState.killRot != KSPSerialPort.VControls.SAS)
                    if (KSPSerialPort.VControls.SAS != KSPSerialPort.VControlsOld.SAS)
                    {
                        ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.SAS, KSPSerialPort.VControls.SAS);
                        KSPSerialPort.VControlsOld.SAS = KSPSerialPort.VControls.SAS;
                        //ScreenMessages.PostScreenMessage("SAS: " + KSPSerialPort.VControls.SAS.ToString(), 10f, KSPIOScreenStyle);
                    }

                    if (KSPSerialPort.VControls.Lights != KSPSerialPort.VControlsOld.Lights)
                    {
                        ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.Light, KSPSerialPort.VControls.Lights);
                        KSPSerialPort.VControlsOld.Lights = KSPSerialPort.VControls.Lights;
                    }

                    if (KSPSerialPort.VControls.Gear != KSPSerialPort.VControlsOld.Gear)
                    {
                        ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.Gear, KSPSerialPort.VControls.Gear);
                        KSPSerialPort.VControlsOld.Gear = KSPSerialPort.VControls.Gear;
                    }

                    if (KSPSerialPort.VControls.Brakes != KSPSerialPort.VControlsOld.Brakes)
                    {
                        ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, KSPSerialPort.VControls.Brakes);
                        KSPSerialPort.VControlsOld.Brakes = KSPSerialPort.VControls.Brakes;
                    }

                    if (KSPSerialPort.VControls.Abort != KSPSerialPort.VControlsOld.Abort)
                    {
                        ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.Abort, KSPSerialPort.VControls.Abort);
                        KSPSerialPort.VControlsOld.Abort = KSPSerialPort.VControls.Abort;
                    }

                    if (KSPSerialPort.VControls.Stage != KSPSerialPort.VControlsOld.Stage)
                    {
                        if (KSPSerialPort.VControls.Stage)
                            StageManager.ActivateNextStage();

                        ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.Stage, KSPSerialPort.VControls.Stage);
                        KSPSerialPort.VControlsOld.Stage = KSPSerialPort.VControls.Stage;
                    }

                    //================ control groups

                    if (KSPSerialPort.VControls.ControlGroup[1] != KSPSerialPort.VControlsOld.ControlGroup[1])
                    {
                        ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.Custom01, KSPSerialPort.VControls.ControlGroup[1]);
                        KSPSerialPort.VControlsOld.ControlGroup[1] = KSPSerialPort.VControls.ControlGroup[1];
                    }

                    if (KSPSerialPort.VControls.ControlGroup[2] != KSPSerialPort.VControlsOld.ControlGroup[2])
                    {
                        ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.Custom02, KSPSerialPort.VControls.ControlGroup[2]);
                        KSPSerialPort.VControlsOld.ControlGroup[2] = KSPSerialPort.VControls.ControlGroup[2];
                    }

                    if (KSPSerialPort.VControls.ControlGroup[3] != KSPSerialPort.VControlsOld.ControlGroup[3])
                    {
                        ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.Custom03, KSPSerialPort.VControls.ControlGroup[3]);
                        KSPSerialPort.VControlsOld.ControlGroup[3] = KSPSerialPort.VControls.ControlGroup[3];
                    }

                    if (KSPSerialPort.VControls.ControlGroup[4] != KSPSerialPort.VControlsOld.ControlGroup[4])
                    {
                        ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.Custom04, KSPSerialPort.VControls.ControlGroup[4]);
                        KSPSerialPort.VControlsOld.ControlGroup[4] = KSPSerialPort.VControls.ControlGroup[4];
                    }

                    if (KSPSerialPort.VControls.ControlGroup[5] != KSPSerialPort.VControlsOld.ControlGroup[5])
                    {
                        ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.Custom05, KSPSerialPort.VControls.ControlGroup[5]);
                        KSPSerialPort.VControlsOld.ControlGroup[5] = KSPSerialPort.VControls.ControlGroup[5];
                    }

                    if (KSPSerialPort.VControls.ControlGroup[6] != KSPSerialPort.VControlsOld.ControlGroup[6])
                    {
                        ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.Custom06, KSPSerialPort.VControls.ControlGroup[6]);
                        KSPSerialPort.VControlsOld.ControlGroup[6] = KSPSerialPort.VControls.ControlGroup[6];
                    }

                    if (KSPSerialPort.VControls.ControlGroup[7] != KSPSerialPort.VControlsOld.ControlGroup[7])
                    {
                        ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.Custom07, KSPSerialPort.VControls.ControlGroup[7]);
                        KSPSerialPort.VControlsOld.ControlGroup[7] = KSPSerialPort.VControls.ControlGroup[7];
                    }

                    if (KSPSerialPort.VControls.ControlGroup[8] != KSPSerialPort.VControlsOld.ControlGroup[8])
                    {
                        ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.Custom08, KSPSerialPort.VControls.ControlGroup[8]);
                        KSPSerialPort.VControlsOld.ControlGroup[8] = KSPSerialPort.VControls.ControlGroup[8];
                    }

                    if (KSPSerialPort.VControls.ControlGroup[9] != KSPSerialPort.VControlsOld.ControlGroup[9])
                    {
                        ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.Custom09, KSPSerialPort.VControls.ControlGroup[9]);
                        KSPSerialPort.VControlsOld.ControlGroup[9] = KSPSerialPort.VControls.ControlGroup[9];
                    }

                    if (KSPSerialPort.VControls.ControlGroup[10] != KSPSerialPort.VControlsOld.ControlGroup[10])
                    {
                        ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.Custom10, KSPSerialPort.VControls.ControlGroup[10]);
                        KSPSerialPort.VControlsOld.ControlGroup[10] = KSPSerialPort.VControls.ControlGroup[10];
                    }

                    //Set sas mode
                    if (KSPSerialPort.VControls.SASMode != KSPSerialPort.VControlsOld.SASMode)
                    {
                        if (KSPSerialPort.VControls.SASMode != 0 && KSPSerialPort.VControls.SASMode < 11)
                        {
                            if (!ActiveVessel.Autopilot.CanSetMode((VesselAutopilot.AutopilotMode)(KSPSerialPort.VControls.SASMode - 1)))
                            {
                                ScreenMessages.PostScreenMessage("KSPSerialIO: SAS mode " + KSPSerialPort.VControls.SASMode.ToString() + " not avalible");
                            }
                            else
                            {
                                ActiveVessel.Autopilot.SetMode((VesselAutopilot.AutopilotMode)KSPSerialPort.VControls.SASMode - 1);
                            }
                        }
                        KSPSerialPort.VControlsOld.SASMode = KSPSerialPort.VControls.SASMode;
                    }

                    //set navball mode
                    if (KSPSerialPort.VControls.SpeedMode != KSPSerialPort.VControlsOld.SpeedMode)
                    {
                        if (!((KSPSerialPort.VControls.SpeedMode == 0) || ((KSPSerialPort.VControls.SpeedMode == 3) && !TargetExists())))
                        {
                            FlightGlobals.SetSpeedMode((FlightGlobals.SpeedDisplayModes)(KSPSerialPort.VControls.SpeedMode - 1));
                        }
                        KSPSerialPort.VControlsOld.SpeedMode = KSPSerialPort.VControls.SpeedMode;
                    }


                    /* Getting rid of this per c4ooo suggestions
                    if (Math.Abs(KSPSerialPort.VControls.Pitch) > SettingsNStuff.SASTol ||
                    Math.Abs(KSPSerialPort.VControls.Roll) > SettingsNStuff.SASTol ||
                    Math.Abs(KSPSerialPort.VControls.Yaw) > SettingsNStuff.SASTol)
                    {
                        //ActiveVessel.Autopilot.SAS.ManualOverride(true); 

                        if ((ActiveVessel.ActionGroups[KSPActionGroup.SAS]) && (wasSASOn == false))
                        {
                            wasSASOn = true;
                            ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.SAS, false);
                        }

                        //ScreenMessages.PostScreenMessage("KSPSerialIO: SAS mode " + wasSASOn);

                        
                        //if (wasSASOn == true)
                        //{                            
                            //ActiveVessel.Autopilot.SAS.lockedMode = false;
                            //ActiveVessel.Autopilot.SAS.dampingMode = true;
                        //}
                                                                      
                        
                        //if (KSPSerialPort.VControls.SAS == true)
                        //{
                        //    KSPSerialPort.VControls.SAS = false;
                        //    KSPSerialPort.VControlsOld.SAS = false;
                        //}
                        
                        //KSPSerialPort.VControlsOld.Pitch = KSPSerialPort.VControls.Pitch;
                        //KSPSerialPort.VControlsOld.Roll = KSPSerialPort.VControls.Roll;
                        //KSPSerialPort.VControlsOld.Yaw = KSPSerialPort.VControls.Yaw;
                    }
                    else
                    {
                        if (wasSASOn == true)
                        {
                            wasSASOn = false;
                            ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.SAS, true);
                            //ActiveVessel.Autopilot.SAS.lockedMode = true;
                            //ActiveVessel.Autopilot.SAS.dampingMode = false;
                        }
                    }
                    */
                

                    KSPSerialPort.ControlReceived = false;
                } //end ControlReceived
                #endregion


            }//end if null and same vessel
            else
            {
                //Debug.Log("KSPSerialIO: ActiveVessel not found");
                //ActiveVessel.OnFlyByWire -= new FlightInputCallback(AxisInput);
            }

        }

        #region utilities

        //lazy version
        private static short FloatAngleToFixed_16(float f) //convert a number in the range of [-360,360) to [-0x8000,0x7FFF)
        {
            return (short)Math.Round(f * 50.0f);
        }

        /*
        private static short FloatAngleToFixed_16(float f) //convert a number in the range of [-360,360) to [-0x8000,0x7FFF)
        {
            return (short)(((int)(f / 360.0f * (float)(0x8000))) & 0xFFFF);
        }
        */
        private static NavHeading WorldVecToNavHeading(Vector3d up, Vector3d north, Vector3d east, Vector3d v)
        {
            NavHeading ret = new NavHeading();
            ret.Pitch = (float)-((Vector3d.Angle(up, v)) - 90.0f);
            Vector3d progradeFlat = Vector3d.Exclude(up, v);
            float NAngle = (float)Vector3d.Angle(north, progradeFlat);
            float EAngle = (float)Vector3d.Angle(east, progradeFlat);
            if (EAngle < 90)
                ret.Heading = NAngle;
            else
                ret.Heading = -NAngle + 360;
            return ret;
        }

        private Boolean TargetExists()
        {
            return (FlightGlobals.fetch.VesselTarget != null) && (FlightGlobals.fetch.VesselTarget.GetVessel() != null); //&& is short circuiting
        }

        private byte GetMaxOverHeat(Vessel V)
        {
            byte percent = 0;
            double sPercent = 0, iPercent = 0;
            double percentD = 0, percentP = 0;

            foreach (Part p in ActiveVessel.parts)
            {
                //internal temperature
                iPercent = p.temperature / p.maxTemp;
                //skin temperature
                sPercent = p.skinTemperature / p.skinMaxTemp;

                if (iPercent > sPercent)
                    percentP = iPercent;
                else
                    percentP = sPercent;

                if (percentD < percentP)
                    percentD = percentP;
            }

            percent = (byte)Math.Round(percentD * 100);
            return percent;
        }


        private IOResource GetResourceTotal(Vessel V, string resourceName)
        {
            IOResource R = new IOResource();

            foreach (Part p in V.parts)
            {
                foreach (PartResource pr in p.Resources)
                {
                    if (pr.resourceName.Equals(resourceName))
                    {
                        R.Current += (float)pr.amount;
                        R.Max += (float)pr.maxAmount;

                        /* shit doesn't work
                        int stageno = p.inverseStage;
                        
                        Debug.Log(pr.resourceName + "  " + stageno.ToString() + "  " + Staging.CurrentStage.ToString());
                        //if (p.inverseStage == Staging.CurrentStage + 1)
                        if (stageno == Staging.CurrentStage)
                        {                            
                            R.CurrentStage += (float)pr.amount;
                            R.MaxStage += (float)pr.maxAmount;
                        }
                         */
                        break;
                    }
                }
            }

            if (R.Max == 0)
                R.Current = 0;

            return R;
        }

        private void AxisInput(FlightCtrlState s)
        {
            switch (SettingsNStuff.ThrottleEnable)
            {
                case 1:
                    s.mainThrottle = KSPSerialPort.VControls.Throttle;
                    break;
                case 2:
                    if (s.mainThrottle == 0)
                    {
                        s.mainThrottle = KSPSerialPort.VControls.Throttle;
                    }
                    break;
                case 3:
                    if (KSPSerialPort.VControls.Throttle != 0)
                    {
                        s.mainThrottle = KSPSerialPort.VControls.Throttle;
                    }
                    break;
                default:
                    break;
            }

            switch (SettingsNStuff.PitchEnable)
            {
                case 1:
                    s.pitch = KSPSerialPort.VControls.Pitch;
                    break;
                case 2:
                    if (s.pitch == 0)
                        s.pitch = KSPSerialPort.VControls.Pitch;
                    break;
                case 3:
                    if (KSPSerialPort.VControls.Pitch != 0)
                        s.pitch = KSPSerialPort.VControls.Pitch;
                    break;
                default:
                    break;
            }

            switch (SettingsNStuff.RollEnable)
            {
                case 1:
                    s.roll = KSPSerialPort.VControls.Roll;
                    break;
                case 2:
                    if (s.roll == 0)
                        s.roll = KSPSerialPort.VControls.Roll;
                    break;
                case 3:
                    if (KSPSerialPort.VControls.Roll != 0)
                        s.roll = KSPSerialPort.VControls.Roll;
                    break;
                default:
                    break;
            }

            switch (SettingsNStuff.YawEnable)
            {
                case 1:
                    s.yaw = KSPSerialPort.VControls.Yaw;
                    break;
                case 2:
                    if (s.yaw == 0)
                        s.yaw = KSPSerialPort.VControls.Yaw;
                    break;
                case 3:
                    if (KSPSerialPort.VControls.Yaw != 0)
                        s.yaw = KSPSerialPort.VControls.Yaw;
                    break;
                default:
                    break;
            }
            /*
            if (ActiveVessel.Autopilot.SAS.lockedMode == true)
            {
            }
            */
            switch (SettingsNStuff.TXEnable)
            {
                case 1:
                    s.X = KSPSerialPort.VControls.TX;
                    break;
                case 2:
                    if (s.X == 0)
                        s.X = KSPSerialPort.VControls.TX;
                    break;
                case 3:
                    if (KSPSerialPort.VControls.TX != 0)
                        s.X = KSPSerialPort.VControls.TX;
                    break;
                default:
                    break;
            }

            switch (SettingsNStuff.TYEnable)
            {
                case 1:
                    s.Y = KSPSerialPort.VControls.TY;
                    break;
                case 2:
                    if (s.Y == 0)
                        s.Y = KSPSerialPort.VControls.TY;
                    break;
                case 3:
                    if (KSPSerialPort.VControls.TY != 0)
                        s.Y = KSPSerialPort.VControls.TY;
                    break;
                default:
                    break;
            }

            switch (SettingsNStuff.TZEnable)
            {
                case 1:
                    s.Z = KSPSerialPort.VControls.TZ;
                    break;
                case 2:
                    if (s.Z == 0)
                        s.Z = KSPSerialPort.VControls.TZ;
                    break;
                case 3:
                    if (KSPSerialPort.VControls.TZ != 0)
                        s.Z = KSPSerialPort.VControls.TZ;
                    break;
                default:
                    break;
            }

            switch (SettingsNStuff.WheelSteerEnable)
            {
                case 1:
                    s.wheelSteer = KSPSerialPort.VControls.WheelSteer;
                    break;
                case 2:
                    if (s.wheelSteer == 0)
                    {
                        s.wheelSteer = KSPSerialPort.VControls.WheelSteer;
                    }
                    break;
                case 3:
                    if (KSPSerialPort.VControls.WheelSteer != 0)
                    {
                        s.wheelSteer = KSPSerialPort.VControls.WheelSteer;
                    }
                    break;
                default:
                    break;
            }

            switch (SettingsNStuff.WheelThrottleEnable)
            {
                case 1:
                    s.wheelThrottle = KSPSerialPort.VControls.WheelThrottle;
                    break;
                case 2:
                    if (s.wheelThrottle == 0)
                    {
                        s.wheelThrottle = KSPSerialPort.VControls.WheelThrottle;
                    }
                    break;
                case 3:
                    if (KSPSerialPort.VControls.WheelThrottle != 0)
                    {
                        s.wheelThrottle = KSPSerialPort.VControls.WheelThrottle;
                    }
                    break;
                default:
                    break;
            }
        }

        private byte GetSOINumber(string name)
        {
            byte SOI;

            switch (name.ToLower())
            {
                case "sun":
                    SOI = 100;
                    break;
                case "moho":
                    SOI = 110;
                    break;
                case "eve":
                    SOI = 120;
                    break;
                case "gilly":
                    SOI = 121;
                    break;
                case "kerbin":
                    SOI = 130;
                    break;
                case "mun":
                    SOI = 131;
                    break;
                case "minmus":
                    SOI = 132;
                    break;
                case "duna":
                    SOI = 140;
                    break;
                case "ike":
                    SOI = 141;
                    break;
                case "dres":
                    SOI = 150;
                    break;
                case "jool":
                    SOI = 160;
                    break;
                case "laythe":
                    SOI = 161;
                    break;
                case "vall":
                    SOI = 162;
                    break;
                case "tylo":
                    SOI = 163;
                    break;
                case "bop":
                    SOI = 164;
                    break;
                case "pol":
                    SOI = 165;
                    break;
                case "eeloo":
                    SOI = 170;
                    break;
                default:
                    SOI = 0;
                    break;
            }
            return SOI;
        }

        // this recursive stage look up stuff stolen and modified from KOS and others
        public static List<Part> GetListOfActivatedEngines(Vessel vessel)
        {
            var retList = new List<Part>();

            foreach (var part in vessel.Parts)
            {
                foreach (PartModule module in part.Modules)
                {
                    var engineModule = module as ModuleEngines;
                    if (engineModule != null)
                    {
                        if (engineModule.getIgnitionState)
                        {
                            retList.Add(part);
                        }
                    }

                    var engineModuleFx = module as ModuleEnginesFX;
                    if (engineModuleFx != null)
                    {
                        var engineMod = engineModuleFx;
                        if (engineModuleFx.getIgnitionState)
                        {
                            retList.Add(part);
                        }
                    }
                }
            }

            return retList;
        }

        public static double ProspectForResource(String resourceName, List<Part> engines)
        {
            List<Part> visited = new List<Part>();
            double total = 0;

            foreach (var part in engines)
            {
                total += ProspectForResource(resourceName, part, ref visited);
            }

            return total;
        }

        public static double ProspectForResource(String resourceName, Part engine)
        {
            List<Part> visited = new List<Part>();

            return ProspectForResource(resourceName, engine, ref visited);
        }

        public static double ProspectForResource(String resourceName, Part part, ref List<Part> visited)
        {
            double ret = 0;

            if (visited.Contains(part))
            {
                return 0;
            }

            visited.Add(part);

            foreach (PartResource resource in part.Resources)
            {
                if (resource.resourceName.ToLower() == resourceName.ToLower())
                {
                    ret += resource.amount;
                }
            }

            foreach (AttachNode attachNode in part.attachNodes)
            {
                if (attachNode.attachedPart != null //if there is a part attached here
                        && attachNode.nodeType == AttachNode.NodeType.Stack //and the attached part is stacked (rather than surface mounted)
                        && (attachNode.attachedPart.fuelCrossFeed //and the attached part allows fuel flow
                            )
                        && !(part.NoCrossFeedNodeKey.Length > 0 //and this part does not forbid fuel flow
                                && attachNode.id.Contains(part.NoCrossFeedNodeKey))) // through this particular node
                {


                    ret += ProspectForResource(resourceName, attachNode.attachedPart, ref visited);
                }
            }

            return ret;
        }

        public static double ProspectForResourceMax(String resourceName, List<Part> engines)
        {
            List<Part> visited = new List<Part>();
            double total = 0;

            foreach (var part in engines)
            {
                total += ProspectForResourceMax(resourceName, part, ref visited);
            }

            return total;
        }

        public static double ProspectForResourceMax(String resourceName, Part engine)
        {
            List<Part> visited = new List<Part>();

            return ProspectForResourceMax(resourceName, engine, ref visited);
        }

        public static double ProspectForResourceMax(String resourceName, Part part, ref List<Part> visited)
        {
            double ret = 0;

            if (visited.Contains(part))
            {
                return 0;
            }

            visited.Add(part);

            foreach (PartResource resource in part.Resources)
            {
                if (resource.resourceName.ToLower() == resourceName.ToLower())
                {
                    ret += resource.maxAmount;
                }
            }

            foreach (AttachNode attachNode in part.attachNodes)
            {
                if (attachNode.attachedPart != null //if there is a part attached here
                        && attachNode.nodeType == AttachNode.NodeType.Stack //and the attached part is stacked (rather than surface mounted)
                        && (attachNode.attachedPart.fuelCrossFeed //and the attached part allows fuel flow
                            )
                        && !(part.NoCrossFeedNodeKey.Length > 0 //and this part does not forbid fuel flow
                                && attachNode.id.Contains(part.NoCrossFeedNodeKey))) // through this particular node
                {


                    ret += ProspectForResourceMax(resourceName, attachNode.attachedPart, ref visited);
                }
            }

            return ret;
        }

        #endregion

        void FixedUpdate()
        {
        }

        void OnDestroy()
        {
            if (KSPSerialPort.Port.IsOpen)
            {
                KSPSerialPort.Port.Close();
                ScreenMessages.PostScreenMessage("Port closed", 10f, KSPIOScreenStyle);
            }

            ActiveVessel.OnFlyByWire -= new FlightInputCallback(AxisInput);
        }
    }
}
