@echo off
setlocal
echo ===============================================
echo  DESINSTALANDO SERVICIO CBF ALERTAS COMPRAS
echo ===============================================
set SERVICE_NAME=CBFAlertasCompras

sc stop "%SERVICE_NAME%"
sc delete "%SERVICE_NAME%"

echo.
echo ✅ Servicio desinstalado correctamente.
pause
