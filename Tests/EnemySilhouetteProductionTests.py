#!/usr/bin/env python3
"""Discoverable entry point for the production F2 factory, decoder, fallback and mutation suite."""
import os,subprocess,sys,tempfile
from pathlib import Path
root=Path(__file__).resolve().parents[1]
with tempfile.TemporaryDirectory(prefix='enemy-silhouette-production-') as output:
 subprocess.run([sys.executable,str(root/'ArtSource/EnemySilhouettes/export_validate.py'),str(root),output,os.environ.get('DOTNET','dotnet')],check=True)
