#!/bin/sh
set -e

# Ensure KYC storage directory exists with proper ownership
mkdir -p /app/App_Data/kyc
chown -R "$APP_UID:$APP_UID" /app/App_Data

# Drop privileges and run the application
exec su -s /bin/sh -c "exec dotnet CasinoRoyale.dll" "$(getent passwd "$APP_UID" | cut -d: -f1)"
