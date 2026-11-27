from pathlib import Path
import ctypes as C,json
root=Path(__file__).resolve().parents[1];lib=C.CDLL(str(root/'UnityProject/Assets/Plugins/macOS/libhelmetphoto.dylib'));lib.hp_image_info.argtypes=[C.c_char_p,C.POINTER(C.c_int),C.POINTER(C.c_int)];lib.hp_image_info.restype=C.c_int
results={}
for name,expected in [('synthetic-robot.png',1),('synthetic-robot.jpg',1),('transparent.png',1),('corrupt.png',0),('unsupported.gif',-1),('oversized.png',-2)]:
 w=C.c_int();h=C.c_int();status=lib.hp_image_info(str(root/'evidence/photo-fixtures'/name).encode(),C.byref(w),C.byref(h));assert status==expected,(name,status);results[name]={'status':status,'width':w.value,'height':h.value,'pass':True}
(root/'evidence/photo-format-checks.json').write_text(json.dumps(results,indent=2));print(json.dumps(results,indent=2))
