const { execFileSync } = require('child_process');
const path = require('path');
const fs = require('fs');

const chrome = 'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe';
const targetUrl = 'http://localhost:5116/san-pham/serum-phuc-hoi-da-la-roche-posay-hyalu-b5-30ml';
const outPath = path.join(__dirname, 'Hinh_4_2_Giao_Dien_Chi_Tiet_San_Pham.png');

console.log('Capturing product detail screenshot for Hình 4.2...');
execFileSync(chrome, [
    '--headless=new',
    '--disable-gpu',
    '--window-size=1920,1080',
    '--hide-scrollbars',
    '--screenshot=' + outPath,
    targetUrl
]);

console.log('Screenshot saved to:', outPath);
console.log('Size:', fs.statSync(outPath).size, 'bytes');
