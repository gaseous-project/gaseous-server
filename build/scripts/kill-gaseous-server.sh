#!/usr/bin/env bash
set -euo pipefail

# Kill only processes whose command line contains the exact token 'gaseous-server'.
# 1) Find matching PIDs from ps output (no other patterns searched)
# 2) Try SIGTERM with brief waits
# 3) SIGKILL if still present

# Collect candidate PIDs strictly by appearance of 'gaseous-server' in the command line
mapfile -t pids < <(ps -eo pid=,args= | grep -F "gaseous-server" | grep -v "grep" | awk '{print $1}' | sort -u)

if [[ ${#pids[@]} -eq 0 ]]; then
  echo "No gaseous-server processes found."
  exit 0
fi

echo "Found gaseous-server PIDs: ${pids[*]}"
kill -TERM "${pids[@]}" || true

# Wait up to 5 seconds for graceful shutdown
for i in {1..5}; do
  sleep 1
  mapfile -t still < <(ps -eo pid=,args= | grep -F "gaseous-server" | grep -v "grep" | awk '{print $1}' | sort -u)
  if [[ ${#still[@]} -eq 0 ]]; then
    echo "gaseous-server terminated gracefully."
    exit 0
  fi
  echo "Waiting for shutdown... ($i)"
done

# Force kill any remaining
mapfile -t still < <(ps -eo pid=,args= | grep -F "gaseous-server" | grep -v "grep" | awk '{print $1}' | sort -u)
if [[ ${#still[@]} -gt 0 ]]; then
  echo "Force killing remaining gaseous-server PIDs: ${still[*]}"
  kill -KILL "${still[@]}" || true
fi

echo "Done."
