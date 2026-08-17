#!/bin/bash
pkill "Risk of Rain 2."

if [ -d "/run/media/icebrah/buh/gale/riskofrain2/profiles/debug/BepInEx/core/" ]; then
  steam "-applaunch" "632360" "--doorstop-enabled" "true" "--doorstop-target-assembly" "/run/media/icebrah/buh/gale/riskofrain2/profiles/debug/BepInEx/core/BepInEx.Preloader.dll"
fi

