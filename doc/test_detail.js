const http = require('http');

http.get('http://localhost:5116/Home/GetProductTab?category=all', res => {
    let html = '';
    res.on('data', d => html += d);
    res.on('end', () => {
        const regex = /href="([^"]+)"/g;
        let match;
        const links = [];
        while ((match = regex.exec(html)) !== null) {
            links.push(match[1]);
        }
        console.log('Found links in GetProductTab:');
        console.log(links.slice(0, 10));
    });
});
