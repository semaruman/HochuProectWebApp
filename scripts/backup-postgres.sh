#!/usr/bin/env bash
# Backup PostgreSQL database for HochuProect.
# Usage: ./scripts/backup-postgres.sh [output_dir]
set -euo pipefail

OUTPUT_DIR="${1:-./backups}"
TIMESTAMP="$(date -u +%Y%m%dT%H%M%SZ)"
FILE="${OUTPUT_DIR}/hochuproect_${TIMESTAMP}.sql.gz"

: "${PGHOST:=localhost}"
: "${PGPORT:=5432}"
: "${PGUSER:=postgres}"
: "${PGDATABASE:=hochuproect}"

mkdir -p "${OUTPUT_DIR}"
echo "Creating backup: ${FILE}"
pg_dump -h "${PGHOST}" -p "${PGPORT}" -U "${PGUSER}" -d "${PGDATABASE}" --no-owner --no-acl | gzip > "${FILE}"
echo "Done."
