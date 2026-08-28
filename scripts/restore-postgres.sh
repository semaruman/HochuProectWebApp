#!/usr/bin/env bash
# Restore PostgreSQL database from a gzipped SQL dump.
# Usage: ./scripts/restore-postgres.sh path/to/backup.sql.gz
set -euo pipefail

if [ $# -lt 1 ]; then
  echo "Usage: $0 path/to/backup.sql.gz"
  exit 1
fi

BACKUP_FILE="$1"

: "${PGHOST:=localhost}"
: "${PGPORT:=5432}"
: "${PGUSER:=postgres}"
: "${PGDATABASE:=hochuproect}"

echo "Restoring ${BACKUP_FILE} into ${PGDATABASE} on ${PGHOST}:${PGPORT}"
gunzip -c "${BACKUP_FILE}" | psql -h "${PGHOST}" -p "${PGPORT}" -U "${PGUSER}" -d "${PGDATABASE}"
echo "Done."
