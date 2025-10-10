#!/usr/bin/env bash
# clean-nonios.sh
# Usage: ./clean-nonios.sh
# Cleans non-iOS projects in the solution to avoid failing on machines where iOS workload packs are not available.

set -euo pipefail

echo "Cleaning non-iOS projects..."

# Clean main library project
if [ -f "RentalHousingManagementConsole/RentalHousingManagementConsole.csproj" ]; then
  echo "Cleaning RentalHousingManagementConsole project..."
  dotnet clean "RentalHousingManagementConsole/RentalHousingManagementConsole.csproj" || echo "Warning: clean failed for RentalHousingManagementConsole"
fi

# Clean Desktop project
if [ -f "RentalHousingManagementConsole.Desktop/RentalHousingManagementConsole.Desktop.csproj" ]; then
  echo "Cleaning Desktop project..."
  dotnet clean "RentalHousingManagementConsole.Desktop/RentalHousingManagementConsole.Desktop.csproj" || echo "Warning: clean failed for Desktop"
fi

# Clean Android project (optional)
if [ -f "RentalHousingManagementConsole.Android/RentalHousingManagementConsole.Android.csproj" ]; then
  echo "Cleaning Android project..."
  dotnet clean "RentalHousingManagementConsole.Android/RentalHousingManagementConsole.Android.csproj" || echo "Warning: clean failed for Android"
fi

echo "Non-iOS clean finished."
