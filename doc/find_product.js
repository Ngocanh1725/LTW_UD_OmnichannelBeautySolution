const http = require('http');

http.get('http://localhost:5116/', res => {
    let html = '';
    res.on('data', d => html += d);
    res.on('end', () => {
        const regex = /href="(\/Product\/Detail\/[^"]+)"/g;
        let match;
        const links = new Set();
        while ((match = regex.exec(html)) !== null) {
            links.add(match[1]);
        }
        console.log('Product Links found:');
        Array.from(links).slice(0, 15).forEach(l => console.log(l));
    });
});
