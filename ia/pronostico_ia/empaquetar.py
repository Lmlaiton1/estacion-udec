from pathlib import Path
import hashlib
import json
import zipfile

ROOT=Path(__file__).resolve().parent
files=[p for p in ROOT.rglob('*') if p.is_file() and p.name!='MANIFIESTO_SHA256.json'
       and p.suffix!='.log' and not any(x in p.parts for x in ['__pycache__','.ipynb_checkpoints','.ipython'])]
hashes={p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest() for p in files}
(ROOT/'MANIFIESTO_SHA256.json').write_text(json.dumps(hashes,ensure_ascii=False,indent=2),encoding='utf-8')
archive=ROOT.parent/'PRONOSTICO_IA_COMPLETO.zip'
with zipfile.ZipFile(archive,'w',zipfile.ZIP_DEFLATED) as z:
    for rel in list(hashes)+['MANIFIESTO_SHA256.json']:z.write(ROOT/rel,ROOT.name+'/'+rel)
with zipfile.ZipFile(archive) as z:
    assert z.testzip() is None
    for rel,sha in hashes.items():assert hashlib.sha256(z.read(ROOT.name+'/'+rel)).hexdigest()==sha
print(json.dumps({'archivo':str(archive),'archivos':len(hashes)+1,'bytes':archive.stat().st_size,'CRC_SHA256':'correctos'},indent=2))
