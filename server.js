const { Console } = require('console');
const Xtrem = require('./XTREM');
const prompt = require('prompt-sync')();
let xtrem = require('./XTREM');


const clientip = prompt('What is the IP of the client?   ');

console.log('\n');
console.log('Running a node.js application\n');
console.log(`...connecting to ${clientip}:4445\n`);
xtrem = new Xtrem(clientip);
xtrem.InitScale();

//SendCommand('1234567890123456789');

