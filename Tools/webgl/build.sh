#!/bin/bash
set -euo pipefail
project=$(pwd -P)
revision=$(git rev-parse HEAD)
config=/var/lib/jenkins/.config/unity-webgl
unity_home=/var/lib/jenkins/.config/unity3d/Unity
test -f ProjectSettings/ProjectVersion.txt
test -f "$config/PhotonAppSettings.asset"
test -f "$unity_home/licenses/UnityEntitlementLicense.xml"
mkdir -p Logs Assets/Photon/Fusion/Resources
cp "$config/PhotonAppSettings.asset" Assets/Photon/Fusion/Resources/PhotonAppSettings.asset
uid=$(id -u)
gid=$(id -g)

# Restore R3 before Unity attempts to compile scripts on a fresh checkout.
docker run --rm --cpus=1 --memory=1g --user "$uid:$gid" \
    -e HOME=/tmp/dotnet-home -e DOTNET_CLI_TELEMETRY_OPTOUT=1 -e DOTNET_ROLL_FORWARD=Major \
    -v "$project:/workspace" -w /workspace \
    mcr.microsoft.com/dotnet/sdk:8.0 \
    sh -ec 'dotnet tool install NuGetForUnity.Cli --version 4.5.0 --tool-path /tmp/nuget-cli && /tmp/nuget-cli/nugetforunity restore /workspace'

# This is the actual EC2 host identity used during official activation.
# Never copy another computer's machine-id or change the license XML.
run_unity() {
docker run --rm --cpus=2 --memory=8g --memory-swap=8g \
    --user "$uid:$gid" -e HOME=/home/unity -e WEBGL_REVISION="$revision" \
    --tmpfs "/home/unity:uid=$uid,gid=$gid,mode=700" \
    --mount type=bind,src=/etc/machine-id,dst=/etc/machine-id,readonly \
    --mount "type=bind,src=$unity_home,dst=/home/unity/.config/unity3d/Unity" \
    --mount "type=bind,src=$project,dst=/workspace" -w /workspace \
    unityci/editor:ubuntu-6000.3.22f1-webgl-3@sha256:509149d9a3bf36e84ce6e2916f7de6168d3cded05502c3129fc511de6768a42a \
    unity-editor -batchmode -nographics -projectPath /workspace -buildTarget WebGL "$@"
}

run_unity -runTests -testPlatform EditMode \
    -testFilter Game.Architecture.Tests.NetworkContractTests \
    -testResults /workspace/Logs/webgl-contract-results.xml -logFile /workspace/Logs/webgl-tests.log
run_unity -quit -executeMethod Game.Editor.WebBuild.Build -logFile /workspace/Logs/webgl-build.log
test -f Builds/WebGL/index.html
test "$(cat Builds/WebGL/version.txt)" = "$revision"
