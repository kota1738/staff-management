#!/usr/bin/env bash
# Ship this API to the home server, which runs aarch64 and therefore cannot use
# the x86-64 SQL Server image. The SQLite variant in docker-compose.server.yml
# is used instead.
#
#   ./deploy.sh                 # deploy to kota@server:~/staff-management
#   REMOTE=other ./deploy.sh    # different host
#
# The bind-mounted data directory is chowned to the image's APP_UID (1654) so
# the container can create the database without running as root.

set -euo pipefail

REMOTE="${REMOTE:-kota@server}"
REMOTE_DIR="${REMOTE_DIR:-staff-management}"

cd "$(dirname "$0")"

if [ ! -f .env ]; then
  echo "no .env in the working tree; the remote one will be used if present" >&2
fi

echo "packing working tree"
tar czf - \
  --exclude=.git \
  --exclude=.env \
  --exclude=data \
  --exclude=bin \
  --exclude=obj \
  --exclude='*.db' \
  . | ssh "$REMOTE" "mkdir -p \"\$HOME/$REMOTE_DIR\" && tar xzf - -C \"\$HOME/$REMOTE_DIR\""

echo "preparing the data directory"
ssh "$REMOTE" "mkdir -p \"\$HOME/$REMOTE_DIR/data\" && \
  docker run --rm -v \"\$HOME/$REMOTE_DIR/data:/data\" alpine:3 chown -R 1654:1654 /data && \
  cd \"\$HOME/$REMOTE_DIR\" && (test -f .env || cp .env.example .env)"

echo "starting on $REMOTE"
ssh "$REMOTE" "cd \"\$HOME/$REMOTE_DIR\" && docker compose -f docker-compose.server.yml up -d --build"

echo
ssh "$REMOTE" "cd \"\$HOME/$REMOTE_DIR\" && docker compose -f docker-compose.server.yml ps --format 'table {{.Name}}\t{{.Status}}'"
