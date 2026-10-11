#!/usr/bin/env bash
# Run with: bash Tools/Export-iOS.sh. Requires macOS, Unity and its iOS Build Support.
set -euo pipefail

project_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd -P)"
editor_version="$(sed -n 's/^m_EditorVersion: //p' "$project_root/ProjectSettings/ProjectVersion.txt" | tr -d '\r')"
unity_path="${UNITY_PATH:-/Applications/Unity/Hub/Editor/$editor_version/Unity.app/Contents/MacOS/Unity}"
bundle_id="com.h644782259.emberfall.ios"
team_id=""
output=""
check_only=false
sdk="device"

usage() {
  echo 'Usage: bash Tools/Export-iOS.sh [--unity /path/to/Unity] [--bundle-id com.yourname.emberfall] [--team-id ABCDE12345] [--output Builds/iOS/MyExport] [--sdk device|simulator] [--check-only]'
}
while [[ $# -gt 0 ]]; do
  case "$1" in
    --unity|--bundle-id|--team-id|--output|--sdk)
      [[ $# -ge 2 ]] || { usage >&2; exit 2; }
      case "$1" in
        --unity) unity_path="$2" ;;
        --bundle-id) bundle_id="$2" ;;
        --team-id) team_id="$2" ;;
        --output) output="$2" ;;
        --sdk) sdk="$2" ;;
      esac
      shift 2 ;;
    --check-only) check_only=true; shift ;;
    --help|-h) usage; exit 0 ;;
    *) echo "Unknown argument: $1" >&2; usage >&2; exit 2 ;;
  esac
done

[[ "$sdk" == device || "$sdk" == simulator ]] || { echo "SDK must be device or simulator" >&2; exit 2; }
[[ "$(uname -s)" == Darwin ]] || { echo 'This script is for macOS. Use Export-iOS.ps1 for the Windows prerequisite check.' >&2; exit 2; }
[[ -x "$unity_path" ]] || { echo "Unity editor not found: $unity_path. Install $editor_version with Unity Hub, or pass --unity." >&2; exit 2; }
ios_support="$(dirname -- "$unity_path")/../PlaybackEngines/iOSSupport"
if [[ ! -d "$ios_support" ]]; then
  ios_support="$(dirname -- "$unity_path")/../../../PlaybackEngines/iOSSupport"
fi
[[ -d "$ios_support" ]] || { echo "Missing iOS Build Support for this editor. Add it in Unity Hub before exporting. No module has been installed by this script." >&2; exit 2; }
echo "Unity editor: $unity_path"
echo 'iOS Build Support: present'
if xcode-select -p >/dev/null 2>&1 && xcrun --find xcodebuild >/dev/null 2>&1; then
  echo 'Xcode command-line tools: found (device signing still requires your selected team).'
else
  echo 'Xcode is not configured. You can export source, but need full Xcode and its iOS platform support to build/run on iPhone.'
fi
if $check_only; then
  echo 'Prerequisite check only; Unity was not started and no Xcode/IPA was generated.'
  exit 0
fi

if [[ -z "$output" ]]; then
  if [[ "$sdk" == simulator ]]; then output="$project_root/Builds/iOS/Simulator";
  else output="$project_root/Builds/iOS/Xcode"; fi
fi
if [[ "$output" != /* ]]; then output="$project_root/$output"; fi
[[ ! -L "$output" ]] || { echo 'The fixed export must be a real directory, not a link.' >&2; exit 2; }
output="$(python3 -c 'import os,sys; print(os.path.realpath(sys.argv[1]))' "$output")"
case "$output" in "$project_root/Builds/iOS/"*) ;; *) echo 'Export must stay under this project Builds/iOS directory.' >&2; exit 2 ;; esac
# Reuse a saved Xcode signing team unless the caller explicitly selects another.
if [[ -z "$team_id" && -f "$output/Emberfall.xcodeproj/project.pbxproj" ]]; then
  team_id="$(python3 - "$output/Emberfall.xcodeproj/project.pbxproj" <<'PYTEAM'
import re, sys
from pathlib import Path
teams=set(re.findall(r'DEVELOPMENT_TEAM = "?([A-Z0-9]{10})"?;',Path(sys.argv[1]).read_text()))
if len(teams)>1: raise SystemExit('Multiple signing teams configured; pass --team-id explicitly.')
print(next(iter(teams),''))
PYTEAM
)"
fi
mkdir -p "$project_root/Logs" "$project_root/Builds/iOS"
staging="$(mktemp -d "$project_root/Builds/iOS/.Xcode-stage-XXXXXX")"
trap 'rm -rf -- "$staging"' EXIT
log_file="$project_root/Logs/ios-export-$sdk-latest.log"
args=(-batchmode -quit -buildTarget iOS -projectPath "$project_root" -executeMethod Emberfall.Editor.IOSBuild.Export -logFile "$log_file" -emberfallIosOutput "$staging" -emberfallIosBundleId "$bundle_id" -emberfallIosSdk "$sdk")
if [[ -n "$team_id" ]]; then args+=(-emberfallIosTeamId "$team_id"); fi
echo 'Close this project in Unity and Xcode before exporting.'
if ! python3 "$project_root/Tools/Build-Xcode/UnityExportRunner.py" "$log_file" "$unity_path" "${args[@]}"; then
  tail -n 60 "$log_file" >&2 || true
  echo "Unity export failed. The fixed project was not replaced. Log: $log_file" >&2
  exit 1
fi
[[ -f "$staging/Unity-iPhone.xcodeproj/project.pbxproj" ]] || { echo "Unity returned without a complete Xcode project. Inspect $log_file" >&2; exit 1; }
python3 - "$staging" "$output" <<'PYPUBLISH'
import errno, os, shutil, sys, time, uuid
from pathlib import Path
staging, output=map(Path,sys.argv[1:])
project=staging/'Unity-iPhone.xcodeproj'
project.rename(staging/'Emberfall.xcodeproj')
project=staging/'Emberfall.xcodeproj'
for path in project.rglob('*.xcscheme'):
    path.write_text(path.read_text().replace('Unity-iPhone.xcodeproj','Emberfall.xcodeproj'))
    if path.name=='Unity-iPhone.xcscheme': path.rename(path.with_name('Emberfall.xcscheme'))
for workspace in staging.rglob('contents.xcworkspacedata'):
    workspace.write_text(workspace.read_text().replace('Unity-iPhone.xcodeproj','Emberfall.xcodeproj'))
output.parent.mkdir(parents=True,exist_ok=True)
backup=output.with_name('.Xcode-previous-'+uuid.uuid4().hex)
if output.exists(): os.replace(output,backup)
try:
    os.replace(staging,output)
except BaseException:
    if backup.exists(): os.replace(backup,output)
    raise
if backup.exists():
    # Finder/Xcode may recreate metadata briefly in the renamed directory.
    for attempt in range(5):
        try:
            shutil.rmtree(backup)
            break
        except FileNotFoundError:
            break
        except OSError as error:
            if error.errno != errno.ENOTEMPTY or attempt == 4:
                raise
            time.sleep(0.2)
PYPUBLISH
echo "Fixed Xcode project exported: $output/Emberfall.xcodeproj"
echo 'Open Emberfall.xcodeproj, select the Emberfall scheme, your developer team and connected device, then Run.'
echo 'No historical export, signed IPA, or device installation was retained or performed.'
