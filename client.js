var udp = require('dgram');

var client = udp.createSocket('udp4');

client.bind({port: 5556});
client.on('message', function (msg, info) {
    console.log('Data received from server : ' + msg.toString());
    console.log('Received %d bytes from %s:%d\n', msg.length, info.address, info.port);
});


var str = '***ff*010715*34.00000kg*48.00000***123';
var data = Buffer.from(str);
function sendDgram(){
    client.send(data, 4445, '10.10.10.243', function (error) {
        if (error) {
            console.log(error);
            client.close();
        } else {
            console.log('Data is sent !');
        }
    });
}
sendDgram(data);
//setInterval(sendDgram, 3000);
