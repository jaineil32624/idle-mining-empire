#!/usr/bin/env bash
# Compiles every C# script against the real Unity engine/editor assemblies inside a GameCI
# Unity editor image. No Unity license is needed because Unity itself is never launched;
# only its bundled C# compiler and reference DLLs are used.
set -uo pipefail
note(){ echo "::error title=ci::$*"; }

PROJECT="${1:-/project}"
OUT="$PROJECT/.ci/out"
mkdir -p "$OUT"

EDITOR_DATA="$(dirname "$(readlink -f "$(command -v unity-editor 2>/dev/null || echo /opt/unity/Editor/Unity)")")/Data"
[ -d "$EDITOR_DATA" ] || EDITOR_DATA=/opt/unity/Editor/Data
echo "Editor data: $EDITOR_DATA"

DOTNET="$(find "$EDITOR_DATA" -maxdepth 3 -type f -name dotnet | head -n1)"
CSC="$(find "$EDITOR_DATA" -type f -name csc.dll -path '*Roslyn*' | head -n1)"
echo "dotnet=$DOTNET csc=$CSC"

MANAGED="$EDITOR_DATA/Managed"
NETSTD="$(find "$EDITOR_DATA/NetStandard" -type f -path '*ref/2.1.0/netstandard.dll' | head -n1)"
UGUI="$(find "$EDITOR_DATA" -type d -path '*BuiltInPackages/com.unity.ugui' | head -n1)"
echo "netstandard=$NETSTD ugui=$UGUI"; [ -d "$UGUI/Runtime" ] || echo "::error title=RESULT ugui-layout::$(find "$UGUI" -maxdepth 2 | head -40 | tr '\n' ' ')"

DEFINES="UNITY_6000_0_OR_NEWER;UNITY_2023_1_OR_NEWER;UNITY_2022_3_OR_NEWER;UNITY_2021_3_OR_NEWER;UNITY_2020_3_OR_NEWER;UNITY_2019_4_OR_NEWER;UNITY_5_6_OR_NEWER;UNITY_5_3_OR_NEWER;UNITY_64;UNITY_STANDALONE;UNITY_STANDALONE_LINUX;PLATFORM_STANDALONE;NET_STANDARD_2_0;NET_STANDARD_2_1;NET_STANDARD;CSHARP_7_3_OR_NEWER;ENABLE_UNITYEVENTS;ENABLE_LEGACY_INPUT_MANAGER;PACKAGE_PHYSICS2D;PACKAGE_PHYSICS;PACKAGE_ANIMATION;PACKAGE_TILEMAP"

engine_refs() {
  for f in "$MANAGED"/UnityEngine/*.dll; do
    case "$(basename "$f")" in UnityEditor*) continue;; esac
    echo "-r:$f"
  done
  echo "-r:$NETSTD"
}

editor_refs() {
  for f in "$MANAGED"/UnityEngine/*.dll; do echo "-r:$f"; done
  [ -f "$MANAGED/UnityEditor.dll" ] && echo "-r:$MANAGED/UnityEditor.dll"
  echo "-r:$NETSTD"
}

compile() { # name defines refs-file sources-file
  local name="$1" defines="$2" refs="$3" srcs="$4"
  echo "::group::Compile $name ($(wc -l < "$srcs") files)"
  set +e
  "$DOTNET" "$CSC" -nologo -noconfig -nostdlib+ -target:library -langversion:9.0 -nowarn:0169,0649,0414,0618,0672,1701,1702 \
    -define:"$defines" -out:"$OUT/$name.dll" @"$refs" @"$srcs" > "$OUT/$name.log" 2>&1
  local rc=$?
  set -e
  if [ $rc -ne 0 ]; then
    local body
    body="$( (grep -E 'error' "$OUT/$name.log" || head -c 3000 "$OUT/$name.log") | sed "s#$PROJECT/##" | sort -u | head -n 60 | cut -c1-260 | sed ':a;N;$!ba;s/\n/%0A/g')"
    echo "::error title=RESULT $name rc=$rc::$body"
  fi
  echo "::endgroup::"
  if [ $rc -ne 0 ]; then note "FAILED: $name"; exit 1; fi
  echo "OK: $name"
}

# 1) uGUI (UnityEngine.UI) from the editor's built-in package
engine_refs > "$OUT/engine.rsp"
find "$UGUI/Runtime" -name '*.cs' -not -path '*/TMP/*' -not -path '*/Editor/*' -not -path '*/Tests/*' > "$OUT/ugui.src"
compile UnityEngine.UI "$DEFINES" "$OUT/engine.rsp" "$OUT/ugui.src"
echo "::error title=RESULT ugui-info::files=$(wc -l < "$OUT/ugui.src") dll=$(stat -c %s "$OUT/UnityEngine.UI.dll" 2>/dev/null) sample=$(head -3 "$OUT/ugui.src" | tr '\n' ' ') asmdefs=$(find "$UGUI" -name '*.asmdef' | sed "s#$UGUI/##" | tr '\n' ' ') grepdef=$(grep -rhoE '^#if [A-Z_]+' "$UGUI/Runtime" --include=*.cs | sort | uniq -c | sort -rn | head -5 | tr '\n' ' ')"

# 2) Game runtime scripts (Assembly-CSharp)
{ cat "$OUT/engine.rsp"; echo "-r:$OUT/UnityEngine.UI.dll"; } > "$OUT/game.rsp"
find "$PROJECT/Assets" -name '*.cs' -not -path '*/Editor/*' > "$OUT/game.src"
compile Assembly-CSharp "$DEFINES" "$OUT/game.rsp" "$OUT/game.src"

# 3) Editor scripts (Assembly-CSharp-Editor)
{ editor_refs; echo "-r:$OUT/UnityEngine.UI.dll"; echo "-r:$OUT/Assembly-CSharp.dll"; } > "$OUT/editor.rsp"
find "$PROJECT/Assets" -name '*.cs' -path '*/Editor/*' > "$OUT/editor.src"
compile Assembly-CSharp-Editor "$DEFINES;UNITY_EDITOR" "$OUT/editor.rsp" "$OUT/editor.src"

echo "All assemblies compiled successfully."
