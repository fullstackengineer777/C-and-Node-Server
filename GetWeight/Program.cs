using PC_XTREM;
using System;
using System.Net;
using System.Threading;



namespace GetWeight
{
    class Program
    {
        private static Xtrem scale = new Xtrem();
        public static Xtrem Scale { get => scale; set => scale = value; }

        static void Main(string[] args)
        {

            if (args.Length == 0)
            {
                Console.WriteLine("Format must be .\\GetWeight IPAdress");

            }
            else
            {
                Console.WriteLine("Scale init...");

                InitScale(Scale, IPAddress.Parse("127.0.0.1"), 5556, 4445);

                // TEST 3
                Scale.SendCommand("\u000200FFE10110000\u0003\r\n");                 //Start sending (stream mode)

                while (Scale.W_Display == "")
                {

                    Thread.Sleep(100);                                              //this way you can stop the loop in the case of no connection to the scale

                    if (Scale.IsNotConnected == true)                               //error scale not responding
                    {
                        Console.WriteLine("No response");
                        break;
                    }
                }

                Scale.SendCommand("\u000200FFE10100000\u0003\r\n");                 //Stop sending

                Console.WriteLine(Scale.W_Display);                                 //write  W_Display to the console

            }

        }


        static void InitScale(Xtrem s, IPAddress ip, int sendport, int recport)
        {
            s.Udp = true;          //its a network device, not a serial one

            s.IsNotConnected = true;

            s.UdpSendEndpoint = new IPEndPoint(address: ip, port: recport);
            s.UdpRecPort = sendport;

            s.Init_Udp_Comms();             //Init network communication


        }


    }
}
