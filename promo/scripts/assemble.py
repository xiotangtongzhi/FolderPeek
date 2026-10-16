import os,json,wave,subprocess,math
from pathlib import Path
import numpy as np
root=Path(__file__).resolve().parents[1];ff=os.environ.get('FFMPEG','ffmpeg');sr=48000
def save(path,a):
 with wave.open(str(path),'wb')as w:w.setnchannels(1);w.setsampwidth(2);w.setframerate(sr);w.writeframes(np.clip(a,-32767,32767).astype(np.int16).tobytes())
def stamp(t):
 n=round(t*1000);return f'{n//3600000:02}:{n//60000%60:02}:{n//1000%60:02},{n%1000:03}'
for lang in ['zh-CN','en-US']:
 r=root/lang;(r/'out').mkdir(exist_ok=True);markers=json.loads((r/'data/speech-markers.json').read_text(encoding='utf-8'));lead=1.0;tail=2.6
 raw=subprocess.check_output([ff,'-v','error','-i',str(r/'audio/continuous.mp3'),'-ar',str(sr),'-ac','1','-f','s16le','-'])
 a=np.frombuffer(raw,np.int16);audioDuration=len(a)/sr;duration=math.ceil((audioDuration+lead+tail)*30)/30
 continuous=np.pad(a,(round(lead*sr),round(duration*sr)-round(lead*sr)-len(a)))
 save(r/'audio/raw.wav',continuous)
 for i,s in enumerate(markers):
  s['voiceStart']+=lead;s['voiceEnd']+=lead
 for i,s in enumerate(markers):
  s['start']=0 if i==0 else (markers[i-1]['voiceEnd']+s['voiceStart'])/2
  s['end']=duration if i==len(markers)-1 else (s['voiceEnd']+markers[i+1]['voiceStart'])/2
  assert s['start']<=s['voiceStart']<s['voiceEnd']<=s['end']
 (r/'data/timeline.json').write_text(json.dumps(markers,ensure_ascii=False,indent=2),encoding='utf-8')
 subprocess.run([ff,'-y','-v','error','-i',str(r/'audio/raw.wav'),'-af','loudnorm=I=-17:TP=-2:LRA=6','-ar',str(sr),'-ac','2',str(r/'audio/voice-only.wav')],check=True)
 t=np.arange(len(continuous))/sr;music=np.zeros(len(t));chords=[[130.81,164.81,196],[110,130.81,164.81],[87.31,110,130.81],[98,123.47,146.83]]
 for k in range(math.ceil(duration/12)):
  ix=(t>=k*12)&(t<(k+1)*12);u=t[ix]-k*12;env=np.minimum(1,u/1.2)*np.minimum(1,(12-u)/1.6)
  for f in chords[k%4]:music[ix]+=(np.sin(2*np.pi*f*u)+.15*np.sin(2*np.pi*2*f*u))*.0028*env
 for beat in np.arange(0,duration,.625):
  ix=(t>=beat)&(t<beat+.35);u=t[ix]-beat;music[ix]+=.007*np.sin(2*np.pi*(72*u-45*u*u))*np.exp(-u*18)*np.minimum(1,u/.008)
 music*=np.minimum(1,t/2)*np.minimum(1,(duration-t)/2.6);save(r/'audio/score.wav',music*32767)
 subprocess.run([ff,'-y','-v','error','-i',str(r/'audio/voice-only.wav'),'-i',str(r/'audio/score.wav'),'-filter_complex','[0:a][1:a]amix=inputs=2:duration=first:normalize=0,alimiter=limit=0.89:level=0','-ar',str(sr),'-ac','2',str(r/'audio/narration.wav')],check=True)
 (r/'data/meta.json').write_text(json.dumps(dict(duration=duration,language=lang,continuousVoice=True,addedInterSentenceSilence=0,globalLeadingSilence=lead,globalTrailingSilence=tail),indent=2))
 (r/'out/FolderPeek_Bilingual.srt').write_text('\n\n'.join(f'{i+1}\n{stamp(s["voiceStart"])} --> {stamp(s["voiceEnd"])}\n{s["zh"]}\n{s["en"]}'for i,s in enumerate(markers))+'\n',encoding='utf-8-sig')
 print(lang,'duration',duration,'continuous speech',audioDuration,'sentences',len(markers))
