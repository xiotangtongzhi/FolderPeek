import fs from 'node:fs';import path from 'node:path';import {fileURLToPath,pathToFileURL} from 'node:url';import {spawn} from 'node:child_process';import {serve} from './serve.mjs';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');process.chdir(root);fs.mkdirSync('out',{recursive:true});
const dependencies=process.env.FILM_NODE_MODULES||path.join(root,'node_modules');
const {chromium}=await import(pathToFileURL(path.join(dependencies,'playwright-core/index.mjs')));
const {WebSocketServer}=await import(pathToFileURL(path.join(dependencies,'ws/wrapper.mjs')));
const arg=(name,def)=>{let i=process.argv.indexOf('--'+name);return i<0?def:process.argv[i+1]};
const ff=process.env.FFMPEG||'ffmpeg';
const chrome=process.env.CHROME||'C:/Program Files/Google/Chrome/Application/chrome.exe';
const timeline=JSON.parse(fs.readFileSync('data/timeline.json','utf8')),duration=timeline.at(-1).end;
const mode=arg('mode','video'),fps=+arg('fps',30),from=+arg('from',0),to=+arg('to',duration),out=arg('out','out/FolderPeek_v1.5_Promo_1080p.mp4');
const server=serve();let browser;
try{
browser=await chromium.launch({executablePath:chrome,headless:true,args:['--disable-background-timer-throttling','--disable-renderer-backgrounding']});
const page=await browser.newPage({viewport:{width:1920,height:1080}});const errors=[];page.on('pageerror',e=>{errors.push(String(e));console.error(e)});await page.goto('http://127.0.0.1:5197/?export=1');await page.waitForFunction(()=>window.film?.ready);await page.evaluate(()=>document.fonts.ready);
if(mode==='stills'){
 fs.mkdirSync('out/stills',{recursive:true});for(const s of timeline){for(const p of [.22,.55,.85]){const time=s.start+(s.end-s.start)*p;await page.evaluate(t=>window.film.render(t),time);const b=await page.evaluate(()=>window.film.png());fs.writeFileSync(`out/stills/${String(s.id).padStart(2,'0')}-${Math.round(p*100)}.png`,Buffer.from(b.split(',')[1],'base64'));}console.log('stills',s.id,s.scene)}
 const layout=await page.evaluate(async()=>{const lines=await fetch('data/timeline.json').then(r=>r.json());const c=document.querySelector('canvas').getContext('2d');let report=[];for(const s of lines){c.font='500 58px "Microsoft YaHei",Arial';report.push({id:s.id,titleWidth:c.measureText(s.title).width,titleFits:c.measureText(s.title).width<1730})}return report});fs.writeFileSync('out/layout-check.json',JSON.stringify({errors,titles:layout},null,2));
}else{
 const log=fs.openSync(out+'.log','w'),args=['-y','-hide_banner','-f','image2pipe','-vcodec','mjpeg','-r',String(fps),'-i','pipe:0','-ss',String(from),'-i','audio/narration.wav','-t',String(to-from),'-vf','scale=1920:1080:out_color_matrix=bt709','-c:v','libx264','-preset','fast','-crf','18','-pix_fmt','yuv420p','-color_primaries','bt709','-color_trc','bt709','-colorspace','bt709','-c:a','aac','-b:a','192k','-ac','2','-movflags','+faststart',out];
 const encoder=spawn(ff,args,{stdio:['pipe','ignore',log],windowsHide:true});const encoded=new Promise((resolve,reject)=>encoder.on('exit',c=>c===0?resolve():reject(Error('Encoder failed '+c))));let count=0;
 const wsServer=new WebSocketServer({port:5198});const done=new Promise((resolve,reject)=>{wsServer.on('connection',ws=>{ws.on('message',(data,binary)=>{if(!binary){encoder.stdin.end();resolve();return}encoder.stdin.write(data,()=>{count++;if(count%300===0)console.log('rendered',count,'/',Math.ceil((to-from)*fps));ws.send('ok')})});ws.on('error',reject)});wsServer.on('error',reject)});
 await page.evaluate(({from,to,fps})=>{window.exportPromise=window.film.stream('ws://127.0.0.1:5198',from,to,fps)},{from,to,fps});await done;await encoded;wsServer.close();fs.closeSync(log);fs.writeFileSync('out/render-report.json',JSON.stringify({frames:count,fps,from,to,errors,bytes:fs.statSync(out).size},null,2));console.log('COMPLETE',out);
}
if(errors.length)throw Error('Browser errors detected');
}finally{if(browser)await browser.close();server.close()}
