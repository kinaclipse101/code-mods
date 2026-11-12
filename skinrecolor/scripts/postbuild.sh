#!/bin/bash

buildDir="$1""$3"/bin/"$4"/"$2"/netstandard2.1/
pcDebugDir="/run/media/icebrah/buh/gale/riskofrain2/profiles/debug/BepInEx/plugins/"
pcDebugDirClient="/run/media/icebrah/buh/gale/riskofrain2/profiles/debugclient/BepInEx/plugins/"
laptopDebugDir="/home/kina/.local/share/com.kesomannen.gale/riskofrain2/profiles/debug/BepInEx/plugins/"
weaverDir="/home/kina/Documents/github/code-mods/weaver/NetworkWeaver/"

runBuild () {
  if ! [ -d "$1$3" ]; then
    mkdir "$1$3"
  fi
  
  echo "LOCATION $1$3/""$3.dll"
  
  cp "$buildDir""$3.dll" "$1$3/""$3.dll"
  cp "$buildDir""$3.pdb" "$1$3/""$3.pdb"
  
  if [[ "$4" == "Weaver" ]]; then 
    echo wine "$weaverDir"Unity.UNetWeaver.exe  "$weaverDir"libs/UnityEngine.CoreModule.dll "$weaverDir"libs/com.unity.multiplayer-hlapi.Runtime.dll "$1$3" "$1$3/""$3.dll" "$weaverDir"/libs/
    wine "$weaverDir"Unity.UNetWeaver.exe  "$weaverDir"libs/UnityEngine.CoreModule.dll "$weaverDir"libs/com.unity.multiplayer-hlapi.Runtime.dll "$1$3" "$1$3/""$3.dll" "$weaverDir"/libs/
  fi
}

if [ -d "$pcDebugDir" ]; then
  runBuild "$pcDebugDir" "$2" "$3" "$4"
fi

if [ -d "$pcDebugDirClient" ]; then
  runBuild "$pcDebugDirClient" "$2" "$3" "$4"
fi

if [ -d "$laptopDebugDir" ]; then
  runBuild "$laptopDebugDir" "$2" "$3" "$4"
fi
