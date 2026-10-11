#!/usr/bin/env python3
"""Production wolf factory, real buffers and exact quadruped pose branch; managed only."""
from pathlib import Path
import sys,tempfile,subprocess
root=Path(__file__).resolve().parents[1]
with tempfile.TemporaryDirectory(prefix='emberfall-wolf-') as tmp:
 subprocess.run([sys.executable,str(root/'ArtSource/ActorModules/export_wolf_motion.py'),str(root),tmp,sys.argv[1] if len(sys.argv)>1 else 'dotnet'],check=True)
