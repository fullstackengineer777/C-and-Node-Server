
const { throws } = require('assert');
const dgram = require('dgram');

class DataReceived {
    constructor(address, data){
        this.Address = address;
        this.Data = data;
    }
}

let Xtrem = class {

    sendport = 5556;
    recport = 4445;
    server = null;

    //communications error
    isNotConnected = true;
    ConnectState;
    rx_buffer = "";
    scale_info = false;

    //Get Adc counts
    GetXTemp = false;

    //Scale definition
    rEsc;
    rMax;
    cimalPlaces;
    ResFactor;
    type = "";
    
    //adjust information
    initZero;
    //slopeFactor;
    //vEsc;
    vInput;
    ctsEsc;
    adcCts;

    curEsc = 1;
    maxCounts = 1;
            
    //geo adjust
    geo_Local;
    geo_Adjust;

    //weighing information
    w_Brut = 0;
    w_Tare = 0;
    w_Net = 0;
    w_Display = "";
    w_Unit = "";
    w_UnitCode = 0;

    w_Flag_Zero = false;
    w_Flag_Tare = false;
    w_Flag_Stability = false;

    stabilityTime;
    
    w_Flag_NetoDisp = false;
    w_Flag_HighRes = false;
    //W_Flag_InitZero = false;
    //W_Flag_ManualTare = false;

    //W_Flag_TareMode = 0;
    // W_Range = 0;

    holdWeightChange = null;
    W_Hold = null;
    HoldMode = false;

    Unit = [ "", "g ", "kg", "oz", "lb" ];
    R_Mode = [ "Single range", "2 ranges", "2 intervals" ];
    
    constructor( clientip){
        this.clientip = clientip;
    }

    InitScale(){
        
        // --------------------creating a udp server --------------------//
        this.server = dgram.createSocket({ type: 'udp4', reuseAddr: true });

        this.server.bind({ 
            port: this.recport
        });
        //this.server.connect(this.sendport, this.clientip);//??????

        this.server.on('error', (err) => {

            //sending end diagram.
            let end_msg = "\u000200FFE10100000\u0003\r\n";
            this.SendCommand(end_msg);

            console.log(`server error:\n${err.stack}`);
            this.server.close();
        });

        //private async Listen_UDP_Async()
        this.server.on('message', (msg, rinfo) => {
             
            console.log(`server got: ${msg} from ${rinfo.address}:${rinfo.port}`);
            this.rx_buffer = msg.toString();
            
            this.ParseWeightStream(this.rx_buffer.substr(1, this.rx_buffer.length - 4));

            // let data_gram = Buffer.from("rx_buffer");
            // this.server.send(data_gram, this.sendport , this.clientip, function (error) {
            //     if (error) {
            //         console.log(error);
            //         this.server.close();
            //     } else {
            //     //  console.log('Data is sent !');
            //     }
            // });
            
        });

        
        this.server.on('listening', () => {
            const address = this.server.address();
            console.log('Connected!\n');
            console.log(`server listening ${address.address}:${address.port}`);
            //sending start diagram
            let start_msg = "\u000200FFE10110000\u0003\r\n";
            console.log("...sending command to start data  00FFE10110000")
            this.SendCommand(start_msg);

           //setInterval(this.CloseServer, 5000);
        });
        
    }
    
    CloseServer = () => {
        if((this.w_Display == "") && (this.isNotConnected ==  true)){
            console.log("No response");
            let end_msg = "\u000200FFE10100000\u0003\r\n";
            console.log("... sending command to end    00FFE10100000");
            this.SendCommand(end_msg);
            this.server.close();
        }
    }    
    // sending datagram from server
    SendCommand(command) {

        this.IsWaitingData = true; 
        let data = "";
        //get the ip of the remote client
        let param = parseInt(command.substr(6, 4), 16);
        
        //Send command
        let Udp_Send = Buffer.from(command);
        this.server.send(Udp_Send, this.sendport, this.clientip, function (error) {
            if (error) {
                this.server.close();
            } else {
                //console.log('Command has been sent from Server!');
            }
        });
        
        let recv = new DataReceived(-1,"");
        let n_send = 0;
        let _cops = 5;        
        while (data.length == 0)
        {
            //Listener.Send(Udp_Send, Udp_Send.Length, UdpSendEndpoint);
            this.server.send(Udp_Send, this.sendport, this.clientip, function (error) {
                if (error) {
                    this.server.close();
                } else {
                    //console.log('Command has been sent from Server! n = ' + (n_send+2));
                }
            });

            let stop = false;
            let milliseconds = new Date().getTime();
            let currentms = 0;
            let timeout = 200;
            while (!stop)
            {
                if (this.rx_buffer.length > 0)
                {
                    recv = this.ParseDataReceived(this.rx_buffer);
                    if (recv.Address == param)
                    {
                        stop = true;
                    }

                }

                currentms = new Date().getTime();
                if ((currentms - milliseconds) > timeout)
                {
                    stop = true;
                }

            }

            //onsole.WriteLine("Stops before timeout ends " + (currentms - milliseconds) + "," + n_send);
                                                                

            if (recv.Address == param)
            {
                data = recv.Data;
                this.isNotConnected = false;

            }

            //Console.Write(string.Format("{1} {0:X4}", param, n_send) + " | " + string.Format("{0:X4}", recv.Address) + ":" + recv.Data + " | " + rx_buffer);
            if (data.length == 0)
            {
                //console.log("SendCommand error " );
                //Console.WriteLine("SendCommand(" + string.Format("0x{0:X4}", param) + ") error in " + udpSendEndpoint.Address + ":" + udpSendEndpoint.Port);
            }
            
            n_send++;

            if (n_send > _cops)
            {
                break;
            }
        }
                                                        
        //IsWaitingData = false;
        
    }

    ParseDataReceived(data_received){
        let result = new DataReceived(-1,"");
        
        if (data_received.length < 17)
        {        
            return result;
        }

        //device destination
        let dest_id = data_received.substr(3, 2);
        if (dest_id != "00" && dest_id != "FF")
        {
           // console.log("dest_id=" + dest_id);
            return result;
        }

        //Parameter code (Address)
        let address = data_received.substr(6, 4);
        result.Address = parseInt(address, 16);
        console.log(`address = ${result.Address}`);
        
        //data length
        let l = parseInt(data_received.substr(10, 2), 16);
        console.log(`length = ${l}`);
        if (data_received.length != l + 17)
        {
            //Console.WriteLine("data length = " + data_received.Length + " / l +13 = " + (l + 13));
            return result;
        }
                    
        result.Data = data_received.substr(12, l);

        return result;
    }

    /*---------------  parse the received the udp datagram ----------------*/
    ParseWeightStream(data_received) {
        
        console.log("Packet received! \n ????????????????????");
        console.log("decoding packet");
       
        let CurrentFlag;
        let  Flag_Change = false;
        let Disp_weight;

        //device destination
        let dest_id = data_received.substr(2,2);
        if (dest_id !== "00" && dest_id !== "ff")
        {
            console.log("dest_id doesn't match :   " + dest_id);
            return;
        }
        
        //Parameter code (Address)
        let address = data_received.substr(5, 4);

        let hexstr = data_received.substr(9, 2);
        let l = parseInt(hexstr, 16);
        // console.log(data_received.length);
        // console.log(data_received);
        // console.log(l);
        if (data_received.length != l + 13)
        {
            console.log("Length field doesn't match");
            return;
        }
        console.log("address" , address);
        switch (address){
            case "0107":  
                //ConnectStatue = true;
                let Weight_status = parseInt(data_received.substr(34, 3), 16);
                CurrentFlag = Boolean( Weight_status & 1 );
                if (CurrentFlag != this.w_Flag_Zero)
                {
                    Flag_Change = Flag_Change | true; 
                }
                else
                {
                    // OnPropertyChanged("W_Flag_Zero");
                }
                CurrentFlag == true ? this.w_Flag_Zero = true : this.w_Flag_Zero = false;
                
                CurrentFlag = Boolean( Weight_status & 2 );
                if (CurrentFlag != this.w_Flag_Tare)
                {
                    Flag_Change = Flag_Change | true; 
                }
                else
                {
                // OnPropertyChanged("W_Flag_Zero");
                }
                CurrentFlag == true ? this.w_Flag_Tare = true : this.w_Flag_Tare = false;
                
                CurrentFlag = Boolean( Weight_status & 8 );
                if (CurrentFlag != this.w_Flag_NetoDisp)
                {
                    Flag_Change = Flag_Change | true; 
                }
                else
                {
                // OnPropertyChanged("W_Flag_Zero");
                }
                CurrentFlag == true ? this.w_Flag_NetoDisp = true : this.w_Flag_NetoDisp = false;
                
                CurrentFlag = Boolean( Weight_status & 4 );
                if (CurrentFlag != this.w_Flag_Stability)
                {
                    Flag_Change = Flag_Change | true;   
                /*   if (CurrentFlag == true)
                    {
                        if (StabilityTimer != null)
                            StabilityTimer.Start();
                    }
                    else
                    {
                        if (StabilityTimer != null)
                            StabilityTimer.Stop();
                    } */
                }
                else
                {
                // OnPropertyChanged("W_Flag_Stability");
                }
                CurrentFlag == true ? this.w_Flag_Stability = true : this.w_Flag_Stability = false;

                CurrentFlag = Boolean(Weight_status & 0x20);
                if (CurrentFlag != this.w_Flag_HighRes)
                {
                    if (this.w_Flag_HighRes == true)
                    {
                        this.W_Hold = 0;
                    }
                    Flag_Change = Flag_Change | true;
                }
                else
                {
                    //OnPropertyChanged("W_Flag_HighRes");
                }
                CurrentFlag == true ? this.w_Flag_HighRes = true : this.w_Flag_HighRes = false;

                let  w = data_received.substr(12, 8);
                let decimalPlaces = 0;
                if (w.indexOf('.') > 0)
                {
                    decimalPlaces = w.length - w.indexOf('.') - 1;
                }
                else
                {
                    decimalPlaces = 0;
                }
                this.ResFactor = (Weight_status & 0x20) == 0x20 ? 10 : 1;

                //hold mode
                let dec = decimalPlaces - parseInt(this.ResFactor) / 10;
                let noload = 20 * this.curEsc / Math.pow(10, dec);
                if (this.w_Flag_Stability && (this.w_Net < noload))
                {
                    this.holdWeightChange = true;
                                            
                }

                if (this.w_Flag_Stability && this.w_Net > parseFloat(this.curEsc))
                {

                    if (this.holdWeightChange == true || this.w_Net > this.W_Hold)       // && Scale.W_Net > holdWeight)
                    {
                        this.holdWeightChange = false;
                        this.W_Hold = this.W_Brut;
                    }
                }

                this.W_Brut = parseFloat(data_received.substr(12, 8));
                this.W_Tare = parseFloat(data_received.substr(23, 8));
                this.w_Net = this.W_Brut - this.W_Tare;
                this.w_Unit = data_received.substr(20, 2);
                Disp_weight = this.w_Flag_NetoDisp ?  this.w_Net.toFixed(decimalPlaces).toString() + " " + this.w_Unit
                        :  this.W_Brut.toFixed(decimalPlaces).toString() + " " + this.w_Unit;                 

                CurrentFlag = Boolean(this.w_Display == Disp_weight);
                if (CurrentFlag == false)
                {
                    Flag_Change = Flag_Change | true;
                }

                if (this.HoldMode == true)
                {
                    this.w_Display = this.W_Hold.toFixed(decimalPlaces).toString() + " " + this.w_Unit;
                }
                else
                {
                    this.w_Display = Disp_weight;
                }
                
                //QR Code 
                let _weight_value;
                let w_value = parseFloat(this.w_Display.substr(0, this.w_Display.length - 2))
                if (w_value)
                {
                    _weight_value = w_value.toString() + this.w_Unit;
                }
                else
                {
                    _weight_value = this.w_Display;
                }
                console.log("Weight on Scale is " + _weight_value);
                break;
            case "0111":  
                Flag_Change = false;
                        
                if (this.w_Display != "ADC L" && this.w_Display != "ADC H")
                {
                    
                    this.adcCts = parseInt(data_received.substr(11, l));
                    let _VInput = parseFloat(this.adcCts) / parseFloat(maxCounts) * 10;
                    this.vInput = _VInput.toFixed(3).toString() + " mV";                      
                }                    
                break;
            case "0210":
                Flag_Change = false;                                        
                let VinCts = data_received.substr(11, l);
                let _Vin = parseFloat(VinCts) / 71.8;
                VinVolt = _Vin.toFixed(1).toString() + " Vcc";
        
                break;
            case "0202":  
                Flag_Change = false;
                if (type == "XTREM-S")
                {
                    let _stmp = data_received.substr(11, l);
                    let _tmp = parseFloat(_stmp);
                    this.xTemp = _tmp.toFix(1).toString() + " \u2103";
              
                }
                else
                {
                    this.xTemp = "not available";
                    this.GetXTemp = false;
                }
                break;
            case "0100":
                ConnectState = true;
                //error flags
                let error_status = parseInt(data_received.substr(11, 2), 16);
                error_status = error_status & 0x1F;

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
                    this.vInput = "> 20 mV";
                    this.adcCts = 8388608;

                }
                else if (error_status == 5)
                {
                    Disp_weight = "ADC L";          //Load cell input signal too low
                    this.vInput = "< -20 mV";
                    this.adcCts = -8388608;
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
                
                CurrentFlag = Boolean(this.w_Display == Disp_weight);
                if (CurrentFlag == false)
                {
                    Flag_Change = Flag_Change | true;
                }

                this.w_Display = Disp_weight;  
                
                break;
            default:
                break;
        }
        if (Flag_Change == true)
        {
            //OnWeightChanged();        
        }

    }


}

module.exports = Xtrem;