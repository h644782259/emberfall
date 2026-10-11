#!/usr/bin/env python3
"""Actual enemy factory + adapter geometry, and authored-byte transform regression."""
from pathlib import Path
import sys,tempfile,subprocess,struct
root=Path(__file__).resolve().parents[1]
b=(root/'Assets/Resources/TacticalAttachments/HuntBadge.bytes').read_bytes();magic,count,indices=struct.unpack_from('<III',b)
vertices=[struct.unpack_from('<8f',b,12+i*32)[:3] for i in range(count)]
for start,expected in [(0,(-.14,.03,.60)),(72,(0,.27,.59))]:
 points=vertices[start:start+36];center=[(min(v[k] for v in points)+max(v[k] for v in points))*.5 for k in range(3)]
 assert all(abs(a-e)<1e-6 for a,e in zip(center,expected)),('unbaked joined-object transform',center,expected)
print('PASS HuntBadge exported first blade and collar numeric centers match authored coordinates (rotation baked).',flush=True)
with tempfile.TemporaryDirectory(prefix='emberfall-tactical-enemies-') as out:
 subprocess.run([sys.executable,str(root/'ArtSource/TacticalAttachments/export_enemies.py'),str(root),out,sys.argv[1] if len(sys.argv)>1 else 'dotnet'],check=True)
