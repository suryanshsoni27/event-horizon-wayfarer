#!/bin/zsh
set -eu
cd "${0:A:h}/.."
mkdir -p evidence
cmake -S native -B native/build
cmake --build native/build
python3 tools/check_native.py
python3 tools/check_light.py
OPENBLAS_NUM_THREADS=2 python3 tools/train.py
OPENBLAS_NUM_THREADS=2 python3 tools/train_defense.py
OPENBLAS_NUM_THREADS=2 python3 tools/check_multi.py
python3 tools/check_photo.py
python3 tools/check_planets.py
python3 tools/check_expanse.py
'/Applications/Unity/Hub/Editor/6000.6.10f1-arm64/Unity.app/Contents/MacOS/Unity' -batchmode -nographics -projectPath "$PWD/UnityProject" -executeMethod BuildDemo.Build -quit -logFile "$PWD/evidence/unity-build-expanse.log"
