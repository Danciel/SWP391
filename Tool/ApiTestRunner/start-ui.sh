#!/usr/bin/env sh
cd "$(dirname "$0")" && dotnet run -- --ui "$@"
