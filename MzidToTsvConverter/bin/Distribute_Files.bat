@echo off
echo Distributing the Release version of MzidToTsvConverter
echo.

xcopy Release\net48\MzidToTsvConverter.exe C:\DMS_Programs\MzidToTsvConverter /Y /D /F
xcopy Release\net48\MzidToTsvConverter.pdb C:\DMS_Programs\MzidToTsvConverter /Y /D /F
xcopy Release\net48\*.dll C:\DMS_Programs\MzidToTsvConverter /Y /D /F
xcopy ..\..\Readme.md C:\DMS_Programs\MzidToTsvConverter /Y /D /F

echo.
echo Copying to \\pnl\projects\OmicsSW\DMS_Programs\AnalysisToolManagerDistribution\MzidToTsvConverter
echo.
if not "%1"=="NoPause" pause

xcopy Release\net48\MzidToTsvConverter.exe \\pnl\projects\OmicsSW\DMS_Programs\AnalysisToolManagerDistribution\MzidToTsvConverter /Y /D /F
xcopy Release\net48\MzidToTsvConverter.pdb \\pnl\projects\OmicsSW\DMS_Programs\AnalysisToolManagerDistribution\MzidToTsvConverter /Y /D /F
xcopy Release\net48\*.dll \\pnl\projects\OmicsSW\DMS_Programs\AnalysisToolManagerDistribution\MzidToTsvConverter /Y /D /F
xcopy ..\..\Readme.md \\pnl\projects\OmicsSW\DMS_Programs\AnalysisToolManagerDistribution\MzidToTsvConverter /Y /D /F
if not "%1"=="NoPause" pause
