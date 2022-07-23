using System;
using System.Globalization;
using System.IO.Ports;
using System.Timers;
using System.ComponentModel;
using System.Text.RegularExpressions;

using System.Net;
using System.Net.Sockets;
using System.Threading;
using Timer = System.Timers.Timer;
using System.Windows.Threading;

namespace PC_XTREM
{

    public class Xtrem : INotifyPropertyChanged
    {

        //events
        public event EventHandler WeightChanged;
        public event EventHandler Recallscaledef;
        public event EventHandler NameChanged;
        public event EventHandler NewStableWeight;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyname)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyname));
        }

        public event EventHandler ConnectProgress;

        //UDP comms
        public bool Udp = false;
        public UdpClient Listener;

        public Thread ReadUdpData;
        public bool StopThread = false;

        private IPEndPoint udpSendEndpoint;
        public IPEndPoint UdpSendEndpoint { get => udpSendEndpoint; set => udpSendEndpoint = value; }
        
        public int UdpRecPort;

        public bool IsWaitingData = true;

        //end UDP comms


        //communications error
        public Timer aTimer;
        private bool isNotConnected;
        private static bool ConnectState;
        public string rx_buffer = "";
        private static bool scale_info = false;


        //serial port
        private SerialPort comPort;
        private string outputRate = "";
        private int id = 0xff;
        private int baudRateCode;

        //Get Adc counts
        public Timer AdcCtsTimer;
        public bool GetAdcCts = false;
        public bool GetVinCts = false;
        public bool GetXTemp = false;

        //Scale model and serial number
        private string type;

        private string serialNumber;
        private string firmwareVersion;
        private string sealSwitchState;


        //Scale definition
        private int curEsc;
        private int curMax;
        private int decimalPlaces;
        public int ResFactor;

        private string negativeWeight;

        //private string rangeMode;
        private int rangeModeCode;
        private string definitionString;

        //adjust information
        private long initZero;
        private double slopeFactor;
        private string vEsc;
        private string vInput;
        private int ctsEsc;
        private long adcCts;
        private string maxCounts;
                
        //geo adjust
        private string geo_Local;
        private string geo_Adjust;

        //weighing information
        private double w_Brut = 0;
        private double w_Tare = 0;
        private double w_Net = 0;
        private string w_Display = "";
        private string w_Unit = "";
        private int w_UnitCode = 0;

        private bool w_Flag_Zero = false;
        private bool w_Flag_Tare = false;
        private bool w_Flag_Stability = false;

        private DispatcherTimer StabilityTimer;
        private TimeSpan StabilityElapsedTime;
        private double stabilityTime;
        public double StabilityTime
        {
            get => stabilityTime;
            set
            {
                stabilityTime = value;
                OnPropertyChanged("StabilityTime");
            }
        }

        
        private bool w_Flag_NetoDisp = false;
        private bool w_Flag_HighRes = false;
        public bool W_Flag_InitZero = false;
        public bool W_Flag_ManualTare = false;

        public int W_Flag_TareMode = 0;
        //public int W_Range = 0;

        public static bool holdWeightChange;
        public double W_Hold;
        public bool HoldMode = false;

        private static readonly string[] Unit = { "", "g ", "kg", "oz", "lb" };
        //private static readonly string[] R_Mode = { "Single range", "2 ranges", "2 intervals" };

        public string SerialNumber 
        {
            get => serialNumber;
            set 
            {
                
                if (Udp == false && comPort == null)
                {
                    return;
                }

                if ((serialNumber == null && value != null))
                {
                    serialNumber = value;
                    OnPropertyChanged("SerialNumber");
                    return;
                }

                if (serialNumber == value)
                {
                    //serialNumber = value;
                    OnPropertyChanged("SerialNumber");
                    return;
                }

                string old_value = serialNumber;

                int verif = WriteParam(0, value);


                if (verif == 0)
                {
                    serialNumber = value;
                    
                    
                }
                else
                {
                    serialNumber = old_value;
                }

                OnPropertyChanged("SerialNumber");

                
            }
        }

        public double W_Net
        {
            get => w_Net;
            set 
            { 
                w_Net = value;
                OnPropertyChanged("W_Net");
            }
        }

        public double W_Tare
        {
            get => w_Tare;
            set
            {
                w_Tare = value;
                OnPropertyChanged("W_Tare");
            }
        }


        public double W_Brut
        {
            get => w_Brut;
            set
            {
                w_Brut = value;
                OnPropertyChanged("W_Brut");
            }
        }

        public bool W_Flag_Stability 
        { 
            get => w_Flag_Stability;
            set
            {
                w_Flag_Stability = value;
                OnPropertyChanged("W_Flag_Stability");
                
            }
        }

        public bool W_Flag_Zero { 
            get => w_Flag_Zero;
            set 
            { 
                w_Flag_Zero = value;
                OnPropertyChanged("W_Flag_Zero");
            }
        }

        public bool W_Flag_Tare 
        { 
            get => w_Flag_Tare;
            set
            {
                w_Flag_Tare = value;
                OnPropertyChanged("W_Flag_Tare");
            }
        }

        public bool W_Flag_NetoDisp
        { 
            get => w_Flag_NetoDisp;
            set
            {
                w_Flag_NetoDisp = value;
                OnPropertyChanged("W_Flag_NetoDisp");
            }
        }

        public bool W_Flag_HighRes
        { 
            get => w_Flag_HighRes;
            set
            {
                w_Flag_HighRes = value;
                OnPropertyChanged("W_Flag_HighRes");
            }
        }

        public string Type 
        { 
            get => type;
            set
            {
                type = value;
                OnPropertyChanged("Type");
            }
        }

        public string DefinitionString 
        { 
            get => definitionString;
            set
            {
                definitionString = value;
                OnPropertyChanged("DefinitionString");
            }
        }


        //adjust information
        //private long initZero;
        public long InitZero
        {
            get => initZero;
            set
            {
                if (Udp == false && comPort == null)
                {
                    return;
                }
                                
                if (initZero == value)
                {
                    //initZero = value;
                    OnPropertyChanged("InitZero");
                    return;
                }

                long _old = initZero;

                string val = string.Format("{0}", value);

                var verif = WriteParam(0x30, val);

                if (verif == 0)
                {
                    initZero = value;
                    
                }
                else
                {
                    initZero = _old;
                }

                OnPropertyChanged("InitZero");
            }
        }



        public string MaxCounts
        {
            get => maxCounts;
            set
            {
                if (Udp == false && comPort == null)
                {
                    return;
                }

                if (maxCounts == null)
                {
                    maxCounts = value;
                    OnPropertyChanged("MaxCounts");
                    return;
                }


                if (maxCounts == value)
                {
                    //maxCounts = value;
                    OnPropertyChanged("MaxCounts");
                    return;
                }


                string _old = maxCounts;
                               
                var verif = WriteParam(0x32, value);

                if (verif == 0)
                {
                    maxCounts = value;

                }
                else
                {
                    maxCounts = _old;
                }

                //OnPropertyChanged("MaxCounts");
            }
        }



        //private float vEsc;
        public string VEsc
        {
            get => vEsc;
            set
            {
                vEsc = value;
                OnPropertyChanged("VEsc");
            }
        }

        public string VInput
        {
            get => vInput;
            set
            {
                vInput = value;
                OnPropertyChanged("VInput");
            }
        }


        //private int ctsEsc;
        public int CtsEsc
        {
            get => ctsEsc;
            set
            {
                ctsEsc = value;
                OnPropertyChanged("CtsEsc");
            }
        }

        //private int adcCts;
        public long AdcCts
        {
            get => adcCts;
            set
            {
                adcCts = value;
                OnPropertyChanged("AdcCts");
            }
        }

        public string Geo_Local
        {
            get => geo_Local;
            set
            {
                if (Udp == false && comPort == null)
                {
                    return;
                }

                if (geo_Local == null)
                {
                    geo_Local = value;
                    OnPropertyChanged("Geo_Local");
                    return;
                }

                if (geo_Local == value)
                {
                    //geo_Local = value;
                    OnPropertyChanged("Geo_Local");
                    return;
                }

                string _old = geo_Local;

                var verif = WriteParam(0x41, value);

                if (verif == 0)
                {
                    geo_Local = value;

                }
                else
                {
                    geo_Local = _old;
                }

                OnPropertyChanged("Geo_Local");
            }
        }

        public string Geo_Adjust
        {
            get => geo_Adjust;
            set
            {
                if (Udp == false && comPort == null)
                {
                    return;
                }

                if (geo_Adjust == null)
                {
                    geo_Adjust = value;
                    OnPropertyChanged("Geo_Adjust");
                    return;
                }


                if (geo_Adjust == value)
                {
                    //geo_Adjust = value;
                    OnPropertyChanged("Geo_Adjust");
                    return;
                }

                string _old = geo_Adjust;

                var verif = WriteParam(0x42, value);

                if (verif == 0)
                {
                    geo_Adjust = value;

                }
                else
                {
                    geo_Adjust = _old;
                }

                OnPropertyChanged("Geo_Adjust");
            }
        }


        public string W_Display 
        { 
            get => w_Display;
            set
            {
                w_Display = value;

                OnPropertyChanged("W_Display");
            }
        }

        public bool IsNotConnected 
        { 
            get => isNotConnected;
            set
            {
                isNotConnected = value;
                OnPropertyChanged("IsNotConnected");
            }
        }

        public int Id 
        { 
            get => id;
            set
            {
                if (Udp == false && comPort == null)
                {
                    return;
                }

                if (id == value)
                {
                    //id = value;
                    OnPropertyChanged("Id");
                    return;
                }

                int old_id = id;

                var verif = WriteParam(1, string.Format("{0:X2}", value));

                if (verif == 0)
                {
                    id = value;

                }
                else
                {
                    id = old_id;
                }

                OnPropertyChanged("Id");

                

            }
        }

        public string FirmwareVersion
        { 
            get => firmwareVersion;
            set
            {
                firmwareVersion = value;
                OnPropertyChanged("FirmwareVersion");
            }    
        }

        public string SealSwitchState 
        { 
            get => sealSwitchState;
            set
            {
                sealSwitchState = value;
                OnPropertyChanged("SealSwitchState");
            }
        }

        public SerialPort ComPort 
        { 
            get => comPort;
            set
            {
                comPort = value;
                OnPropertyChanged("ComPort");
            }
        }

        public string OutputRate 
        { 
            get => outputRate; 
            set 
            {
                if (Udp == false && comPort == null)
                {
                    return;
                }

                if (outputRate == null)
                {
                    outputRate = value;
                    OnPropertyChanged("OutputRate");
                    return;
                }


                if (outputRate == value)
                {
                   // outputRate = value;
                    OnPropertyChanged("OutputRate");
                    return;
                }

                string old_value = outputRate;
                                
                var verif = WriteParam(0x13,value);

                if (verif == 0)
                {
                    outputRate = value;

                }
                else
                {
                    outputRate = old_value;
                }

                OnPropertyChanged("OutputRate");

            }
                
        }

        //public string RangeMode 
        //{ 
        //    get => rangeMode; 
        //    set 
        //    { 
        //        rangeMode = value;
        //        OnPropertyChanged("RangeMode");
        //    }
        //}

        public int RangeModeCode 
        { 
            get => rangeModeCode;
            set
            {
                if (Udp == false && comPort == null)
                {
                    return;
                }

                if (rangeModeCode == value)
                {
                    //rangeModeCode = value;
                    OnPropertyChanged("RangeModeCode");
                    return;
                }

                int _old = rangeModeCode;

                var verif = WriteParam(0x21, string.Format("{0}",value));

                if (verif == 0)
                {
                    rangeModeCode = value;

                }
                else
                {
                    rangeModeCode = _old;
                }

                OnPropertyChanged("RangeModeCode");

                

            }
        }


        public string NegativeWeight
        {
            get => negativeWeight;
            set
            {
                if (Udp == false && comPort == null)
                {
                    return;
                }

                if (negativeWeight == null)
                {
                    negativeWeight = value;
                    OnPropertyChanged("NegativeWeight");
                    return;
                }
                
                if (negativeWeight == value)
                {
                    //negativeWeight = value;
                    OnPropertyChanged("NegativeWeight");
                    return;
                }
                
                string _old = negativeWeight;

                //var verif = WriteParam(0x29, string.Format("{0}", value));
                var verif = WriteParam(0x29, value);

                if (verif == 0)
                {
                    negativeWeight = value;

                }
                else
                {
                    negativeWeight = _old;
                }

                OnPropertyChanged("NegativeWeight");
            }
        }


        public string W_Unit 
        { 
            get => w_Unit; 
            set 
            {
                w_Unit = value;
                double escvalue = curEsc / Math.Pow(10, decimalPlaces);
                //DefinitionString = string.Format("Max {0}{3} Min {1}{3}  e={2}{3}", (double)curMax / Math.Pow(10, decimalPlaces), 20 * escvalue, escvalue, w_Unit);
                definitionString = string.Format("Max {0}{3} Min {1}{3}  e={2}{3}", curMax, 20 * escvalue, escvalue, w_Unit);

                //OnPropertyChanged("W_Unit");
            }
        }

        public int W_UnitCode           //1="g ", 2="kg", 3="lb", 4="oz"
        {
            get => w_UnitCode;
            set
            {
                if (Udp == false && comPort == null)
                {
                    return;
                }

                if (w_UnitCode == value)
                {
                    //w_UnitCode = value;
                    OnPropertyChanged("W_UnitCode");
                    return;
                }

                int _old = w_UnitCode;

                var verif = WriteParam(0x20, string.Format("{0}",value));

                if (verif == 0)
                {

                    if (w_UnitCode == 0) w_UnitCode = 1;
                    w_UnitCode = value;
                    w_Unit = Unit[value];

                    double escvalue = curEsc / Math.Pow(10, decimalPlaces);
                    definitionString = string.Format("Max {0}{3} Min {1}{3}  e={2}{3}", curMax, 20 * escvalue, escvalue, w_Unit);
                    OnPropertyChanged("DefinitionString");
                }
                else
                {
                    w_UnitCode = _old;
                }

                OnPropertyChanged("W_UnitCode");
            }
        }


        public int CurMax 
        { 
            get => curMax; 
            set 
            {

                if (Udp == false && comPort == null)
                {
                    return;
                }

                if (curMax == value)
                {
                    //curMax = value;
                    OnPropertyChanged("CurMax");
                    return;
                }

                int _old = curMax;
                                
                string max = string.Format("{0}",value * Math.Pow(10, decimalPlaces ));
                                
                var verif = WriteParam(0x22,max);

                if (verif == 0)
                {
                    curMax = value;
                                                              
                    double escvalue = curEsc / Math.Pow(10, decimalPlaces);
                    definitionString = string.Format("Max {0}{3} Min {1}{3}  e={2}{3}", curMax , 20 * escvalue, escvalue, w_Unit);
                    OnPropertyChanged("DefinitionString");
                }
                else
                {
                    curMax = _old;
                }

                OnPropertyChanged("CurMax");
            }
        }
        
        public int CurEsc 
        { 
            get => curEsc; 
            set 
            {

                if (Udp == false && comPort == null)
                {
                    return;
                }

                if (curEsc == value)
                {
                    //curEsc = value;
                    OnPropertyChanged("CurEsc");
                    return;
                }

                int _old = curEsc;

                string esc = value.ToString();
                                
                var verif = WriteParam(0x23,esc);

                if (verif == 0)
                {
                    curEsc = value;
                    double escvalue = curEsc / Math.Pow(10, decimalPlaces);
                    definitionString = string.Format("Max {0}{3} Min {1}{3}  e={2}{3}", curMax, 20 * escvalue, escvalue, w_Unit);
                    OnPropertyChanged("DefinitionString");
                    ChangeScaleDef();

                }
                else
                {
                    curEsc = _old;
                }

                OnPropertyChanged("CurEsc");
            }
        }




        public int DecimalPlaces
        {
            get => decimalPlaces;
            set
            {

                if (Udp == false && comPort == null)
                {
                    return;
                }

                if (decimalPlaces == value)
                {
                    decimalPlaces = value;
                    OnPropertyChanged("DecimalPlaces");
                    return;
                }

                int _old = decimalPlaces;

                var verif = WriteParam(0x26, string.Format("{0}", value));

                if (verif == 0)
                {
                    decimalPlaces = value;

                    string max = string.Format("{0}", curMax * Math.Pow(10, decimalPlaces));
                    _ = WriteParam(0x22, max);

                    slopeFactor *= Math.Pow(10, _old - value);
                    string slope = string.Format("{0:0}", slopeFactor * 10000);
                    _ = WriteParam(0x31, slope);
                                        
                    OnPropertyChanged("SlopeFactor");

                    ChangeScaleDef();

                    double escvalue = curEsc / Math.Pow(10, decimalPlaces);
                    definitionString = string.Format("Max {0}{3} Min {1}{3}  e={2}{3}", curMax, 20 * escvalue, escvalue, w_Unit);
                    OnPropertyChanged("DefinitionString");
                }
                else
                {
                    decimalPlaces = _old;
                }

                OnPropertyChanged("DecimalPlaces");
            }
        }


        //private long slopeFactor;
        public double SlopeFactor
        {
            get => slopeFactor;
            set
            {
                if (Udp == false && comPort == null)
                {
                    return;
                }

                if (slopeFactor == value)
                {
                    //slopeFactor = value;
                    OnPropertyChanged("SlopeFactor");
                    return;
                }

                double _old = slopeFactor;

                string val = string.Format("{0:0}", value * 10000);

                var verif = WriteParam(0x31, val);

                if (verif == 0)
                {
                    slopeFactor = value;
                    ChangeScaleDef();
                    
                }
                else
                {
                    slopeFactor = _old;
                }

                OnPropertyChanged("SlopeFactor");
            }
        }


        public void ChangeScaleDef()
        {
            //string _get = Get_Param(0x0033, 2);
            //if (_get != "-")
            //{
            //    vEsc = (float)(Convert.ToDouble(_get, NumberFormatInfo.InvariantInfo));
            //}

            vEsc = string.Format(new NumberFormatInfo() { NumberDecimalDigits = 2, NumberDecimalSeparator = "." }, "{0:F} uV/e",(slopeFactor * (double) curEsc) / Convert.ToDouble(maxCounts) * 10000);


            //_get = Get_Param(0x0036, 2);
            //if (_get != "-")
            //{
            //    ctsEsc = (int)(Convert.ToDecimal(_get));
            //}

            ctsEsc = (int)(slopeFactor * curEsc);

            OnPropertyChanged("VEsc");
            OnPropertyChanged("CtsEsc");

        }

        public int BaudRateCode         //0=9600, 1=19200, 2=38400, 3=57600, 4=115200
        { 
            get => baudRateCode;
            set
            {
                if (Udp == false && comPort == null)
                {
                    return;
                }
                                
                int old_baudratecode = baudRateCode;
                int[] Baud = { 9600, 19200, 38400, 57600, 115200 };
                int br = value;

                string command = string.Format("\u000200{0:X2}W{1:X4}{2:X2}{3}00\u0003\r\n", id, 0x10, 1, br);
                SendCommand(command);

                if (comPort == null)
                {
                    return;

                }

                comPort.Close();
                comPort.BaudRate = Baud[br];
                comPort.Open();
                
                var verif = Get_Param(0x10,2);

                if (verif == string.Format("{0:D2}", value)) 
                {
                    baudRateCode = br;

                }
                else
                {
                    baudRateCode = old_baudratecode;
                    comPort.Close();
                    comPort.BaudRate = Baud[old_baudratecode];
                    comPort.Open();
                }

                OnPropertyChanged("BaudRate");

            }
        }

        //Zero user settings
        private string zeroTracking;
        private string zeroTrackingRange;
        private string zeroInit;
        private string zeroInitRange;

        //Tare user settings
        private string tareAuto;
        private string tareOnStability;
        private string tareMode;

        //Stability user settings
        private string filterLevel;
        private string filterAnimal;
        private string stabilityRange;

        public string ZeroTracking 
        { 
            get => zeroTracking;
            set
            {
                if (Udp == false && comPort == null)
                {
                    return;
                }

                if (zeroTracking == null)
                {
                    zeroTracking = value;
                    OnPropertyChanged("ZeroTracking");
                    return;
                }

                if (zeroTracking == value)
                {
                    //zeroTracking = value;
                    OnPropertyChanged("ZeroTracking");
                    return;
                }

                string old_value = zeroTracking;


                var verif = WriteParam(0x50, value);

                if (verif == 0)
                {
                    zeroTracking = value;
                }
                else
                {
                    zeroTracking = old_value;
                }

                OnPropertyChanged("ZeroTracking");

            }

        }

        public string ZeroTrackingRange 
        { 
            get => zeroTrackingRange;
            set
            {
                if (Udp == false && comPort == null)
                {
                    return;
                }

                if (zeroTrackingRange == null)
                {
                    zeroTrackingRange = value;
                    OnPropertyChanged("ZeroTrackingRange");
                    return;
                }

                if (ZeroTrackingRange == value)
                {
                    //zeroTrackingRange = value;
                    OnPropertyChanged("ZeroTrackingRange");
                    return;
                }

                string old_value = zeroTrackingRange;

                var verif = WriteParam(0x51, value);

                if (verif == 0)
                {
                    zeroTrackingRange = value;
                }
                else
                {
                    zeroTrackingRange = old_value;
                }

                OnPropertyChanged("ZeroTrackingRange");

            }
        }

        public string ZeroInit 
        { 
            get => zeroInit;
            set
            {
                if (Udp == false && comPort == null)
                {
                    return;
                }

                if (zeroInit == null)
                {
                    zeroInit = value;
                    OnPropertyChanged("ZeroInit");
                    return;
                }

                if (zeroInit == value)
                {
                    //zeroInit = value;
                    OnPropertyChanged("ZeroInit");
                    return;
                }

                string old_value = zeroInit;

                var verif = WriteParam(0x52, value);

                if (verif == 0)
                {
                    zeroInit = value;
                }
                else
                {
                    zeroInit = old_value;
                }

                OnPropertyChanged("ZeroInit");

            }
        }



        public string ZeroInitRange 
        { 
            get => zeroInitRange;
            set
            {
                if (Udp == false && comPort == null)
                {
                    return;
                }

                if (zeroInitRange == null)
                {
                    zeroInitRange = value;
                    OnPropertyChanged("ZeroInitRange");
                    return;
                }

                if (zeroInitRange == value)
                {
                    //zeroInitRange = value;
                    OnPropertyChanged("ZeroInitRange");
                    return;
                }


                string old_value = zeroInitRange;

                var verif = WriteParam(0x53, value);

                if (verif == 0)
                {
                    zeroInitRange = value;
                }
                else
                {
                    zeroInitRange = old_value;
                }

                OnPropertyChanged("ZeroInitRange");
            }
        }



        public string TareAuto 
        { 
            get => tareAuto;
            set
            {
                if (Udp == false && comPort == null)
                {
                    return;
                }

                if (tareAuto == null)
                {
                    tareAuto = value;
                    OnPropertyChanged("TareAuto");
                    return;
                }

                if (tareAuto == value)
                {
                    //tareAuto = value;
                    OnPropertyChanged("TareAuto");
                    return;
                }

                string old_value = tareAuto;

                var verif = WriteParam(0x61, value);

                if (verif == 0)
                {
                    tareAuto = value;
                }
                else
                {
                    tareAuto = old_value;
                }

                OnPropertyChanged("TareAuto");
            }
        }

        public string TareOnStability 
        { 
            get => tareOnStability;
            set
            {
                if (Udp == false && comPort == null)
                {
                    return;
                }

                if (tareOnStability == null)
                {
                    tareOnStability = value;
                    OnPropertyChanged("TareOnStability");
                    return;
                }

                if (tareOnStability == value)
                {
                    //tareOnStability = value;
                    OnPropertyChanged("TareOnStability");
                    return;
                }

                string old_value = tareOnStability;

                var verif = WriteParam(0x62, value);

                if (verif == 0)
                {
                    tareOnStability = value;
                }
                else
                {
                    tareOnStability = old_value;
                }

                OnPropertyChanged("TareOnStability");
            }
        }

        public string TareMode 
        { 
            get => tareMode;
            set
            {
                if (Udp == false && comPort == null)
                {
                    return;
                }

                if (tareMode == null)
                {
                    tareMode = value;
                    OnPropertyChanged("TareMode");
                    return;
                }

                if (tareMode == value)
                {
                    //tareMode = value;
                    OnPropertyChanged("TareMode");
                    return;
                }

                string old_value = tareMode;

                var verif = WriteParam(0x60, value);

                if (verif == 0)
                {
                    tareMode = value;
                }
                else
                {
                    tareMode = old_value;
                }

                OnPropertyChanged("TareMode");
            }
        }

        public string FilterLevel 
        { 
            get => filterLevel;
            set
            {
                if (Udp == false && comPort == null)
                {
                    return;
                }

                if (filterLevel == null)
                {
                    filterLevel = value;
                    OnPropertyChanged("FilterLevel");
                    return;
                }

                if (filterLevel == value)
                {
                    //filterLevel = value;
                    OnPropertyChanged("FilterLevel");
                    return;
                }

                string old_value = filterLevel;

                var verif = WriteParam(0x70, value);

                if (verif == 0)
                {
                    filterLevel = value;


                }
                else
                {
                    filterLevel = old_value;
                }

                OnPropertyChanged("FilterLevel");

            }
        }

        public string FilterAnimal 
        { 
            get => filterAnimal;
            set
            {
                if (Udp == false && comPort == null)
                {
                    return;
                }

                if (filterAnimal == null)
                {
                    filterAnimal = value;
                    OnPropertyChanged("FilterAnimal");
                    return;
                }

                if (filterAnimal == value)
                {
                    //filterAnimal = value;
                    OnPropertyChanged("FilterAnimal");
                    return;
                }

                string old_value = filterAnimal;

                var verif = WriteParam(0x72, value);

                if (verif == 0)
                {
                    filterAnimal = value;
                }
                else
                {
                    filterAnimal = old_value;
                }

                OnPropertyChanged("FilterAnimal");
                
            }
        }

        public string StabilityRange 
        { 
            get => stabilityRange;
            set
            {
                if (Udp == false && comPort == null)
                {
                    return;
                }

                if (stabilityRange == null)
                {
                    stabilityRange = value;
                    OnPropertyChanged("StabilityRange");
                    return;
                }

                if (stabilityRange == value)
                {
                    //stabilityRange = value;
                    OnPropertyChanged("StabilityRange");
                    return;
                }

                string old_value = stabilityRange;

                var verif = WriteParam(0x73, value);

                if (verif == 0)
                {
                    stabilityRange = value;
                }
                else
                {
                    stabilityRange = old_value;
                }

                OnPropertyChanged("StabilityRange");
                                
            }
        }


        //network
        private string wiFiBoardCode;
        private string wiFiBoardType;

        private string name;
        private string aP_Password;
        private string aP_IP_Address;
        private string aP_DHCP;
        private string wiFi_AP;

        private string sTA_SSID;
        private string sTA_Password;
        private string sTA_IP_Address;
        private string sTA_DHCP;

        private string tCP_Server_Port;

        private string uDP_AP_Remote_Port;
        private string uDP_AP_Local_Port;

        private string uDP_STA_Remote_Port;
        private string uDP_STA_Local_Port;


        public string Name
        {
            get => name;
            set
            {

                if (Udp == false && comPort == null)
                {
                    return;
                }

                if (name == null)
                {
                    name = value;
                    OnPropertyChanged("Name");
                    return;
                }

                if (name == value)
                {
                    //name = value;
                    OnPropertyChanged("Name");
                    return;
                }

                //OnStartNameChange();

                string old_value = name;

                string v = value;
                if (v.Length > 31)
                {
                    v = value.Substring(0, 31);
                }


                var verif = WriteParam(0x500, v);

                if (verif == 0)
                {
                    name = v;
                }
                else
                {
                    name = old_value;
                }

                OnPropertyChanged("Name");
                OnNameChanged();
            }
        }

        public string AP_Password 
        { 
            get => aP_Password;
            set
            {

                if (Udp == false && comPort == null)
                {
                    return;
                }

                if (aP_Password == null)
                {
                    aP_Password = value;
                    OnPropertyChanged("AP_Password");
                    return;
                }

                if (aP_Password == value)
                {
                    //aP_Password = value;
                    OnPropertyChanged("AP_Password");
                    return;
                }

                //OnStartNameChange();

                string old_value = aP_Password;

                string v = value;
                if (v.Length > 31)
                {
                    v = value.Substring(0, 31);
                }


                var verif = WriteParam(0x501, v);

                if (verif == 0)
                {
                    aP_Password = v;
                }
                else
                {
                    aP_Password = old_value;
                }

                OnPropertyChanged("AP_Password");

                //OnNameChanged();
            }
        }


        public string AP_IP_Address 
        { 
            get => aP_IP_Address;
            set
            {
                
                if (Udp == false && comPort == null)
                {
                    return;
                }

                if (aP_IP_Address == null)
                {
                    aP_IP_Address = value;
                    OnPropertyChanged("AP_IP_Address");
                    return;
                }

                if (aP_IP_Address == value)
                {
                    //aP_IP_Address = value;
                    OnPropertyChanged("AP_IP_Address");
                    return;
                }

                //OnStartNameChange();

                string old_value = aP_IP_Address;

                string v = value;
                if (v.Length > 15)
                {
                    v = value.Substring(0, 15);
                }


                var verif = WriteParam(0x502, v);

                if (verif == 0)
                {
                    aP_IP_Address = v;


                }
                else
                {
                    aP_IP_Address = old_value;
                }


                OnPropertyChanged("AP_IP_Address");

                //OnNameChanged();
                
            }
        
        
        }
                
        
        public string AP_DHCP 
        { 
            get => aP_DHCP;
            set
            {
                if (Udp == false && comPort == null)
                {
                    return;
                }

                if (aP_DHCP == null)
                {
                    aP_DHCP = value;
                    OnPropertyChanged("AP_DHCP");
                    return;
                }

                if (aP_DHCP == value)
                {
                    //aP_DHCP = value;
                    OnPropertyChanged("AP_DHCP");
                    return;
                }

                string old_value = aP_DHCP;

                var verif = WriteParam(0x503, value);

                if (verif == 0)
                {
                    aP_DHCP = value;
                }
                else
                {
                    aP_DHCP = old_value;
                }


                OnPropertyChanged("AP_DHCP");
                                
            }

        }

        public string WiFi_AP 
        { 
            get => wiFi_AP;
            set
            {
                if (Udp == false && comPort == null)
                {
                    return;
                }

                if (wiFi_AP == null)
                {
                    wiFi_AP = value;
                    OnPropertyChanged("WiFi_AP");
                    return;
                }

                if (wiFi_AP == value)
                {
                    //wiFi_AP = value;
                    OnPropertyChanged("WiFi_AP");
                    return;
                }

                string old_value = wiFi_AP;
                                
                var verif = WriteParam(0x504, value);

                if (verif == 0)
                {
                    wiFi_AP = value;
                }
                else
                {
                    wiFi_AP = old_value;
                }

                OnPropertyChanged("WiFi_AP");
                                
            }

        }

    
        public string STA_SSID 
        { 
            get => sTA_SSID;
            set
            {
                if (Udp == false && comPort == null)
                {
                    return;
                }

                if (sTA_SSID == null)
                {
                    sTA_SSID = value;
                    OnPropertyChanged("STA_SSID");
                    return;
                }

                if (sTA_SSID == value)
                {
                    //sTA_SSID = value;
                    OnPropertyChanged("STA_SSID");
                    return;
                }


                string old_value = sTA_SSID;

                string v = value;
                if (v.Length > 31)
                {
                    v = value.Substring(0, 31);
                }


                var verif = WriteParam(0x600, v);

                if (verif == 0)
                {
                    sTA_SSID = v;


                }
                else
                {
                    sTA_SSID = old_value;
                }


                OnPropertyChanged("STA_SSID");
                
            }
        }


        public string STA_Password 
        { 
            get => sTA_Password;
            set
            {
                if (Udp == false && comPort == null)
                {
                    return;
                }

                if (sTA_Password == null)
                {
                    sTA_Password = value;
                    OnPropertyChanged("STA_Password");
                    return;
                }

                if (sTA_Password == value)
                {
                    //sTA_Password = value;
                    OnPropertyChanged("STA_Password");
                    return;
                }

                string old_value = sTA_Password;

                string v = value;
                if (v.Length > 31)
                {
                    v = value.Substring(0, 31);
                }


                var verif = WriteParam(0x601, v);

                if (verif == 0)
                {
                    sTA_Password = v;
                }
                else
                {
                    sTA_Password = old_value;
                }

                OnPropertyChanged("STA_Password");
                
            }
        }

        public string STA_IP_Address 
        { 
            get => sTA_IP_Address;
            set
            {
                if (Udp == false && comPort == null)
                {
                    return;
                }

                if (sTA_IP_Address == null)
                {
                    sTA_IP_Address = value;
                    OnPropertyChanged("STA_IP_Address");
                    return;
                }

                if (sTA_IP_Address == value)
                {
                    //sTA_IP_Address = value;
                    OnPropertyChanged("STA_IP_Address");
                    return;
                }

                string old_value = sTA_IP_Address;

                string v = value;
                if (v.Length > 15)
                {
                    v = value.Substring(0, 15);
                }
                                
                var verif = WriteParam(0x602, v);              

                if (verif == 0)
                {

                    sTA_IP_Address = v;
                    
                }
                else
                {
                    sTA_IP_Address = old_value;
                }

                OnPropertyChanged("STA_IP_Address");

            }
        }

        public string STA_DHCP 
        { 
            get => sTA_DHCP;
            set
            {
                if (Udp == false && comPort == null)
                {
                    return;
                }

                if (sTA_DHCP == null)
                {
                    sTA_DHCP = value;
                    OnPropertyChanged("STA_DHCP");
                    return;
                }

                if (sTA_DHCP == value)
                {
                    //sTA_DHCP = value;
                    OnPropertyChanged("STA_DHCP");
                    return;
                }

                string old_value = sTA_DHCP;
                               
                var verif = WriteParam(0x603, value);

                if (verif == 0)
                {
                    sTA_DHCP = value;
                }
                else
                {
                    sTA_DHCP = old_value;
                }

                OnPropertyChanged("STA_DHCP");

            }
        }

        public string TCP_Server_Port 
        { 
            get => tCP_Server_Port;
            set
            {
                //OnStartNameChange();

                if (Udp == false && comPort == null)
                {
                    return;
                }

                if (tCP_Server_Port == null)
                {
                    tCP_Server_Port = value;
                    OnPropertyChanged("TCP_Server_Port");
                    return;
                }

                if (tCP_Server_Port == value)
                {
                    //tCP_Server_Port = value;
                    OnPropertyChanged("TCP_Server_Port");
                    return;
                }

                string old_value = tCP_Server_Port;

                string v;
                if (uint.TryParse(value, out uint result))
                {
                    v = string.Format("{0}", result);
                }
                else
                {
                    v = old_value;
                }


                var verif = WriteParam(0x702, v);

                if (verif == 0)
                {
                    tCP_Server_Port = v;
                }
                else
                {
                    tCP_Server_Port = old_value;
                }

                OnPropertyChanged("TCP_Server_Port");

                //OnNameChanged();

            }
        }
        
        public string UDP_AP_Remote_Port 
        { 
            get => uDP_AP_Remote_Port;
            set
            {
                //OnStartNameChange();

                if (Udp == false && comPort == null)
                {
                    return;
                }

                if (uDP_AP_Remote_Port == null)
                {
                    uDP_AP_Remote_Port = value;
                    OnPropertyChanged("UDP_AP_Remote_Port");
                    return;
                }

                if (uDP_AP_Remote_Port == value)
                {
                    //uDP_AP_Remote_Port = value;
                    OnPropertyChanged("UDP_AP_Remote_Port");
                    return;
                }

                string old_value = uDP_AP_Remote_Port;

                string v;                
                if (uint.TryParse(value, out uint result))
                {
                    v = string.Format("{0}", result);
                }
                else
                {
                    v = old_value;
                }

                var verif = WriteParam(0x700, v);

                if (verif == 0)
                {
                    uDP_AP_Remote_Port = v;
                    OnPropertyChanged("UDP_AP_Remote_Port");

                    uint p = result + 1;
                    uDP_STA_Remote_Port = string.Format("{0}", p);
                    OnPropertyChanged("UDP_STA_Remote_Port");

                }
                else
                {
                    uDP_AP_Remote_Port = old_value;
                }

                
                //OnNameChanged();
            }
        }
        
        public string UDP_AP_Local_Port 
        { 
            get => uDP_AP_Local_Port;
            set
            {
                //OnStartNameChange();

                if (Udp == false && comPort == null)
                {
                    return;
                }

                if (uDP_AP_Local_Port == null)
                {
                    uDP_AP_Local_Port = value;
                    OnPropertyChanged("UDP_AP_Local_Port");
                    return;
                }

                if (uDP_AP_Local_Port == value)
                {
                    //uDP_AP_Local_Port = value;
                    OnPropertyChanged("UDP_AP_Local_Port");
                    return;
                }

                string old_value = uDP_AP_Local_Port;

                string v;
                if (uint.TryParse(value, out uint result))
                {
                    v = string.Format("{0}", result);
                }
                else
                {
                    v = old_value;
                }


                var verif = WriteParam(0x701, v);

                if (verif == 0)
                {
                    uDP_AP_Local_Port = v;
                    OnPropertyChanged("UDP_AP_Local_Port");

                    uint p = result + 1;
                    uDP_STA_Local_Port = string.Format("{0}", p);
                    OnPropertyChanged("UDP_STA_Local_Port");

                }
                else
                {
                    uDP_AP_Local_Port = old_value;
                }
                               
                //OnNameChanged();
            }

        }
        
        public string UDP_STA_Remote_Port 
        { 
            get => uDP_STA_Remote_Port;
            set
            {
                if (Udp == false && comPort == null)
                {
                    return;
                }

                if (uDP_STA_Remote_Port == null)
                {
                    uDP_STA_Remote_Port = value;
                    OnPropertyChanged("UDP_STA_Remote_Port");
                    return;
                }

                if (uDP_STA_Remote_Port == value)
                {
                    //uDP_STA_Remote_Port = value;
                    OnPropertyChanged("UDP_STA_Remote_Port");
                    return;
                }

                string old_value = uDP_STA_Remote_Port;

                string v;
                if (uint.TryParse(value, out uint result))
                {
                    v = string.Format("{0}", result);
                }
                else
                {
                    v = old_value;
                }


                uDP_STA_Remote_Port = v;
                OnPropertyChanged("UDP_STA_Remote_Port");

                uDP_AP_Remote_Port = string.Format("{0}", result - 1);
                OnPropertyChanged("UDP_AP_Remote_Port");


            }
        }
        
        public string UDP_STA_Local_Port 
        { 
            get => uDP_STA_Local_Port;
            set
            {
               
                if (Udp == false && comPort == null)
                {
                    return;
                }

                if (uDP_STA_Local_Port == null)
                {
                    uDP_STA_Local_Port = value;
                    OnPropertyChanged("UDP_STA_Local_Port");
                    return;
                }

                if (uDP_STA_Local_Port == value)
                {
                    //uDP_STA_Local_Port = value;
                    OnPropertyChanged("UDP_STA_Local_Port");
                    return;
                }

                //OnStartNameChange();

                string old_value = uDP_STA_Local_Port;

                string v;
                if (uint.TryParse(value, out uint result))
                {
                    v = string.Format("{0}", result);
                }
                else
                {
                    v = old_value;
                }


                uDP_STA_Local_Port = v;
                OnPropertyChanged("UDP_STA_Local_Port");

                uDP_AP_Local_Port = string.Format("{0}", result - 1);
                OnPropertyChanged("UDP_AP_Local_Port");


            }

        }


        public string WiFiBoardCode 
        { 
            get => wiFiBoardCode;
            set
            {
                wiFiBoardCode = value;
                //OnPropertyChanged("WiFiBoardCode");
            }
        }

        public string WiFiBoardType 
        { 
            get => wiFiBoardType;
            set
            {
                wiFiBoardType = value;
                //OnPropertyChanged("WiFiBoardType");
            }
        }

        
        //power alarm settings
        private string vinMin;
        private string vinMax;
        private string vinCts;
        private string vinVolt;
        private string xTemp;
            

        private string voutMin;
        private string voutMax;

        public string VinMin 
        { 
            get => vinMin;
            set
            {
                if (Udp == false && comPort == null)
                {
                    return;
                }

                if (vinMin == null)
                {
                    vinMin = value;
                    OnPropertyChanged("VinMin");
                    return;
                }

                if (vinMin == value)
                {
                    OnPropertyChanged("VinMin");
                    return;
                }

                string old_value = vinMin;

                var verif = WriteParam(0x2, value);

                if (verif == 0)
                {
                    vinMin = value;
                }
                else
                {
                    vinMin = old_value;
                }

                OnPropertyChanged("VinMin");

            }
        }
        
        public string VinMax 
        { 
            get => vinMax;
            set
            {
                if (Udp == false && comPort == null)
                {
                    return;
                }

                if (vinMax == null)
                {
                    vinMax = value;
                    OnPropertyChanged("VinMax");
                    return;
                }

                if (vinMax == value)
                {
                    OnPropertyChanged("VinMax");
                    return;
                }

                string old_value = vinMax;

                var verif = WriteParam(0x3, value);

                if (verif == 0)
                {
                    vinMax = value;
                }
                else
                {
                    vinMax = old_value;
                }

                OnPropertyChanged("VinMax");

            }
        }


        public string VinCts
        {
            get => vinCts;
            set
            {
                vinCts = value;
                OnPropertyChanged("VinCts");
            }
        }

        public string VinVolt
        {
            get => vinVolt;
            set
            {
                vinVolt = value;
                OnPropertyChanged("VinVolt");
            }
        }



        public string VoutMin
        {
            get => voutMin;
            set
            {
                if (Udp == false && comPort == null)
                {
                    return;
                }

                if (voutMin == null)
                {
                    voutMin = value;
                    OnPropertyChanged("VoutMin");
                    return;
                }

                if (voutMin == value)
                {
                    OnPropertyChanged("VoutMin");
                    return;
                }

                string old_value = voutMin;

                var verif = WriteParam(0x4, value);

                if (verif == 0)
                {
                    voutMin = value;
                }
                else
                {
                    voutMin = old_value;
                }

                OnPropertyChanged("VoutMin");

            }
        }
        
        public string VoutMax 
        { 
            get => voutMax;
            set
            {
                if (Udp == false && comPort == null)
                {
                    return;
                }

                if (voutMax == null)
                {
                    voutMax = value;
                    OnPropertyChanged("VoutMax");
                    return;
                }

                if (voutMax == value)
                {
                    OnPropertyChanged("VoutMax");
                    return;
                }

                string old_value = voutMax;

                var verif = WriteParam(0x5, value);

                if (verif == 0)
                {
                    voutMax = value;
                }
                else
                {
                    voutMax = old_value;
                }

                OnPropertyChanged("VoutMax");

            }
        }


        public string XTemp
        {
            get => xTemp;
            set
            {
                xTemp = value;
                OnPropertyChanged("XTemp");
            }
        }


        //Bull mode
        private string bullMode;
        public string BullMode 
        { 
            get => bullMode;
            set
            {
                if (Udp == false && comPort == null)
                {
                    return;
                }

                if (bullMode == null)
                {
                    bullMode = value;
                    OnPropertyChanged("BullMode");
                    return;
                }

                if (bullMode == value)
                {                    
                    OnPropertyChanged("BullMode");
                    return;
                }

                string old_value = bullMode;

                var verif = WriteParam(0x15, value);

                if (verif == 0)
                {
                    bullMode = value;
                }
                else
                {
                    bullMode = old_value;
                }

                OnPropertyChanged("BullMode");

            }
        }

        
        protected virtual void OnWeightChanged()
        {
            WeightChanged?.Invoke(this, EventArgs.Empty);
        }
                
        protected virtual void OnRecallscaledef()
        {
            Recallscaledef?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnNameChanged()
        {
            NameChanged?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnNewStableWeight()
        {
            NewStableWeight?.Invoke(this, EventArgs.Empty);
        }


        protected virtual void OnConnectProgress()
        {
            ConnectProgress?.Invoke(this, EventArgs.Empty);
        }


        public void Init_SerialPort()
        {
            //StopThread = true;

            try
            {
                comPort.Open();

                //Start reading ComPort
                comPort.DataReceived += new SerialDataReceivedEventHandler(ReadCOMWeightStream);
                comPort.ErrorReceived += ComPort_ErrorReceived;
            }
            catch
            {
                //Console.WriteLine("error en Init_SerialPort");
            }

           
        }

        private void AdcCtsTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (GetAdcCts)
            {
                if (Udp == true)
                {
                    byte[] Udp_Send;
                    //Send write command
                    Udp_Send = System.Text.ASCIIEncoding.UTF8.GetBytes("\u000200FFR01110000\u0003\r\n");
                    Listener.Send(Udp_Send, Udp_Send.Length, UdpSendEndpoint);

                }
                else
                {
                    SendCommand("\u000200FFR01110000\u0003\r\n");
                }
                
                
            }

            if (GetVinCts)
            {
                if (Udp == true)
                {
                    byte[] Udp_Send;
                    //Send write command
                    Udp_Send = System.Text.ASCIIEncoding.UTF8.GetBytes("\u000200FFR02100000\u0003\r\n");
                    Listener.Send(Udp_Send, Udp_Send.Length, UdpSendEndpoint);

                }
                else
                {
                    SendCommand("\u000200FFR02100000\u0003\r\n");
                }


            }

            if (GetXTemp)
            {

                if (Udp == true)
                {
                    byte[] Udp_Send;
                    //Send write command
                    Udp_Send = System.Text.ASCIIEncoding.UTF8.GetBytes("\u000200FFR02020000\u0003\r\n");
                    Listener.Send(Udp_Send, Udp_Send.Length, UdpSendEndpoint);

                }
                else
                {
                    SendCommand("\u000200FFR02020000\u0003\r\n");
                }


            }

            //throw new NotImplementedException();
        }

        private void ComPort_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            //Console.WriteLine(e.EventType.ToString());
            //throw new NotImplementedException();
        }

        public void GetScaleId()
        {
            
            IPEndPoint LocalIP = new IPEndPoint(address: IPAddress.Any, port: UdpRecPort);

            Listener = new UdpClient()
            {
                EnableBroadcast = false,
                ExclusiveAddressUse = false,

            };
            Listener.Client.Blocking = false;
            Listener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            Listener.Client.Bind(LocalIP);
            Listener.Client.Connect(udpSendEndpoint);
                        

            ReadUdpData = new Thread(Listen_UDP_Async);

            StopThread = false;
            ReadUdpData.IsBackground = true;
            ReadUdpData.Start();

            //Get scale id
            while (  serialNumber == null || serialNumber == "-")
            {
                serialNumber = Get_Param(0,5);
            }
            
 //           if (serialNumber=="-")
 //           {
 //               Console.WriteLine(serialNumber+"\r\n");
 //           }

            while (name == null || name == "-")
            {
                name = Get_Param(0x500,5);
            }
            
            
            StopThread = true;
            

        }

       
        private async void Listen_UDP_Async()
        {
            UdpReceiveResult rec;
            
            while (!StopThread)
            {
                try
                {
                    
                    rec = await Listener.ReceiveAsync();

                    rx_buffer = System.Text.Encoding.ASCII.GetString(rec.Buffer);
                    //Console.WriteLine(udpSendEndpoint.ToString() + " " + rx_buffer);

                    if (IsWaitingData == false)
                    {
                        ParseWeightStream(rx_buffer.Substring(1, rx_buffer.Length - 4));
                    }

                    //Thread.Sleep(5);

                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    //Console.WriteLine("Error en Listen_UDP_Async()");
                }

                
            }

            //Console.WriteLine("Stop thread ReadUdpData");

        }
                

        public void Init_Udp_Comms()
        {
            //UDP communication
            //byte[] _ip = udpSendEndpoint.Address.GetAddressBytes();

            IPEndPoint LocalIP = new IPEndPoint(address: IPAddress.Any , port: UdpRecPort);
            
            Listener = new UdpClient()
            {
                EnableBroadcast = false,
                ExclusiveAddressUse = false,
                              
            };
            Listener.Client.Blocking = false;
            Listener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            
            Listener.Client.Bind(LocalIP);
            Listener.Client.Connect(udpSendEndpoint);
                        
            ReadUdpData = new Thread(Listen_UDP_Async);
            StopThread = false;
            ReadUdpData.IsBackground = true;
            ReadUdpData.Start();

           
        }

        public void Init_Scale()
        {
            ConnectState = false;

            IsWaitingData = false;

            //QR encoder
            //Encoder.ErrorCorrectionLevel = ErrorCorrectionLevel.H;

            //Start sending
            SendCommand("\u000200FFE10110000\u0003\r\n");


            // Create a timer and set a two second interval.
            aTimer = new Timer
            {
                Interval = 1000
            };
                        
            // Have the timer fire repeated events (true is the default)
            aTimer.AutoReset = true;

            // Start the timer
            aTimer.Enabled = true;

            // Hook up the Elapsed event for the timer. 
            aTimer.Elapsed += OnTimedEvent;

            
            //Timer reading ADC cts
            AdcCtsTimer = new Timer
            {
                Interval = 100,
                AutoReset = true
            };

            AdcCtsTimer.Elapsed += AdcCtsTimer_Elapsed;
            AdcCtsTimer.Enabled = true;

            //Timer for stability elapsed time
            StabilityTimer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromMilliseconds(50),

            };
            StabilityElapsedTime = TimeSpan.Zero;
            StabilityTimer.Tick += StabilityTimer_Tick;

            //StabilityTime = 500;

        }

        private void StabilityTimer_Tick(object sender, EventArgs e)
        {

            StabilityElapsedTime += StabilityTimer.Interval;
            //throw new NotImplementedException();
        }

        public void GetScaleDef()
        {
            string _get;
            int unit = 0;
            int _rangemode;
            int datacount = 0;

            if (aTimer != null)
            {
                aTimer.Enabled = false;
            }
            
            scale_info = true;
            
            SendCommand("\u000200FFE10100000\u0003\r\n");

            OnConnectProgress();
            Thread.Sleep(1);

            _get = Get_Param(0x01, 5);
            if (_get != "-")
            {
                if (int.TryParse(_get, NumberStyles.AllowHexSpecifier, NumberFormatInfo.CurrentInfo, out int result))
                {
                    id = result;
                    datacount++;
                }
            }
            
            OnConnectProgress();
            Thread.Sleep(1);

            _get = Get_Param(0x07,5);
            if (_get != "-")
            {
                type = _get == "186" || _get == "122" ? "XTREM-S" : "XTREM";
                datacount++;
            }
            else
            {
                type = "";
            }
            OnConnectProgress();
            Thread.Sleep(1);

            _get = Get_Param(0,5);
            if (_get != "-")
            {
                serialNumber = _get;
                datacount++;
            }
            else
            {
                //Console.WriteLine("Error getting S.N.");
            }
            OnConnectProgress();
            Thread.Sleep(1);

            _get = Get_Param(0x28,5);
            if (_get != "-")
            {
                datacount++;
                if (_get == "10")
                {
                    w_Flag_HighRes = true;
                }
                else
                {
                    w_Flag_HighRes = false;
                }
            }
            OnConnectProgress();
            Thread.Sleep(1);


            _get = Get_Param(0x26,5);
            if (_get != "-")
            {
                datacount++;
                decimalPlaces = Convert.ToInt32(_get);
                if (w_Flag_HighRes) decimalPlaces--;

            }
            OnConnectProgress();
            Thread.Sleep(1);

            _get = Get_Param(0x28,5);
            if (_get != "-")
            {
                datacount++;
                ResFactor = Convert.ToInt32(_get);
            }
            OnConnectProgress();
            Thread.Sleep(1);

            int dec = decimalPlaces - (int)ResFactor / 10;

            _get = Get_Param(0x22,5);
            if (_get != "-")
            {
                datacount++;
                curMax = (int)( (Convert.ToDouble(_get) ) / Math.Pow(10,decimalPlaces));
            }
            OnConnectProgress();
            Thread.Sleep(1);

            _get = Get_Param(0x23,5);
            if (_get != "-")
            {
                datacount++;
                curEsc = Convert.ToInt32(_get);
            }
            OnConnectProgress();
            Thread.Sleep(1);

            _get = Get_Param(0x20,5);
            if (_get != "-")
            {
                datacount++;
                unit = Convert.ToInt32(_get);
            }
            OnConnectProgress();
            Thread.Sleep(1);

            w_UnitCode = unit;
            w_Unit = Unit[unit];

            _get = Get_Param(0x10,5);
            if (_get != "-")
            {
                datacount++;
                baudRateCode = Convert.ToInt32(_get);
            }
            OnConnectProgress();
            Thread.Sleep(1);

            _get = Get_Param(0x13,5);
            if (_get != "-")
            {
                outputRate = _get;
                datacount++;
            }
            OnConnectProgress();
            Thread.Sleep(1);

            _get = Get_Param(0x08,5);
            if (_get != "-")
            {
                firmwareVersion = _get;
                datacount++;
            }
            OnConnectProgress();
            Thread.Sleep(1);

            _get = Get_Param(0x09,5);
            if (_get != "-")
            {
                sealSwitchState = _get;
                datacount++;
            }
            OnConnectProgress();
            Thread.Sleep(1);

            _get = Get_Param(0x21,5);
            if (_get != "-")
            {
                datacount++;
                _rangemode = Convert.ToInt32(_get);
                rangeModeCode = _rangemode;
            }
            else
            {
                //_rangemode = 0;
                rangeModeCode = 0;
            }
            //rangeMode = R_Mode[_rangemode];
            OnConnectProgress();
            Thread.Sleep(1);

            double Esc = ((double)curEsc / Math.Pow(10, dec));
            double Min = (20 * Esc);
            //DefinitionString = string.Format("Max {0}{3} Min {1}{3}  e={2}{3}", (double) CurMax / Math.Pow(10, dec), Min, Esc, W_Unit);
            definitionString = string.Format("Max {0}{3} Min {1}{3}  e={2}{3}", curMax , Min, Esc, w_Unit);

            _get = Get_Param(0x500,5);
            if (_get != "-")
            {
                name = _get;
                datacount++;
            }
            OnConnectProgress();
            Thread.Sleep(1);

            //Zero user settings
            _get = Get_Param(0x50,5) ;
            if (_get != "-")
            {
                zeroTracking = _get;
                datacount++;
            }
            OnConnectProgress();
            Thread.Sleep(1);

            _get = Get_Param(0x51,5);
            if (_get != "-")
            {
                zeroTrackingRange = _get;
                datacount++;
            }
            OnConnectProgress();
            Thread.Sleep(1);

            _get = Get_Param(0x52,5);
            if (_get != "-")
            {
                zeroInit = _get;
                datacount++;
            }
            OnConnectProgress();
            Thread.Sleep(1);

            _get = Get_Param(0x53,5);
            if (_get != "-")
            {
                zeroInitRange = _get;
                datacount++;
            }
            OnConnectProgress();
            Thread.Sleep(1);

            //Tare user settings
            _get = Get_Param(0x61,5);
            if (_get != "-")
            {
                tareAuto = _get;
                datacount++;
            }
            OnConnectProgress();
            Thread.Sleep(1);

            _get = Get_Param(0x62,5);
            if (_get != "-")
            {
                tareOnStability = _get;
                datacount++;
            }
            OnConnectProgress();
            Thread.Sleep(1);

            _get = Get_Param(0x60,5);
            if (_get != "-")
            {
                tareMode = _get;
                datacount++;
            }
            OnConnectProgress();
            Thread.Sleep(1);

            _get = Get_Param(0x29,5);
            if (_get != "-")
            {
                negativeWeight = _get;
                datacount++;
            }

            //Stability user settings
            _get = Get_Param(0x70,5);
            if (_get != "-")
            {
                filterLevel = _get;
                datacount++;
            }
            OnConnectProgress();
            Thread.Sleep(1);

            _get = Get_Param(0x72,5);
            if (_get != "-")
            {
                filterAnimal = _get;
                datacount++;
            }
            OnConnectProgress();
            Thread.Sleep(1);

            _get = Get_Param(0x73,5);
            if (_get != "-")
            {
                stabilityRange = _get;
                datacount++;
            }
            OnConnectProgress();
            Thread.Sleep(1);

            //Network settings
            _get = Get_Param(0x0A,5);
            if (_get != "-")
            {
                wiFiBoardCode = _get;
                datacount++;
            }
            if (wiFiBoardCode == "01")
            {
                wiFiBoardType = "ESP 8266";
            } 
            else if (wiFiBoardCode == "02")
            {
                wiFiBoardType = "ESP 32";
            }
            else
            {
                wiFiBoardType = "Not present";
            }
            OnConnectProgress();
            Thread.Sleep(1);

            _get = Get_Param(0x501,5);
            if (_get != "-")
            {
                aP_Password = _get;
                datacount++;
            }
            OnConnectProgress();
            Thread.Sleep(1);

            //Thread.Sleep(50);

            _get = Get_Param(0x502,5);
            if (_get != "-")
            {
                aP_IP_Address = _get;
                datacount++;
            }
            OnConnectProgress();
            Thread.Sleep(1);

            _get = Get_Param(0x503,5);
            if (_get != "-")
            {
                aP_DHCP = _get;
                datacount++;
            }
            OnConnectProgress();
            Thread.Sleep(1);

            _get = Get_Param(0x504,5);
            if (_get != "-")
            {
                wiFi_AP = _get;
                datacount++;
            }
            else
            {
                wiFi_AP = "1";

            }
            OnConnectProgress();
            Thread.Sleep(1);

            _get = Get_Param(0x600,5);
            if (_get != "-")
            {
                sTA_SSID = _get;
                datacount++;
            }
            OnConnectProgress();
            Thread.Sleep(1);

            _get = Get_Param(0x601,5);
            if (_get != "-")
            {
                sTA_Password = _get;
                datacount++;
            }
            OnConnectProgress();
            Thread.Sleep(1);

            _get = Get_Param(0x602,5);
            if (_get != "-")
            {
                sTA_IP_Address = _get;
                datacount++;
            }
            OnConnectProgress();
            Thread.Sleep(1);

            _get = Get_Param(0x603,5);
            if (_get != "-")
            {
                sTA_DHCP = _get;
                datacount++;
            }
            OnConnectProgress();
            Thread.Sleep(1);

            _get = Get_Param(0x702,5);
            if (_get != "-")
            {
                tCP_Server_Port = _get;
                datacount++;
            }
            OnConnectProgress();
            Thread.Sleep(1);

            _get = Get_Param(0x700,5);
            if (_get != "-")
            {
                uDP_AP_Remote_Port = _get;
                datacount++;
            }
            OnConnectProgress();
            Thread.Sleep(1);

            _get = Get_Param(0x701,5);
            if (_get != "-")
            {
                uDP_AP_Local_Port = _get;
                datacount++;
            }
            OnConnectProgress();
            Thread.Sleep(1);

            //adjust information
            //        private long initZero;
            _get = Get_Param(0x0030,5);
            if (_get != "-")
            {
                datacount++;
                initZero = (long)(Convert.ToInt64(_get));
            }
            OnConnectProgress();
            Thread.Sleep(1);

            //        private long slopeFactor;
            _get = Get_Param(0x0031,5);
            if (_get != "-")
            {
                datacount++;
                long a = (long)(Convert.ToInt64(_get));
                slopeFactor = (double) a / 10000;
            }
            OnConnectProgress();
            Thread.Sleep(1);

            //        private float vEsc;
            //_get = Get_Param(0x0033,5);
            //if (_get != "-")
            //{
            //    vEsc = Convert.ToDouble(_get, NumberFormatInfo.InvariantInfo);
            //}
            //OnConnectProgress();

            //        private int ctsEsc;
            //_get = Get_Param(0x0036,5);
            //if (_get != "-")
            //{
            //    ctsEsc = (int)(Convert.ToDecimal(_get));
            //}
            //OnConnectProgress();

            _get = Get_Param(0x0032,5);
            if (_get != "-")
            {
                datacount++;
                maxCounts = _get;
            }
            OnConnectProgress();
            Thread.Sleep(1);

            _get = Get_Param(0x0041, 5);
            if (_get != "-")
            {
                datacount++;
                geo_Local = _get;
            }
            OnConnectProgress();
            Thread.Sleep(1);

            _get = Get_Param(0x0042, 5);
            if (_get != "-")
            {
                datacount++;
                geo_Adjust = _get;
            }
            OnConnectProgress();
            Thread.Sleep(1);

            _get = Get_Param(0x0002, 5);
            if (_get != "-")
            {
                datacount++;
                vinMin = _get;
            }
            OnConnectProgress();
            Thread.Sleep(1);

            _get = Get_Param(0x0003, 5);
            if (_get != "-")
            {
                datacount++;
                vinMax = _get;
            }
            OnConnectProgress();
            Thread.Sleep(1);

            _get = Get_Param(0x0004, 5);
            if (_get != "-")
            {
                datacount++;
                voutMin = _get;
            }
            OnConnectProgress();
            Thread.Sleep(1);

            _get = Get_Param(0x0005, 5);
            if (_get != "-")
            {
                datacount++;
                voutMax = _get;
            }
            OnConnectProgress();
            Thread.Sleep(1);

            _get = Get_Param(0x0015, 5);
            if (_get != "-")
            {
                datacount++;
                bullMode = _get;
            }
            OnConnectProgress();
            Thread.Sleep(1);


            if (uDP_AP_Remote_Port != null)
            {
                uint p = uint.Parse(uDP_AP_Remote_Port) + 1;
                uDP_STA_Remote_Port = string.Format("{0}", p);
            }
            else
            {
                uDP_STA_Remote_Port = "";
            }

            if (uDP_AP_Local_Port != null)
            {
                uint p1 = uint.Parse(uDP_AP_Local_Port) + 1;
                uDP_STA_Local_Port = string.Format("{0}", p1);
            }
            else
            {
                uDP_STA_Local_Port = "";
            }

            //Console.WriteLine("datacount = " + datacount);
            if (datacount>=47)
            {
                scale_info = true;
                isNotConnected = false;
                SendCommand("\u000200FFE10110000\u0003\r\n");
            }
            else
            {
                scale_info = false;
            }

            OnPropertyChanged("DefinitionString");
            ChangeScaleDef();

            if (aTimer != null)
            {
                aTimer.Enabled = true;
            }

        }

        public string Get_Param(int param, int cops)
        {

            string resposta_esperada;
            string data = "-";
            int l;
            int n_send = 0;
            
           
            ConnectState = false;

            //prepara comando lectura a XTREM
            string command = string.Format("\u000200{0:X2}R{1:X4}0000\u0003\r\n", 0xff, param);

            
            if (Udp == false)
            {
                      
                if (comPort == null)
                {
                    return data;
                }

                if (comPort.IsOpen == false)
                {
                    return data;
                }

                //respuesta esperada
                resposta_esperada = string.Format("\u0002..00r{0:X4}..*..\u0003", param);

                int _cops = cops;

                IsWaitingData = true;
                              
                while (data == "-")
                {
                    comPort.Write(command);

                    string datarec = "";
                    bool stop = false;
                    decimal milliseconds = DateTime.Now.Ticks / (decimal)TimeSpan.TicksPerMillisecond;
                    decimal currentms = 0;
                    decimal timeout = 50;
                    while (!stop)
                    {
                        if (rx_buffer.Length > 0)
                        {
                            if (Regex.IsMatch(rx_buffer, resposta_esperada) == true )
                            {
                                datarec = rx_buffer;
                                stop = true;
                            }

                        }

                        currentms = DateTime.Now.Ticks / (decimal)TimeSpan.TicksPerMillisecond;
                        if ((currentms - milliseconds) > timeout)
                        {
                            stop = true;
                        }
                            
                    }
                   
                    //Console.WriteLine("Stops before timeout ends " + (currentms - milliseconds) + "," + n_send);
                                       

                    if (datarec.Length > 0)
                    {
                        int first = Regex.Match(datarec, resposta_esperada).Index;
                        int len = Regex.Match(datarec, resposta_esperada).Length;
                        if (first > -1)
                        {
                            if (len > 11)
                            {
                                l = Convert.ToInt32(datarec.Substring(first + 10, 2), 16);

                                if (len >= 14 + l)
                                {
                                    data = datarec.Substring(first + 12, l);

                                }
                            }
                        }
                        //rx_buffer = "";
                        //ConnectState = true;

                    }

                    if (data == "-")
                    {
                        //Console.WriteLine("Get_Param(" + string.Format("0x{0:X4}", param) + ") error in " + comPort.PortName);
                    }

                    n_send++;

                    if (n_send > _cops)
                    {
                        break;
                    }

                    //Console.WriteLine(string.Format("{0:X4}",param) + " " + n_send );

                    

                }

                                
                IsWaitingData = false;
                                

                return data;

            }
            else
            {
                //respuesta esperada
                resposta_esperada = string.Format("\u0002..00r{0:X4}..*..\u0003", param);

                string datarec = "";

                //DataReceived recv = new DataReceived();

                int _cops = cops * 2;

                byte[] Udp_Send = System.Text.Encoding.UTF8.GetBytes(command);
               
                IsWaitingData = true;

                while (data == "-")
                {
                    //rx_buffer = "";
                    Listener.Send(Udp_Send, Udp_Send.Length, UdpSendEndpoint);

                    bool stop = false;
                    decimal milliseconds = DateTime.Now.Ticks / (decimal)TimeSpan.TicksPerMillisecond;
                    decimal currentms = 0;
                    decimal timeout = 200;
                    while (!stop)
                    {
                        if (rx_buffer.Length > 0)
                        {

                            if (Regex.IsMatch(rx_buffer, resposta_esperada) == true)
                            {
                                datarec = rx_buffer;
                                stop = true;
                            }
                            
                        }

                        currentms = DateTime.Now.Ticks / (decimal)TimeSpan.TicksPerMillisecond;
                        if ((currentms - milliseconds) > timeout)
                        {
                            stop = true;
                        }

                    }

                    //Console.WriteLine("Stops before timeout ends " + (currentms - milliseconds) + "," + n_send);

                    if (datarec.Length > 0)
                    {
                        int first = Regex.Match(datarec, resposta_esperada).Index;
                        int len = Regex.Match(datarec, resposta_esperada).Length;
                        if (first > -1)
                        {
                            if (len > 11)
                            {
                                l = Convert.ToInt32(datarec.Substring(first + 10, 2), 16);

                                if (len >= 14 + l)
                                {
                                    data = datarec.Substring(first + 12, l);

                                }
                            }
                        }
                        //rx_buffer = "";
                        //ConnectState = true;

                    }
                                                         

                    if (data == "-")
                    {
                        //Console.WriteLine("Get_Param(" + string.Format("0x{0:X4}", param) + ") error in " + udpSendEndpoint.Address + ":" + udpSendEndpoint.Port);
                    }


                    //Console.Write(string.Format("{1} {0:X4}", param, n_send) + " | " + string.Format("{0:X4}", recv.Address) + ":" + recv.Data + " | " + rx_buffer);


                    n_send++;

                    if (n_send > _cops)
                    {
                        break;
                    }
                }

                IsWaitingData = false;
                                

                return data;
            }
                        

        }

        


        public int WriteParam(int param, string value)
        {
            int l;
            string data="";

            if (Udp == false)
            {
                IsWaitingData = true;

                try
                {
                                       
                    string resposta_esperada = string.Format("\u0002..00w{0:X4}.....\u0003*", param);
                   
                    int n_send = 0;
                    int _cops = 5;

                    while (data.Length == 0)
                    {
                        rx_buffer = "";
                        
                        string command = string.Format("\u000200{0:X2}W{1:X4}{2:X2}{3}00\u0003\r\n", id, param, value.Length, value);
                        comPort.Write(command);

                        string datarec = "";
                        bool stop = false;
                        decimal milliseconds = DateTime.Now.Ticks / (decimal)TimeSpan.TicksPerMillisecond;
                        decimal currentms = 0;
                        decimal timeout = 200;
                        while (!stop)
                        {
                            if (rx_buffer.Length > 0)
                            {
                                if (Regex.IsMatch(rx_buffer, resposta_esperada) == true)
                                {
                                    datarec = rx_buffer;
                                    stop = true;
                                }

                            }

                            currentms = DateTime.Now.Ticks / (decimal)TimeSpan.TicksPerMillisecond;
                            if ((currentms - milliseconds) > timeout)
                            {
                                stop = true;
                            }

                        }

                        //Console.WriteLine("Stops before timeout ends " + (currentms - milliseconds) + "," + n_send);
                                                
                        int first = Regex.Match(rx_buffer, resposta_esperada).Index;
                        if (first > -1)
                        {
                            if (Regex.Match(rx_buffer, resposta_esperada).Length > 11)
                            {
                                l = Convert.ToInt32(rx_buffer.Substring(first + 10, 2), 16);

                                if (Regex.Match(rx_buffer, resposta_esperada).Length >= 14 + l)
                                {
                                    data = rx_buffer.Substring(first + 12, l);
                                    if (param == 0x500 || param == 0x501 || param == 0x502 || param == 0x700 || param == 0x701 || param == 0x702)
                                    {
                                        data = "0";
                                    }
                                }
                            }
                        }

                        if (data.Length == 0)
                        {
                            //Console.WriteLine("WriteParam(" + string.Format("0x{0:X4}", param) + ") error in " + comPort.PortName);
                        }

                        n_send++;

                        if (n_send > _cops)
                        {
                            break;
                        }
                    }

                                        
                    //ComPort.Write(Constants.StartSending);
                    isNotConnected = false;
                    IsWaitingData = false;

                    if (data == "0")
                    {
                        return 0;
                    }
                    else
                    {
                        if (param==0x602 && data == "3")
                        {
                            return 0;
                        }

                        return -1;
                    }

                }
                catch
                {                                                            
                    //ComPort.Write(Constants.StartSending);
                    isNotConnected = false;
                    IsWaitingData = false;

                    //Console.WriteLine("Error en WriteParam() RS232");

                    return 1;
                }

            }
            else
            {
                IsWaitingData = true;
                                
                try
                {

                    //Send write command
                    string command = string.Format("\u000200{0:X2}W{1:X4}{2:X2}{3}00\u0003\r\n", id, param, value.Length, value);
                    byte[] Udp_Send = System.Text.ASCIIEncoding.UTF8.GetBytes(command);

                    DataReceived recv = new DataReceived();
                    
                    IPEndPoint local = new IPEndPoint(0,0); 
                    int n_send = 0;
                    int _cops = 5;
                    if (param == 0x500 || param == 0x501 || param == 0x502 || param == 0x700 || param == 0x701 || param == 0x702)
                    {
                        _cops = 20;
                    }

                    while (data.Length == 0)
                    {
                        rx_buffer = "";

                        Listener.Send(Udp_Send, Udp_Send.Length, UdpSendEndpoint);

                        bool stop = false;
                        decimal milliseconds = DateTime.Now.Ticks / (decimal)TimeSpan.TicksPerMillisecond;
                        decimal currentms = 0;
                        decimal timeout = 200;
                        while (!stop)
                        {
                            if (rx_buffer.Length > 0)
                            {
                                recv = ParseDataReceived(rx_buffer);
                                if (recv.Address == param)
                                {
                                    stop = true; 
                                }

                            }

                            currentms = DateTime.Now.Ticks / (decimal)TimeSpan.TicksPerMillisecond;
                            if ((currentms - milliseconds) > timeout)
                            {
                                stop = true;
                            }

                        }

                        //Console.WriteLine("Stops before timeout ends " + (currentms - milliseconds) + "," + n_send);

                        if (recv.Address == param)
                        {
                            data = recv.Data;
                                                                                    
                        }


                        //Console.Write(string.Format("{1} {0:X4}", param, n_send) + " | " + string.Format("{0:X4}", recv.Address) + ":" + recv.Data + " | " + rx_buffer);
                        if (data.Length == 0)
                        {
                            //Console.WriteLine("WriteParam(" + string.Format("0x{0:X4}", param) + ") error in " + udpSendEndpoint.Address + ":" + udpSendEndpoint.Port);
                        }


                        n_send++;

                        if (n_send > _cops)
                        {
                            break;
                        }
                    }

                    isNotConnected = false;

                    IsWaitingData = false;

                    if (data == "0")
                    {
                        if (param == 0x602 && data == "3")
                        {
                            return 0;
                        }

                        return 0;
                    }
                    else
                    {
                        return -1;
                    }
                    
                }
                catch (Exception ex)
                {
                    isNotConnected = false;

                    IsWaitingData = false;

                    Console.WriteLine(ex.Message);
                    //Console.WriteLine("Error en WriteParam() UDP");

                    return 1;
                }
            }
            
            
        }

        public void SendCommand(string command)
        {
            try
            {
                if (Udp == false)
                {
                    comPort.Write(command);
                }
                else
                {
                    IsWaitingData = true;

                    try
                    {
                        byte[] Udp_Send;
                        string data = "";
                        int param = int.Parse(command.Substring(6, 4), NumberStyles.AllowHexSpecifier, NumberFormatInfo.CurrentInfo);

                        //Send command
                        Udp_Send = System.Text.ASCIIEncoding.UTF8.GetBytes(command);

                        Listener.Send(Udp_Send, Udp_Send.Length, UdpSendEndpoint);
                        
                        DataReceived recv = new DataReceived();
                        IPEndPoint local = new IPEndPoint(0,0);
                        int n_send = 0;
                        int _cops = 5;

                                                
                        while (data.Length == 0)
                        {
                            
                            Listener.Send(Udp_Send, Udp_Send.Length, UdpSendEndpoint);

                            bool stop = false;
                            decimal milliseconds = DateTime.Now.Ticks / (decimal)TimeSpan.TicksPerMillisecond;
                            decimal currentms = 0;
                            decimal timeout = 200;
                            while (!stop)
                            {
                                if (rx_buffer.Length > 0)
                                {
                                    recv = ParseDataReceived(rx_buffer);
                                    if (recv.Address == param)
                                    {
                                        stop = true;
                                    }

                                }

                                currentms = DateTime.Now.Ticks / (decimal)TimeSpan.TicksPerMillisecond;
                                if ((currentms - milliseconds) > timeout)
                                {
                                    stop = true;
                                }

                            }

                            //onsole.WriteLine("Stops before timeout ends " + (currentms - milliseconds) + "," + n_send);
                                                                                

                            if (recv.Address == param)
                            {
                                data = recv.Data;
                                isNotConnected = false;

                            }

                            //Console.Write(string.Format("{1} {0:X4}", param, n_send) + " | " + string.Format("{0:X4}", recv.Address) + ":" + recv.Data + " | " + rx_buffer);
                            if (data.Length == 0)
                            {
                                //Console.WriteLine("SendCommand(" + string.Format("0x{0:X4}", param) + ") error in " + udpSendEndpoint.Address + ":" + udpSendEndpoint.Port);
                            }
                            
                            n_send++;

                            if (n_send > _cops)
                            {
                                break;
                            }
                        }
                                                                        
                        IsWaitingData = false;
                        
                    }
                    catch (Exception ex)
                    {
                        isNotConnected = true;
                        IsWaitingData = false;

                        Console.WriteLine(ex.Message);
                        //Console.WriteLine("Error en SendCommand() UDP");

                    }

                    
                }
                

            }
            catch
            {
                //Console.WriteLine("Error en SendCommand() general");
            }

        }


        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {

            //Console.WriteLine("Timeout elapsed at {0}", e.SignalTime);

            if (ConnectState == false)
            {
                ConnectState = false;

                if (comPort != null)
                {
                    if (comPort.IsOpen == false)
                    {
                        Init_SerialPort();
                    }

                    string check = Get_Param(0x100, 1);

                    if (check != "-")
                    {
                        if (IsNotConnected)
                        {
                            IsNotConnected = false;
                            OnRecallscaledef();
                        }

                        IsNotConnected = false;

                        if (scale_info == false)
                        {
                            OnRecallscaledef();
                        }

                        SendCommand("\u000200FFE10110000\u0003\r\n");
                    }
                    else
                    {
                        
                        IsNotConnected = true;
                        W_Display = "";
                    }
                }
                                               

                if (Udp == true)
                {
                    if (udpSendEndpoint.Address != null)
                    {
                        if (ReadUdpData.IsAlive == false)
                        {
                            ReadUdpData = new Thread(Listen_UDP_Async);

                            StopThread = false;
                            ReadUdpData.IsBackground = true;
                            ReadUdpData.Start();
                        }

                        string check = Get_Param(0x100, 2);

                        if (check != "-")
                        {
                            IsNotConnected = false;

                            if (scale_info == false)
                            {
                                OnRecallscaledef();
                            }

                            SendCommand("\u000200FFE10110000\u0003\r\n");

                        }
                        else
                        {
                            IsNotConnected = true;
                            W_Display = "";
                        }
                                                
                    }
                    
                }
                
            }
            else
            {
                if (IsNotConnected)
                {
                    IsNotConnected = false;
                    OnRecallscaledef();
                }

                IsNotConnected = false;
            }
            
            ConnectState = false;

        }

        
        public void ReadCOMWeightStream(object sender,
                SerialDataReceivedEventArgs e)
        {
            string command;

            try
            {
                if (comPort.IsOpen == false)
                {
                    return;
                }

                rx_buffer += comPort.ReadExisting();

                if (IsWaitingData)
                {
                    return;
                }

                while (rx_buffer.IndexOf('\u0002') != -1 && rx_buffer.IndexOf('\u0003') != -1)
                {

                    int ls = rx_buffer.IndexOf('\u0002');
                    ls++;

                    int le = rx_buffer.IndexOf('\u0003', ls);
                    if (le < ls)
                    {
                        rx_buffer = "";

                        return;
                    }

                    command = rx_buffer.Substring(ls, le - ls);

                    rx_buffer = rx_buffer.Substring(le + 1);

                    if (command.Length >= 15)
                    {
                        ParseWeightStream(command);
                    }

                }

                if (rx_buffer.IndexOf('\u0002') == -1)
                {
                    rx_buffer = "";
                }


            }
            catch
            {
                //Console.WriteLine("Error en ReadCOMWeightStream()");

                return;
            }
            
            //Thread.Sleep(10);
        }

        public void ReadUDPWeightStream()
        {
            string command;

            try
            {
                
                while (rx_buffer.IndexOf('\u0002') != -1 && rx_buffer.IndexOf('\u0003') != -1)
                {

                    int ls = rx_buffer.IndexOf('\u0002');
                    ls++;

                    int le = rx_buffer.IndexOf('\u0003', ls);
                    if (le < ls)
                    {
                        rx_buffer = "";

                        return;
                    }

                    command = rx_buffer.Substring(ls, le - ls);


                    //rx_buffer = rx_buffer.Substring(le);
                    rx_buffer = rx_buffer.Substring(le + 1);

                    if (command.Length >= 15)
                    {
                        //ConnectState = true;
                        ParseWeightStream(command);

                    }

                }

            }
            catch
            {
                //Console.WriteLine("Error en ReadUDPWeightStream()");

                return;
            }


        }


        public struct DataReceived
        {
            public int Address;
            public string Data;
        }

        public DataReceived ParseDataReceived(string data_received)
        {
            DataReceived result = new DataReceived
            {
                Address = -1,
                Data = ""
            };

            if (data_received.Length < 17)
            {
                
                return result;
            }

            //device destination
            string dest_id = data_received.Substring(3, 2);
            if (dest_id != "00" && dest_id != "FF")
            {
                //Console.WriteLine("dest_id=" + dest_id);
                return result;
            }

            //Parameter code (Address)
            string address = data_received.Substring(6, 4);
            if (int.TryParse(address, NumberStyles.AllowHexSpecifier, NumberFormatInfo.CurrentInfo, out int ad) == true)
            {
                result.Address = ad;
            }

            //data length
            if (int.TryParse(data_received.Substring(10, 2), NumberStyles.AllowHexSpecifier, NumberFormatInfo.CurrentInfo, out int l) == false)
            {
                //Console.WriteLine("Parse subs(9,2)=" + data_received.Substring(9, 2));
                return result;
            }

            if (data_received.Length != l + 17)
            {
                //Console.WriteLine("data length = " + data_received.Length + " / l +13 = " + (l + 13));
                return result;
            }
                        
            result.Data = data_received.Substring(12, l);

            return result;
        }

        public void ParseWeightStream(string data_received)
        {
            bool Flag_Change = false;
            bool CurrentFlag;
            string Disp_weight;

            //Device Id
            //string sender_id = cmd.Substring(0, 2);

            //device destination
            string dest_id = data_received.Substring(2, 2);
            if (dest_id != "00" && dest_id != "FF")
            {
                //Console.WriteLine("dest_id=" + dest_id);
                return;
            }

            //Console.WriteLine(data_received);

            //Parameter code (Address)
            string address = data_received.Substring(5, 4);
            

            //data length
            if (int.TryParse(data_received.Substring(9, 2), NumberStyles.AllowHexSpecifier, NumberFormatInfo.CurrentInfo, out int l) == false)
            {
                //Console.WriteLine("Parse subs(9,2)=" + data_received.Substring(9, 2));
                return;
            }

            if (data_received.Length != l + 13)
            {
                //Console.WriteLine("data length = " + data_received.Length + " / l +13 = " + (l + 13));
                return;
            }

            //data received
            //string data_rec = cmd.Substring(11, l);


            switch (address)
            {
                case "0107":

                    //decimal milliseconds = DateTime.Now.Ticks / (decimal)TimeSpan.TicksPerMillisecond;
                    //tlast = milliseconds - tlast;
                    //Console.WriteLine("tlast = " + tlast);
                    //tlast = milliseconds;
                    
                    ConnectState = true;

                    //weighing flags
                    ushort Weight_status = 0;

                    if (ushort.TryParse(data_received.Substring(34, 3), NumberStyles.AllowHexSpecifier, NumberFormatInfo.CurrentInfo, out ushort result))
                    {
                        Weight_status = result;
                    }


                    CurrentFlag = Convert.ToBoolean(Weight_status & 1);
                    if (CurrentFlag != w_Flag_Zero)
                    {
                        Flag_Change |= true; 
                    }
                    else
                    {
                        OnPropertyChanged("W_Flag_Zero");
                    }

                    _ = (CurrentFlag) == true
                        ? w_Flag_Zero = true
                        : w_Flag_Zero = false;

                    CurrentFlag = Convert.ToBoolean(Weight_status & 2);
                    if (CurrentFlag != w_Flag_Tare)
                    {
                        Flag_Change |= true;
                    }
                    else
                    {
                        OnPropertyChanged("W_Flag_Tare");
                    }

                    _ = (CurrentFlag) == true
                        ? w_Flag_Tare = true
                        : w_Flag_Tare = false;

                    CurrentFlag = Convert.ToBoolean(Weight_status & 8);
                    if (CurrentFlag != w_Flag_NetoDisp)
                    {
                        Flag_Change |= true;
                    }
                    else
                    {
                        OnPropertyChanged("W_Flag_NetoDisp");
                    }

                    _ = (CurrentFlag) == true
                        ? w_Flag_NetoDisp = true
                        : w_Flag_NetoDisp = false;

                    CurrentFlag = Convert.ToBoolean(Weight_status & 4);
                    if (CurrentFlag != w_Flag_Stability)
                    {
                        Flag_Change |= true;   
                        if (CurrentFlag == true)
                        {
                            if (StabilityTimer != null)
                                StabilityTimer.Start();
                        }
                        else
                        {
                            if (StabilityTimer != null)
                                StabilityTimer.Stop();
                        }
                    }
                    else
                    {                       
                        OnPropertyChanged("W_Flag_Stability");
                    }

                    _ = (CurrentFlag) == true
                        ? w_Flag_Stability = true
                        : w_Flag_Stability = false;

                    if (w_Flag_Stability == false)
                    {
                        StabilityElapsedTime = TimeSpan.Zero;
                        
                    }
                    else
                    {
                        if (StabilityElapsedTime > TimeSpan.FromMilliseconds(stabilityTime) && stabilityTime > 0)
                        {
                            StabilityElapsedTime = TimeSpan.Zero;
                            if (StabilityTimer != null)
                                StabilityTimer.Stop();
                            OnNewStableWeight();
                        }
                    }
                    
                    CurrentFlag = Convert.ToBoolean(Weight_status & 0x20);
                    if (CurrentFlag != w_Flag_HighRes)
                    {
                        if (w_Flag_HighRes == true)
                        {
                            W_Hold = 0;
                        }
                        Flag_Change |= true;
                    }
                    else
                    {
                        OnPropertyChanged("W_Flag_HighRes");
                    }
                    _ = (CurrentFlag) == true
                        ? w_Flag_HighRes = true
                        : w_Flag_HighRes = false;


                    string w = data_received.Substring(12, 8);
                    if (w.IndexOf('.') > 0)
                    {
                        decimalPlaces = w.Length - w.IndexOf('.') - 1;
                    }
                    else
                    {
                        decimalPlaces = 0;
                    }


                    ResFactor = (Weight_status & 0x20) == 0x20 ? 10 : 1;

                    //hold mode
                    int dec = decimalPlaces - (int)ResFactor / 10;
                    double noload = 20 * curEsc / Math.Pow(10, dec);
                    if (w_Flag_Stability && (w_Net < noload))
                    {
                        holdWeightChange = true;
                                                
                    }

                    if (w_Flag_Stability && w_Net > (double)curEsc)
                    {

                        if (holdWeightChange == true || w_Net > W_Hold)       // && Scale.W_Net > holdWeight)
                        {
                            holdWeightChange = false;
                            W_Hold = W_Brut;
                        }
                    }

                    W_Brut = Convert.ToDouble(data_received.Substring(12, 8), NumberFormatInfo.InvariantInfo);

                    /*
                    if (W_Flag_HighRes == true)
                    {
                        AdcCts = (long)(W_Brut * Math.Pow(10, DecimalPlaces-1) * slopeFactor);
                        double _VEsc = (slopeFactor * (double)curEsc) / Convert.ToDouble(maxCounts) ;
                        double _VInput = (double)(W_Brut * Math.Pow(10, DecimalPlaces - 1) * _VEsc / (double) curEsc * Math.Pow(10, DecimalPlaces - 1));
                        VInput = string.Format(new NumberFormatInfo() { NumberDecimalDigits = 3, NumberDecimalSeparator = "." }, "{0:F} mV", _VInput);
                    }
                    else
                    {
                        AdcCts = (long)(W_Brut * Math.Pow(10, DecimalPlaces) * slopeFactor);
                        double _VEsc = (slopeFactor * (double)curEsc) / Convert.ToDouble(maxCounts) ;
                        double _VInput = (double)(W_Brut * Math.Pow(10, DecimalPlaces) * _VEsc / (double)curEsc * Math.Pow(10, DecimalPlaces));
                        VInput = string.Format(new NumberFormatInfo() { NumberDecimalDigits = 3, NumberDecimalSeparator = "." }, "{0:F} mV", _VInput);

                    }*/


                    W_Tare = Convert.ToDouble(data_received.Substring(23, 8), NumberFormatInfo.InvariantInfo);
                    W_Net = W_Brut - W_Tare;
                    w_Unit = data_received.Substring(20, 2);
                    Disp_weight = w_Flag_NetoDisp
                        ? string.Format(new NumberFormatInfo() { NumberDecimalDigits = decimalPlaces, NumberDecimalSeparator = "." }, "{0:F} {1}", w_Net, w_Unit)
                        : string.Format(new NumberFormatInfo() { NumberDecimalDigits = decimalPlaces, NumberDecimalSeparator = "." }, "{0:F} {1}", W_Brut, w_Unit);


                    CurrentFlag = Convert.ToBoolean(w_Display == Disp_weight);
                    if (CurrentFlag == false)
                    {
                        Flag_Change |= true;
                    }

                    

                    if (HoldMode == true)
                    {
                        W_Display = string.Format(new NumberFormatInfo() { NumberDecimalDigits = decimalPlaces, NumberDecimalSeparator = "." }, "{0:F} {1}", W_Hold, w_Unit);
                    }
                    else
                    {
                        W_Display = Disp_weight;
                    }
                    

                    //QR Code 
                    string _weight_value;
                    if (Double.TryParse(w_Display.Substring(0, w_Display.Length - 2), NumberStyles.Any, NumberFormatInfo.InvariantInfo, out double w_value))
                    {

                        _weight_value = Convert.ToString(w_value, NumberFormatInfo.CurrentInfo);
                    }
                    else
                    {
                        _weight_value = w_Display;
                    }
                    


                    //_weight_value = "-9999999,9";
                    //Encoder.TryEncode(string.Format("{0,12}",_weight_value), out W_qrCode);

                    break;

                case "0111":

                    Flag_Change = false;
                    
                    if (w_Display != "ADC L" && w_Display != "ADC H")
                    {
                        //Console.WriteLine(data_received.Substring(11, l));

                        AdcCts = Convert.ToInt64(data_received.Substring(11, l));

                        double _VInput = (double)adcCts / Convert.ToDouble(maxCounts) * 10;
                        VInput = string.Format(new NumberFormatInfo() { NumberDecimalDigits = 3, NumberDecimalSeparator = "." }, "{0:F} mV", _VInput);
                    }
                    
                    break;

                case "0210":

                    Flag_Change = false;
                                        
                    VinCts = data_received.Substring(11, l);
                    double _Vin = Convert.ToDouble(VinCts) / 71.8;
                    VinVolt = string.Format(new NumberFormatInfo() { NumberDecimalDigits = 1, NumberDecimalSeparator = "." }, "{0:F} Vcc", _Vin);
                                        
                    break;

                case "0202":

                    Flag_Change = false;

                    if (type == "XTREM-S")
                    {
                        string _stmp = data_received.Substring(11, l);
                        double _tmp = Convert.ToDouble(_stmp, NumberFormatInfo.InvariantInfo);
                        XTemp = string.Format(new NumberFormatInfo() { NumberDecimalDigits = 1, NumberDecimalSeparator = "." }, "{0:F} \u2103", _tmp);
                    }
                    else
                    {
                        XTemp = "not available";
                        GetXTemp = false;
                    }

                    break;

                case "0100":

                    ConnectState = true;

                    //error flags
                    ushort error_status = ushort.Parse(data_received.Substring(11, 2), NumberStyles.AllowHexSpecifier);
                    error_status &= 0x1F;

                    if (error_status == 0)
                    {
                        break;
                    }


                    if (error_status == 1)
                    {
                        Disp_weight = "Error 01";       //Flash memory error
                    }
                    else if (error_status == 2)
                    {
                        Disp_weight = "Error 02";       //ADC fail
                    }
                    else if (error_status == 3)
                    {
                        Disp_weight = "Error 03";       //Load cell input signal out of range (>30mV)
                    }
                    else if (error_status == 4)
                    {
                        Disp_weight = "ADC H";          //Load cell input signal too high
                        VInput = "> 20 mV";
                        AdcCts = 8388608;

                    }
                    else if (error_status == 5)
                    {
                        Disp_weight = "ADC L";          //Load cell input signal too low
                        VInput = "< -20 mV";
                        AdcCts = -8388608;
                    }
                    else if (error_status == 7)
                    {
                        //Disp_weight = "-OL-        ";           //Over load, weight > Max+9e
                        Disp_weight = "Over Load   ";           //Over load, weight > Max+9e
                    }
                    else
                    {
                        Disp_weight = error_status == 8 ? "_ _ _ _ _ _ _ _ _ " : "Error";
                    }


                    CurrentFlag = Convert.ToBoolean(w_Display == Disp_weight);
                    if (CurrentFlag == false)
                    {
                        Flag_Change |= true;



                    }

                    W_Display = Disp_weight;

                    break;

                default:
                    break;
            }

            if (Flag_Change == true)
            {
                OnWeightChanged();
                
            }

        }

        
        #region IDisposable Support
        private bool disposedValue = false; // Para detectar llamadas redundantes

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: elimine el estado administrado (objetos administrados).
                    StopThread = true;
                                       
                }

                // TODO: libere los recursos no administrados (objetos no administrados) y reemplace el siguiente finalizador.
                // TODO: configure los campos grandes en nulos.

                disposedValue = true;
            }
        }

        // TODO: reemplace un finalizador solo si el anterior Dispose(bool disposing) tiene código para liberar los recursos no administrados.
        // ~XTREM()
        // {
        //   // No cambie este código. Coloque el código de limpieza en el anterior Dispose(colocación de bool).
        //   Dispose(false);
        // }

        // Este código se agrega para implementar correctamente el patrón descartable.
        public void Dispose()
        {
            // No cambie este código. Coloque el código de limpieza en el anterior Dispose(colocación de bool).
            Dispose(true);
            // TODO: quite la marca de comentario de la siguiente línea si el finalizador se ha reemplazado antes.
            // GC.SuppressFinalize(this);
        }
        #endregion



    }
}
