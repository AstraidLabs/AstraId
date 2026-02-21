#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)
source "$ROOT_DIR/scripts/common/common.sh"
ENV_FILE="$ROOT_DIR/.env.prod"
PID_PREFIX="$STATE_DIR/prod"

setup() {
  require_cmd dotnet
  require_cmd curl
  ensure_env_from_example "$ROOT_DIR/.env" "$ROOT_DIR/.env.example"
  ensure_env_from_example "$ENV_FILE" "$ROOT_DIR/.env.prod.example"
  mkdir -p "$ROOT_DIR/.keys" "$ROOT_DIR/.certs" "$ROOT_DIR/artifacts"
  log "Setup complete."
}

db(){ "$ROOT_DIR/scripts/astraid-dev.sh" db; }
migrate(){ "$ROOT_DIR/scripts/astraid-dev.sh" migrate; }
seed(){ "$ROOT_DIR/scripts/astraid-dev.sh" seed; }

publish() {
  mkdir -p "$ROOT_DIR/artifacts"
  for svc in AuthServer Api AppServer; do
    if [[ -f "$ROOT_DIR/src/$svc/$svc.csproj" ]]; then
      log "Publishing $svc"
      dotnet publish "$ROOT_DIR/src/$svc/$svc.csproj" -c Release -o "$ROOT_DIR/artifacts/$svc"
    fi
  done
  if [[ -f "$ROOT_DIR/src/Web/package.json" ]]; then
    (cd "$ROOT_DIR/src/Web" && [ -d node_modules ] || npm ci; npm run build)
  fi
}

up() {
  setup; db; migrate; seed; publish
  local runner
  runner=$(create_runner_script "AuthServer-prod" "$ENV_FILE" "$ROOT_DIR" "ASPNETCORE_ENVIRONMENT=Production dotnet artifacts/AuthServer/AuthServer.dll" "$PID_PREFIX-auth.pid")
  start_in_terminal "AstraId AuthServer (prod)" "$runner"

  [[ -f "$ROOT_DIR/artifacts/Api/Api.dll" ]] && runner=$(create_runner_script "Api-prod" "$ENV_FILE" "$ROOT_DIR" "ASPNETCORE_ENVIRONMENT=Production dotnet artifacts/Api/Api.dll" "$PID_PREFIX-api.pid") && start_in_terminal "AstraId Api (prod)" "$runner"
  [[ -f "$ROOT_DIR/artifacts/AppServer/AppServer.dll" ]] && runner=$(create_runner_script "AppServer-prod" "$ENV_FILE" "$ROOT_DIR" "ASPNETCORE_ENVIRONMENT=Production dotnet artifacts/AppServer/AppServer.dll" "$PID_PREFIX-app.pid") && start_in_terminal "AstraId AppServer (prod)" "$runner"

  if [[ -f "$ROOT_DIR/src/Web/package.json" ]]; then
    runner=$(create_runner_script "Web-prod" "$ENV_FILE" "$ROOT_DIR/src/Web" "npm run preview -- --host 0.0.0.0 --port 4173" "$PID_PREFIX-web.pid")
    start_in_terminal "AstraId Web (prod preview)" "$runner"
  fi

  sleep 4
  verify
}

down(){ stop_pid_file "$PID_PREFIX-auth.pid"; stop_pid_file "$PID_PREFIX-api.pid"; stop_pid_file "$PID_PREFIX-app.pid"; stop_pid_file "$PID_PREFIX-web.pid"; }
verify(){ AUTHSERVER_ISSUER=${AUTHSERVER_ISSUER:-https://localhost:7001} API_BASE_URL=${API_BASE_URL:-https://localhost:7002} APPSERVER_BASE_URL=${APPSERVER_BASE_URL:-https://localhost:7003} WEB_BASE_URL=${WEB_BASE_URL:-http://localhost:4173} "$ROOT_DIR/scripts/astraid-dev.sh" verify; }

case "${1:-}" in
  setup) setup ;;
  db) db ;;
  migrate) migrate ;;
  seed) seed ;;
  publish) publish ;;
  up) up ;;
  down) down ;;
  verify) verify ;;
  *) echo "Usage: $0 {setup|db|migrate|seed|publish|up|down|verify}"; exit 1 ;;
esac
