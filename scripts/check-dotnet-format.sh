#!/bin/sh
set -eu

echo "Running dotnet format check..."
dotnet format --verify-no-changes --severity error
