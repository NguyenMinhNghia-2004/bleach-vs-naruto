#!/usr/bin/env bash
# Fast type-check of the BVN C# sources with Unity's bundled Roslyn (seconds instead of a Unity import).
# Builds BVN.Runtime then BVN.Game (Assets/BVN/Game) against it.
# Usage: tools/fastcompile.sh [runtime|game|all] [grep-filter]
#   prints compiler errors (optionally only lines matching grep-filter); exit 0 when the selected assemblies are clean.
E="/d/du lieu app/Unity Hub/6000.3.10f1/Editor/Data"
ASSETS="/d/Na vs Ble/BVN-Unity/Assets/BVN"
OUT="${FASTCOMPILE_OUT:-/d/Na vs Ble/work/fastcompile}"
MODE="${1:-all}"
FILTER="${2:-}"
mkdir -p "$OUT"
w() { cygpath -w "$1"; }

refs() {
  echo "-r:\"$(w "$E/NetStandard/ref/2.1.0/netstandard.dll")\""
  for m in UnityEngine UnityEngine.CoreModule UnityEngine.AudioModule UnityEngine.TextRenderingModule UnityEngine.IMGUIModule \
           UnityEngine.InputLegacyModule UnityEngine.ScreenCaptureModule UnityEngine.ImageConversionModule UnityEngine.JSONSerializeModule UnityEngine.UnityWebRequestModule; do
    echo "-r:\"$(w "$E/Managed/UnityEngine/$m.dll")\""
  done
}

build() { # name srcdir extra-refs...
  local name="$1" src="$2"; shift 2
  local rsp="$OUT/$name.rsp"
  {
    echo "-nologo -target:library -nostdlib+ -langversion:9.0 -nowarn:0162,0168,0219,0414,0649,0169,0067,0108,0114,0109,0659,0661,0660,1717"
    echo "-out:\"$(w "$OUT")\\$name.dll\""
    refs
    for r in "$@"; do echo "-r:\"$(w "$r")\""; done
    find "$src" -name '*.cs' | while read -r f; do echo "\"$(w "$f")\""; done
  } > "$rsp"
  "$E/NetCoreRuntime/dotnet.exe" "$E/DotNetSdkRoslyn/csc.dll" "@$(w "$rsp")" > "$OUT/$name.log" 2>&1
  local code=$?
  grep -E "error CS" "$OUT/$name.log" | sed 's#^.*\\Assets\\BVN\\#BVN\\#' | sort -u > "$OUT/$name.errors"
  echo "== $name: $(wc -l < "$OUT/$name.errors") error lines (exit $code)"
  if [ -n "$FILTER" ]; then grep -E "$FILTER" "$OUT/$name.errors" | head -300; else head -300 "$OUT/$name.errors"; fi
  return $code
}

rc=0
if [ "$MODE" = runtime ] || [ "$MODE" = all ] || [ "$MODE" = game ]; then
  build BVN.Runtime "$ASSETS/Runtime" || rc=1
fi
if [ "$MODE" = game ] || [ "$MODE" = all ]; then
  build BVN.Game "$ASSETS/Game" "$OUT/BVN.Runtime.dll" || rc=1
fi
exit $rc
