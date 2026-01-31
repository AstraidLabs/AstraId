#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)

cleanup() {
  echo "Stopping services..."
  if [[ -n "${AUTH_PID:-}" ]]; then
    kill "$AUTH_PID" 2>/dev/null || true
  fi
  if [[ -n "${API_PID:-}" ]]; then
    kill "$API_PID" 2>/dev/null || true
  fi
}

trap cleanup EXIT

echo "Starting AuthServer on https://localhost:7001"
dotnet run --project "$ROOT_DIR/src/AuthServer" --launch-profile AuthServer &
AUTH_PID=$!

echo "Starting Api on https://localhost:7002"
dotnet run --project "$ROOT_DIR/src/Api" --launch-profile Api &
API_PID=$!

echo "Starting Web on http://localhost:5173"
cd "$ROOT_DIR/src/Web"
npm install
npm run dev
