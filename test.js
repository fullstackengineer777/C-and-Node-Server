var udp = require('dgram');

var sendport = 5556, recport = 4445;

//const prompt = require('prompt-sync')();
//const clientip = prompt('What is the IP of the client?   ');
//console.log(`Client's IP is ${clientip}`);

// --------------------creating a udp server --------------------// 
//var server = udp.createSocket({ type: 'udp4', reuseAddr: true });
var server = udp.createSocket('udp4');
//server.bind({ port: recport });
//server.connect(sendport, clientip);

//================ Server is listening
server.on('listening', function () {
    var address = server.address();
    console.log(`the ip of server is ${address}`);
    var port = address.port;
    console.log('Server is listening at port   ' + port);
});
//================ When receiving data from client 
server.on('message', function (msg, info) {
    console.log('Data received from client : ' + msg.toString());
    console.log('Received %d bytes from %s:%d\n', msg.length, info.address, info.port);
    //sending msg to the client
    var response = Buffer.from('From server : your msg is received');
    server.send(response, info.port, 'localhost', function (error) {
        if (error) {
            client.close();
        } else {
            console.log('Data sent !');
        }
    });
});

//================ if an error occurs
server.on('error', function (error) {
    console.log('Error: ' + error);
    server.close();
});

function SendCommand(str) {
    let msg = Buffer.from(str);
    server.send(msg, sendport , clientip, function (error) {
        if (error) {
            server.close();
        } else {
            console.log('Data sent from Server!');
        }
    });
}
