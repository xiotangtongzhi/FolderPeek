import sys,os,json,asyncio,re,unicodedata,time
from pathlib import Path
if os.environ.get('EDGE_TTS_PATH'):sys.path.insert(0,os.environ['EDGE_TTS_PATH'])
import edge_tts
root=Path(__file__).resolve().parents[1]
def norm(t):return ''.join(c.lower() for c in unicodedata.normalize('NFKC',t) if c.isalnum())
async def build(lang,voice,rate):
 r=root/lang;script=json.loads((r/'data/script.json').read_text(encoding='utf-8'));key='zh' if lang=='zh-CN' else 'en'
 spoken=[s[key] for s in script]
 if lang=='en-US':
  spoken=[s.replace('Ctrl-click','Control-click').replace('Ctrl-wheel','Control plus the mouse wheel').replace('SimSun','Sim Sun').replace('1.9','one point nine') for s in spoken]
 text=' '.join(spoken);events=[]
 print('START continuous narration',lang,voice,flush=True)
 with (r/'audio/continuous.mp3').open('wb')as f:
  async for chunk in edge_tts.Communicate(text,voice,rate=rate,pitch='+0Hz',volume='+0%',boundary='WordBoundary').stream():
   if chunk['type']=='audio':f.write(chunk['data'])
   elif chunk['type']=='WordBoundary':events.append(chunk)
 (r/'data/word-boundaries.json').write_text(json.dumps(events,ensure_ascii=False,indent=2),encoding='utf-8')
 canonical=''.join(map(norm,spoken));limits=[];end=0
 for s in spoken:end+=len(norm(s));limits.append(end)
 cursor=0;lines=[[]for s in script];fail=[]
 for e in events:
  token=norm(e['text'])
  if not token:continue
  loc=canonical.find(token,cursor)
  if loc<0:fail.append(e['text']);continue
  if loc>cursor:fail.append('skipped:'+canonical[cursor:loc])
  idx=next(i for i,n in enumerate(limits)if loc<n)
  lines[idx].append(e);cursor=loc+len(token)
 if cursor<len(canonical):fail.append('tail:'+canonical[cursor:])
 assert not fail,fail
 assert all(lines),[i for i,x in enumerate(lines)if not x]
 markers=[]
 for s,ev in zip(script,lines):markers.append(dict(s,voiceStart=ev[0]['offset']/1e7,voiceEnd=(ev[-1]['offset']+ev[-1]['duration'])/1e7))
 (r/'data/speech-markers.json').write_text(json.dumps(markers,ensure_ascii=False,indent=2),encoding='utf-8')
 (r/'data/speech-settings.json').write_text(json.dumps(dict(language=lang,voice=voice,rate=rate,pitch='+0Hz',method='continuous full-script synthesis; no per-sentence generation or joining',provider='Microsoft Edge neural TTS via edge-tts',spokenText=text,wordEvents=len(events),coverage='100% normalized spoken text'),ensure_ascii=False,indent=2),encoding='utf-8')
 print('COMPLETE',lang,len(events),'events',markers[-1]['voiceEnd'],'seconds',flush=True)
async def main():
 await asyncio.gather(build('zh-CN','zh-CN-XiaoxiaoNeural','-5%'),build('en-US','en-US-JennyNeural','-3%'))
asyncio.run(main())
