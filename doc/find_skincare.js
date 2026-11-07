const http = require('http');

http.get('http://localhost:5116/Home/GetProductTab?category=skincare', res => {
    let html = '';
    res.on('data', d => html += d);
    res.on('end', () => {
        const regex = /href="([^"]+)"/g;
        let match;
        const links = new Set();
        while ((match = regex.exec(html)) !== null) {
            links.add(match[1]);
        }
        console.log('Skincare Product Links:');
        Array.from(links).forEach(l => console.log(l));
    });
});
