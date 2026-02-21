#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)
# shellcheck source=scripts/common/common.sh
source "$ROOT_DIR/scripts/common/common.sh"
ENV_FILE="$ROOT_DIR/.env.dev"
PID_PREFIX="$STATE_DIR/dev"

setup() {
  require_cmd dotnet
  require_cmd curl
  ensure_env_from_example "$ROOT_DIR/.env" "$ROOT_DIR/.env.example"
  ensure_env_from_example "$ENV_FILE" "$ROOT_DIR/.env.dev.example"
  mkdir -p "$ROOT_DIR/.keys" "$ROOT_DIR/.certs" "$ROOT_DIR/artifacts"
  if [[ -d "$ROOT_DIR/src/Web" && ! -d "$ROOT_DIR/src/Web/node_modules" ]]; then
    (cd "$ROOT_DIR/src/Web" && npm ci)
  fi
  log "Setup complete."
}

db() {
  if ! command -v psql >/dev/null 2>&1; then
    warn "psql not found. Run scripts in db/*.sql manually."; return 0
  fi
  load_env_file "$ENV_FILE"
  local admin_db="${PGADMIN_DB:-postgres}"
  local host="${PGHOST:-localhost}" port="${PGPORT:-5432}" user="${PGSUPERUSER:-postgres}"
  for sql in "$ROOT_DIR"/db/*.sql; do
    log "Running $(basename "$sql")"
    psql "host=$host port=$port dbname=$admin_db user=$user" -v ON_ERROR_STOP=1 -f "$sql"
  done
}

migrate() {
  if ! command -v dotnet-ef >/dev/null 2>&1; then
    warn "dotnet-ef not installed. Install with: dotnet tool install --global dotnet-ef"; return 0
  fi
  local migrated=0
  while IFS= read -r proj; do
    local dir; dir=$(dirname "$proj")
    if [[ -d "$dir/Migrations" ]]; then
      load_env_file "$ENV_FILE"
      log "Migrating $proj"
      dotnet ef database update --project "$proj" --startup-project "$proj"
      migrated=1
    fi
  done < <(find "$ROOT_DIR/src" -name '*.csproj')
  [[ $migrated -eq 1 ]] || log "No EF migration projects found."
}

seed() {
  if rg -n "AuthBootstrapHostedService" "$ROOT_DIR/src/AuthServer" >/dev/null 2>&1; then
    log "Seeding is executed by AuthServer hosted service on startup."
  else
    log "No explicit seeding entry point found."
  fi
}

up() {
  setup; db; migrate; seed
  load_env_file "$ENV_FILE"

  local auth_pid="$PID_PREFIX-auth.pid" api_pid="$PID_PREFIX-api.pid" app_pid="$PID_PREFIX-app.pid" web_pid="$PID_PREFIX-web.pid"
  local runner

  runner=$(create_runner_script "AuthServer-dev" "$ENV_FILE" "$ROOT_DIR" "dotnet watch run --project src/AuthServer --launch-profile AuthServer" "$auth_pid")
  start_in_terminal "AstraId AuthServer (dev)" "$runner"

  if [[ -f "$ROOT_DIR/src/Api/Api.csproj" ]]; then
    runner=$(create_runner_script "Api-dev" "$ENV_FILE" "$ROOT_DIR" "dotnet watch run --project src/Api --launch-profile Api" "$api_pid")
    start_in_terminal "AstraId Api (dev)" "$runner"
  fi

  if [[ -f "$ROOT_DIR/src/AppServer/AppServer.csproj" ]]; then
    runner=$(create_runner_script "AppServer-dev" "$ENV_FILE" "$ROOT_DIR" "dotnet watch run --project src/AppServer --launch-profile AppServer" "$app_pid")
    start_in_terminal "AstraId AppServer (dev)" "$runner"
  fi

  if [[ -f "$ROOT_DIR/src/Web/package.json" ]]; then
    runner=$(create_runner_script "Web-dev" "$ENV_FILE" "$ROOT_DIR/src/Web" "[ -d node_modules ] || npm ci; npm run dev" "$web_pid")
    start_in_terminal "AstraId Web (dev)" "$runner"
  fi

  sleep 4
  verify
}

down() {
  stop_pid_file "$PID_PREFIX-auth.pid"
  stop_pid_file "$PID_PREFIX-api.pid"
  stop_pid_file "$PID_PREFIX-app.pid"
  stop_pid_file "$PID_PREFIX-web.pid"
  log "Down complete (best effort)."
}

verify() {
  load_env_file "$ENV_FILE"
  local issuer="${AUTHSERVER_ISSUER:-https://localhost:7001}"
  issuer="${issuer%/}"
  local api_url="${API_BASE_URL:-https://localhost:7002}"
  local app_url="${APPSERVER_BASE_URL:-https://localhost:7003}"
  local web_url="${WEB_BASE_URL:-http://localhost:5173}"
  echo "Issuer discovery: $issuer/.well-known/openid-configuration"
  curl -kfsS "$issuer/.well-known/openid-configuration" >/dev/null && echo "  OK" || echo "  FAILED"
  echo "API health: $api_url/health"; curl -kfsS "$api_url/health" >/dev/null && echo "  OK" || echo "  FAILED"
  echo "AppServer health: $app_url/health"; curl -kfsS "$app_url/health" >/dev/null && echo "  OK" || echo "  FAILED"
  echo "Web: $web_url"
}

case "${1:-}" in
  setup) setup ;;
  db) db ;;
  migrate) migrate ;;
  seed) seed ;;
  up) up ;;
  down) down ;;
  verify) verify ;;
  *) echo "Usage: $0 {setup|db|migrate|seed|up|down|verify}"; exit 1 ;;
esac
