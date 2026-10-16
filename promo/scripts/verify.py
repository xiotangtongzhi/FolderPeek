import os,json,subprocess,hashlib,wave
from pathlib import Path
import numpy as np
from PIL import Image,ImageDraw
root=Path(__file__).resolve().parents[1];ff=os.environ.get('FFMPEG','ffmpeg')
for lang in ['zh-CN','en-US']:
 r=root/lang;video=r/f'out/FolderPeek_Promo_Rev2_{lang}_1080p.mp4';timeline=json.loads((r/'data/timeline.json').read_text(encoding='utf-8'));settings=json.loads((r/'data/speech-settings.json').read_text(encoding='utf-8'))
 decode=subprocess.run([ff,'-v','error','-i',str(video),'-f','null','-'],capture_output=True,text=True);assert decode.returncode==0 and not decode.stderr,decode.stderr
 p=subprocess.Popen([ff,'-v','error','-i',str(video),'-vf','scale=320:180','-f','rawvideo','-pix_fmt','rgb24','-'],stdout=subprocess.PIPE)
 targets={round((s['start']+(s['end']-s['start'])*.6)*30):s['id'] for s in timeline};count=0;frames=[];headerMax=0
 while True:
  b=p.stdout.read(320*180*3)
  if len(b)!=320*180*3:break
  a=np.frombuffer(b,np.uint8).reshape(180,320,3)
  # Lettering produces vertical variation; a moving uniform color wipe does not.
  band=a[4:12,235:310,:].astype(float);headerMax=max(headerMax,float(band.std(axis=0).max()))
  if count in targets:frames.append((targets[count],Image.fromarray(a.copy())))
  count+=1
 assert p.wait()==0
 report=json.loads((r/'out/render-report.json').read_text());assert count==report['frames'];assert headerMax<2,headerMax
 assert not report['errors']
 for i,s in enumerate(timeline):
  assert s['start']<=s['voiceStart']<s['voiceEnd']<=s['end']
  if i:assert abs(s['start']-timeline[i-1]['end'])<.001
 with wave.open(str(r/'audio/voice-only.wav'))as w:
  audio=np.frombuffer(w.readframes(w.getnframes()),np.int16).astype(float)/32768;sr=w.getframerate();channels=w.getnchannels();audioDuration=len(audio)/sr/channels
 assert abs(audioDuration-timeline[-1]['end'])<1/30
 result=dict(language=lang,voice=settings['voice'],duration=timeline[-1]['end'],frames=count,decode='PASS',browserErrors=[],topRightTextRemoved='PASS over every encoded frame',topRightMaxVerticalDeviation=headerMax,wordBoundaryCoverage=settings['coverage'],continuousNarration=True,addedInterSentenceSilence=0,voicePeak=float(abs(audio).max()),voiceClippingSamples=int((abs(audio)>.999).sum()),audioVideoSync='PASS',sha256=hashlib.sha256(video.read_bytes()).hexdigest(),bytes=video.stat().st_size)
 (r/'out/verification.json').write_text(json.dumps(result,indent=2));print(json.dumps(result,indent=2),flush=True)
 sheet=Image.new('RGB',(1600,1040),'#151a20');d=ImageDraw.Draw(sheet)
 for j,(sid,im)in enumerate(frames):
  x=j%5*320;y=j//5*208;sheet.paste(im,(x,y));d.text((x+10,y+186),f'{sid+1:02} {timeline[sid]["scene"]}',fill='white')
 sheet.save(r/'out/contact-sheet.jpg',quality=94)
 t=(timeline[17]['start']+timeline[17]['end'])/2
 subprocess.run([ff,'-v','error','-y','-ss',str(t),'-i',str(video),'-frames:v','1',str(r/'out/preview.png')],check=True)
