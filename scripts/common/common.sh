#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR=$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)
STATE_DIR="$ROOT_DIR/scripts/.state"
mkdir -p "$STATE_DIR"

log() { printf '[astraid] %s\n' "$*"; }
warn() { printf '[astraid][warn] %s\n' "$*" >&2; }
err() { printf '[astraid][error] %s\n' "$*" >&2; }

load_env_file() { local file="$1"; [[ -f "$file" ]] || return 0; set -a; source "$file"; set +a; }
require_cmd() { command -v "$1" >/dev/null 2>&1 || { err "Missing dependency: $1"; return 1; }; }
ensure_env_from_example() { [[ -f "$1" ]] || { [[ -f "$2" ]] && cp "$2" "$1" && log "Created $(basename "$1") from $(basename "$2")"; }; }

terminal_kind_linux() {
  if command -v gnome-terminal >/dev/null 2>&1; then echo gnome; return; fi
  if command -v konsole >/dev/null 2>&1; then echo konsole; return; fi
  if command -v xterm >/dev/null 2>&1; then echo xterm; return; fi
  echo none
}

start_in_terminal() {
  local title="$1" script_file="$2"
  case "$(uname -s)" in
    Darwin*)
      osascript <<OSA >/dev/null
 tell application "Terminal"
   activate
   do script "bash '$script_file'"
 end tell
OSA
      ;;
    Linux*)
      case "$(terminal_kind_linux)" in
        gnome) gnome-terminal --title="$title" -- bash "$script_file" ;;
        konsole) konsole --new-tab -p tabtitle="$title" -e bash "$script_file" ;;
        xterm) xterm -T "$title" -e bash "$script_file" ;;
        *) warn "No terminal app found; running $title in current shell background."; bash "$script_file" & ;;
      esac ;;
    MINGW*|MSYS*|CYGWIN*)
      powershell.exe -NoProfile -Command "Start-Process powershell -ArgumentList '-NoExit','-ExecutionPolicy','Bypass','-File','${script_file//\//\\}'" >/dev/null 2>&1 || bash "$script_file" &
      ;;
    *) warn "Unknown OS; running $title in current shell background."; bash "$script_file" & ;;
  esac
}

create_runner_script() {
  local name="$1" env_file="$2" workdir="$3" cmd="$4" pid_file="$5"
  local runner="$STATE_DIR/run-${name}.sh"
  cat > "$runner" <<RUN
#!/usr/bin/env bash
set -euo pipefail
cd "$workdir"
set -a
[[ -f "$env_file" ]] && source "$env_file"
set +a
echo \$\$ > "$pid_file"
echo "[$name] starting: $cmd"
exec bash -lc '$cmd'
RUN
  chmod +x "$runner"
  echo "$runner"
}

stop_pid_file() {
  local pid_file="$1"
  [[ -f "$pid_file" ]] || return 0
  local pid; pid=$(cat "$pid_file" 2>/dev/null || true)
  if [[ -n "$pid" ]] && kill -0 "$pid" 2>/dev/null; then
    kill "$pid" 2>/dev/null || true; sleep 1; kill -9 "$pid" 2>/dev/null || true
    log "Stopped PID $pid"
  fi
  rm -f "$pid_file"
}
