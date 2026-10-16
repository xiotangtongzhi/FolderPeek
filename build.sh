#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")"
DOTNET="${DOTNET:-dotnet}"
"$DOTNET" build FolderPeek.csproj -c Release -o build/release -p:UseSharedCompilation=false
"$DOTNET" run --project tests/CoreTests.csproj -c Release -p:UseSharedCompilation=false
printf '\nBuilt build/release/FolderPeek.exe; keep FolderPeek.exe.config beside it.\n'
printf 'Windows GUI and Explorer COM integration require Windows runtime validation.\n'
