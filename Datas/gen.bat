set WORKSPACE=..
set LUBAN_DLL=%WORKSPACE%\Luban\Luban.dll
set CONF_ROOT=.

dotnet %LUBAN_DLL% ^
    -t server ^
    -d csv ^
    --conf %CONF_ROOT%\luban.conf ^
    -x outputDataDir=csv

pause