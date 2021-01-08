@echo off

xcopy Release\net472\MzidToTsvConverter.exe C:\DMS_Programs\MzidToTsvConverter /Y /D /F
xcopy Release\net472\MzidToTsvConverter.pdb C:\DMS_Programs\MzidToTsvConverter /Y /D /F
xcopy Release\net472\*.dll C:\DMS_Programs\MzidToTsvConverter /Y /D /F
xcopy ..\..\Readme.md C:\DMS_Programs\MzidToTsvConverter /Y /D /F

echo.
echo Copying to \\pnl\projects\OmicsSW\DMS_Programs\AnalysisToolManagerDistribution\MzidToTsvConverter
pause

xcopy Release\net472\MzidToTsvConverter.exe \\pnl\projects\OmicsSW\DMS_Programs\AnalysisToolManagerDistribution\MzidToTsvConverter /Y /D /F
xcopy Release\net472\MzidToTsvConverter.pdb \\pnl\projects\OmicsSW\DMS_Programs\AnalysisToolManagerDistribution\MzidToTsvConverter /Y /D /F
xcopy Release\net472\*.dll \\pnl\projects\OmicsSW\DMS_Programs\AnalysisToolManagerDistribution\MzidToTsvConverter /Y /D /F
xcopy ..\..\Readme.md \\pnl\projects\OmicsSW\DMS_Programs\AnalysisToolManagerDistribution\MzidToTsvConverter /Y /D /F
pause
