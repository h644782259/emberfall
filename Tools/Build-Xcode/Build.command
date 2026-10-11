#!/bin/bash
# Double-click in Finder, or run from any directory with bash.
set -euo pipefail
repo="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../.." && pwd -P)"
mode="${1:-all}"
case "$mode" in
  all|device|simulator) ;;
  --help|-h) echo 'Usage: bash Tools/Build-Xcode/Build.command [all|device|simulator]'; exit 0 ;;
  *) echo "Unknown mode: $mode" >&2; exit 2 ;;
esac
[[ $# -le 1 ]] || { echo 'Only one mode argument is supported.' >&2; exit 2; }
mkdir -p "$repo/Logs"
lock="$repo/Logs/xcode-export.lock"
mkdir "$lock" 2>/dev/null || { echo "Another export is running. If it was interrupted, remove $lock after confirming Unity has stopped." >&2; exit 1; }
snapshot=""
monitor_pid=""
cleanup() {
  status=$?
  trap - EXIT
  if [[ -n "$monitor_pid" ]]; then kill "$monitor_pid" 2>/dev/null || true; fi
  if [[ -n "$snapshot" && -f "$snapshot" ]]; then
    cp "$snapshot" "$repo/ProjectSettings/ProjectSettings.asset"
    rm -f "$snapshot"
  fi
  rmdir "$lock"
  exit "$status"
}
trap cleanup EXIT
trap 'exit 130' INT
trap 'exit 143' TERM
snapshot="$(mktemp "${TMPDIR:-/tmp}/emberfall-player-settings.XXXXXX")"
cp "$repo/ProjectSettings/ProjectSettings.asset" "$snapshot"
echo 'Close Unity and Xcode before exporting; do not edit Player Settings during this operation.'
echo 'Exports current local source, including uncommitted changes. No Git pull or push is performed.'
run_export() {
  local sdk="$1"
  local log="$repo/Logs/ios-export-$sdk-latest.log"
  echo "开始导出：${sdk}；详细日志：$log"
  (
    while true; do
      sleep 15
      echo "[$sdk] Unity 仍在运行；最近日志："
      if [[ -f "$log" ]]; then tail -n 1 "$log" | cut -c 1-240; fi
      if [[ -f "$log" ]] && tail -n 1 "$log" | grep -q 'Licensing\|LicenseClient'; then
        echo '正在等待授权；超时后脚本会自动恢复或给出退出原因。'
      fi
    done
  ) &
  monitor_pid=$!
  local result=0
  bash "$repo/Tools/Export-iOS.sh" --sdk "$sdk" || result=$?
  kill "$monitor_pid" 2>/dev/null || true
  wait "$monitor_pid" 2>/dev/null || true
  monitor_pid=""
  return "$result"
}
if [[ "$mode" == all || "$mode" == simulator ]]; then
  run_export simulator
fi
if [[ "$mode" == all || "$mode" == device ]]; then
  run_export device
fi
echo 'Export complete. Xcode compilation, signing and installation are separate steps.'
