#!/usr/bin/env bash
# Portable, isolated equivalents of the standalone PowerShell checks.
set -euo pipefail
project_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
exec python3 "$project_root/Tools/cloud-validation.py" "$@"
