set OutputPath=_build_output

if exist %OutputPath% rd /s /q %OutputPath%
md %OutputPath%

MSBuild SQLUpdate.sln -t:Clean -p:Configuration=Release
MSBuild SQLUpdate.sln -p:Configuration=Release

xcopy /s .\SQLUpdate\bin\Release %OutputPath%\x64\*.*
xcopy /s .\SCQueryConnectx86\bin\Release %OutputPath%\x86\*.*
