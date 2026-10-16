import http from 'node:http';import fs from 'node:fs';import path from 'node:path';import {fileURLToPath} from 'node:url';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const types={'.html':'text/html','.js':'text/javascript','.json':'application/json','.wav':'audio/wav','.png':'image/png','.jpg':'image/jpeg','.svg':'image/svg+xml'};
export function serve(port=5195){return http.createServer((req,res)=>{let name=decodeURIComponent(new URL(req.url,'http://localhost').pathname);let file=path.resolve(root,'.'+(name==='/'?'/index.html':name));if(!file.startsWith(root+path.sep)){res.writeHead(403).end();return}try{let stat=fs.statSync(file);res.writeHead(200,{'Content-Type':types[path.extname(file)]||'application/octet-stream','Content-Length':stat.size});fs.createReadStream(file).pipe(res)}catch{res.writeHead(404).end()}}).listen(port,'127.0.0.1')}
if(process.argv[1]===fileURLToPath(import.meta.url)){serve();console.log('Preview: http://127.0.0.1:5195')}
