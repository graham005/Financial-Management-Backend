#!/bin/bash
set -e

TIMESTAMP=$(date +"%Y%m%d_%H%M%S")
BACKUP_FILE="/tmp/db_backup_${TIMESTAMP}.bak"

echo "Starting SQL database backup sequence..."

# 1. Take database dump using sqlcmd tools
/opt/mssql-tools18/bin/sqlcmd \
  -S "$DB_HOST" \
  -U sa \
  -P "$DB_PASSWORD" \
  -C \
  -Q "BACKUP DATABASE [FinancialManagementDb] TO DISK='${BACKUP_FILE}' WITH FORMAT, MEDIANAME='DbBackups', NAME='Full Backup';"

echo "Backup created successfully at ${BACKUP_FILE}. Preparing GitHub upload..."

# 2. Configure git parameters using the provided GitHub Token environment variable
mkdir -p /workspace
cd /workspace

# Clone the private backup repository using the token
git clone "https://x-access-token:${GITHUB_TOKEN}@github.com/${GITHUB_REPO_PATH}.git" repo
cd repo

# Move the backup file into the repository folder
mv "$BACKUP_FILE" .

# Git commit and push back up to GitHub
git config user.name "Database Backup Bot"
git config user.email "backup-bot@render.local"
git add .
git commit -m "Automated DB Backup - ${TIMESTAMP} [skip ci]"
git push origin main

echo "Database successfully pushed to private repository: ${GITHUB_REPO_PATH}"