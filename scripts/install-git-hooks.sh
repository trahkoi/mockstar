#!/bin/sh
set -eu

git config --local core.hooksPath githooks
chmod +x githooks/pre-commit scripts/check-dotnet-format.sh

echo "Configured Git hooks path to 'githooks'."
echo "Pre-commit will run dotnet format --verify-no-changes --severity error."
