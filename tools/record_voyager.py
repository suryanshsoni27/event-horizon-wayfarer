"""Capture the opt-in Twelve Horizons feature showcase and assemble its original ambient score."""
from pathlib import Path
import json,os,re,shutil,subprocess,time,wave
import numpy as np

root=Path(__file__).resolve().parents[1];e=root/"evidence";frames=e/"voyager-frames";frames.mkdir(parents=True,exist_ok=True)
exe=root/"Build/Event Horizon.app/Contents/MacOS/Event Horizon"
assert str(exe) not in subprocess.check_output(["ps","-A","-o","comm="],text=True),"Close the normal player first."
for path in frames.glob("*.png"):
    if path.stem.isdigit():path.unlink()
started=time.time()
with (e/"voyager-record-stdout.log").open("w") as log:
    subprocess.run([str(exe),"--voyager-record","-logFile",str(e/"voyager-record.log")],
                   env=dict(os.environ,BH_EVIDENCE=str(e)),stdout=log,stderr=subprocess.STDOUT,
                   check=True,timeout=480)
log=(e/"voyager-record.log").read_text()
assert not re.search(r"(?:NullReferenceException|MissingReferenceException|Shader error|DllNotFoundException)",log), "Player capture contains runtime errors."
match=re.search(r"VOYAGER_FILM_COMPLETE frames=(\d+)",log);assert match,"Capture did not finish."
paths=sorted(p for p in frames.glob("*.png") if p.stem.isdigit())
assert len(paths)==int(match[1]) and [int(p.stem) for p in paths]==list(range(len(paths)))
assert paths[0].stat().st_mtime>=started-.5
duration=len(paths)/12
chapters=[{"name":name,"start_seconds":int(frame)/12} for name,frame in re.findall(r"VOYAGER_FILM (\S+) frame=(\d+)",log)]
meta=[";FFMETADATA1","title=Wayfarer - Twelve Horizons","comment=Staged Unity gameplay with seeded resources, scripted input and cameras. Separately assembled original ambient score."]
for i,chapter in enumerate(chapters):
    end=chapters[i+1]["start_seconds"] if i+1<len(chapters) else duration
    meta.extend(["[CHAPTER]","TIMEBASE=1/1000",f'START={round(chapter["start_seconds"]*1000)}',f'END={round(end*1000)}',f'title={chapter["name"].replace("-"," ").title()}'])
metadata=e/"voyager-chapters.ffmeta";metadata.write_text("\n".join(meta)+"\n")
# Same original chord palette as the game; this is an editorial music bed, not captured live audio.
rate=22050;t=np.arange(round(duration*rate),dtype=np.float64)/rate
chords=np.array([[110,164.8138,220,261.6256],[87.3071,130.8128,174.6141,220],[130.8128,195.9977,261.6256,329.6276],[97.9989,146.8324,195.9977,246.9417]])
left=np.zeros_like(t);right=np.zeros_like(t)
for voice in range(4):
    frequency=chords[(t//4).astype(int)%4,voice]
    phase=np.cumsum(frequency)*2*np.pi/rate
    left+=np.sin(phase)*.055;right+=np.sin(phase+t*.13*2*np.pi)*.055
envelope=np.sin((t%4)/4*np.pi)*np.minimum(1,t/2)*np.minimum(1,(duration-t)/2)
pcm=np.stack([left*envelope,right*envelope],axis=1)
score=e/"voyager-editorial-score.wav"
with wave.open(str(score),"wb") as wav:
    wav.setnchannels(2);wav.setsampwidth(2);wav.setframerate(rate);wav.writeframes((np.clip(pcm,-1,1)*32767).astype("<i2").tobytes())
ffmpeg=shutil.which("ffmpeg");assert ffmpeg
video=e/"twelve-horizons-tour.mp4"
subprocess.run([ffmpeg,"-hide_banner","-loglevel","error","-y","-framerate","12","-i",str(frames/"%04d.png"),
                "-i",str(score),"-i",str(metadata),"-map","0:v:0","-map","1:a:0","-map_metadata","2","-map_chapters","2",
                "-c:v","libx264","-preset","fast","-crf","20","-pix_fmt","yuv420p","-c:a","aac","-b:a","128k",
                "-af","loudnorm=I=-18:TP=-1.5:LRA=9","-shortest","-movflags","+faststart",str(video)],check=True)
(e/"voyager-record-manifest.json").write_text(json.dumps({"frames":len(paths),"fps":12,"duration_seconds":duration,
    "seeded_resources":True,"scripted_input":True,"staged_cameras":True,"live_audio_capture":False,
    "audio":"Separately synthesized original ambient score using the game's chord palette.",
    "performance_benchmark":False,"chapters":chapters},indent=2)+"\n")
for path in paths:path.unlink()
print(f"Encoded {len(paths)} frames, {duration:.1f} seconds; temporary frames cleared.")
