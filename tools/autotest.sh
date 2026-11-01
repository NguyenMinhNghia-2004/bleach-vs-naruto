#!/usr/bin/env bash
# Build (optional) and run the Windows player with an AutoPilot script; prints the autopilot log.
# Usage: tools/autotest.sh <outname> "<script>" [--build] [--timeout SEC]
U="/d/du lieu app/Unity Hub/6000.3.14f1/Editor/Unity.exe"
ROOT="/d/Na vs Ble"
NAME="$1"; SCRIPT="$2"; shift 2
BUILD=0; TO=180
while [ $# -gt 0 ]; do
  case "$1" in
    --build) BUILD=1 ;;
    --timeout) TO="$2"; shift ;;
  esac
  shift
done
if [ $BUILD = 1 ]; then
  "$U" -batchmode -projectPath "D:/Na vs Ble/BVN-Unity" -executeMethod BVN.EditorTools.BuildTools.BuildWindows -logFile "D:/Na vs Ble/work/unity_build.log"
  grep -E "error CS|\[build\]" "$ROOT/work/unity_build.log" | head -30
fi
OUT="$ROOT/work/auto_$NAME"
mkdir -p "$OUT"
rm -f "$OUT"/*.png "$OUT"/autopilot.log
cd "$ROOT/Build/Windows" || exit 1
timeout "$TO" ./BleachVsNaruto.exe -screen-width 800 -screen-height 600 -screen-fullscreen 0 \
  -mute -logFile "D:/Na vs Ble/work/auto_$NAME/player.log" -bvnauto "$SCRIPT" -bvnout "D:/Na vs Ble/work/auto_$NAME"
echo "exit=$?"
cat "$OUT/autopilot.log" 2>/dev/null | cut -c1-400 | head -120
