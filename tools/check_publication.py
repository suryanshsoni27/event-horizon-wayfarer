"""Check staged paths and text for files that must never enter the public source snapshot."""
from pathlib import PurePosixPath
import re,subprocess,sys

def git(*args):
 return subprocess.check_output(['git',*args])

paths=[p.decode() for p in git('diff','--cached','--name-only','--diff-filter=ACMR','-z').split(b'\0') if p]
if not paths:
 paths=[p.decode() for p in git('ls-files','-z').split(b'\0') if p]

blocked={'library','temp','logs','usersettings','archive','evidence','.venv','venv','__pycache__','.ssh','.aws','.azure'}
extensions={'.pem','.key','.p12','.pfx','.keystore','.jks','.dylib','.dll','.so','.log'}
failures=[]
for name in paths:
 p=PurePosixPath(name);parts={s.lower() for s in p.parts}
 if parts&blocked or p.suffix.lower() in extensions or any(s.lower()=='.env' or s.lower().startswith('.env.') and s.lower() not in {'.env.example','.env.sample'} for s in p.parts):
  failures.append((name,'private or generated path'));continue
 if name.startswith(('Build/','Builds/','native/build/')):
  failures.append((name,'build output'));continue
 data=git('show',':'+name)
 if len(data)>100*1024**2:failures.append((name,'oversized Git blob'))
 try:text=data.decode('utf-8')
 except UnicodeDecodeError:continue
 if re.search(r'/(?:Users|home)/[^\s"\']+',text):failures.append((name,'personal absolute path'))
 if re.search(r'-----BEGIN (?:[A-Z ]+)?PRIVATE KEY-----',text):failures.append((name,'private key material'))

for name,reason in failures:print(f'BLOCKED {name}: {reason}')
print(f'Publication path/content check: {len(paths)} files, {len(failures)} blocked.')
sys.exit(bool(failures))
