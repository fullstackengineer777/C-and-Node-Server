using PC_XTREM;
using System;
using System.IO.Ports;
using System.Windows;
using System.Windows.Data;

namespace Xtrem_Scale_Example_Serial
{


    public partial class MainWindow : Window
    {
        // The XTREM class manage communication with a GRAM Xtrem scale, both using a serial port or a network connection.
        // Once initialized, you just need binding your controls to the scale information.
        //
        // Scale.W_Display is a string containing the weight value plus the weighing unit
        // Scale.W_Brut .W_Tare .W_Net are numeric values 
        // Scale.W_Unit is a string with the weighing unit (kg, g, lb, oz)
        // Scale.DefinitionString is the "product plate" (in some countries is mandatory to show it close to the weighing display)
        //
        // When you cannot connect to the scale (or communication is lost) Scale.IsNotConnected is set to "true",
        // and will keep trying to connect


        private Xtrem scale = new Xtrem();
        public Xtrem Scale { get => scale; set => scale = value; }

        public MainWindow()
        {
            InitializeComponent();

            InitScale(Scale, "COM4",9600);    //Initializing the scale using COM3, 115200, n, 8, 1

            DataContext = this;                 //refresh bindings after change Scale content
         
        }

        private void InitScale(Xtrem s, string port, int baud)
        {
            s.Udp = false;          //not a network device
           
            //setting up the serial port
            using (s.ComPort = new SerialPort { })
            {
                // set the appropriate properties.
                s.ComPort.PortName = port;
                s.ComPort.BaudRate = baud;
                s.ComPort.Parity = Parity.None;
                s.ComPort.DataBits = 8;
                s.ComPort.StopBits = StopBits.One;
                s.ComPort.Handshake = Handshake.None;

                // Set the read/write timeouts
                s.ComPort.ReadTimeout = 100;
                s.ComPort.WriteTimeout = 100;
                s.ComPort.ReadBufferSize = 8192;

            }

            
            s.Init_SerialPort();            //open the serial port and a new SerialDataReceive event handler 

            s.GetScaleDef();                //gets all the scale settings (name, serial number, Max capacity, division ...)
            s.Init_Scale();                 //command the scale to start sending weigh information and initialize some timers

        }


        private void Zero_Button_Click(object sender, RoutedEventArgs e)
        {
            Scale.SendCommand("\u000200FFE01050000\u0003\r\n");
        }

        private void Tare_Button_Click(object sender, RoutedEventArgs e)
        {
            Scale.SendCommand("\u000200FFE01020000\u0003\r\n");
        }


    }

    public class BoolToVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {

            if (value != null)
            {
                if ((bool)value == true)
                {
                    return Visibility.Visible;
                }
                else
                {
                    return Visibility.Hidden;
                }

            }

            return Visibility.Hidden;

        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {

            if (value != null)
            {
                if (value is Visibility)
                {
                    if ((Visibility)value == Visibility.Visible)
                        return true;
                    else
                        return false;
                }
            }


            return false;
        }
    }


}
