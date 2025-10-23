@echo off
echo Distributing the Release version of MzidToTsvConverter
echo.

xcopy Release\net48\MzidToTsvConverter.exe C:\DMS_Programs\MzidToTsvConverter /Y /D /F
xcopy Release\net48\MzidToTsvConverter.pdb C:\DMS_Programs\MzidToTsvConverter /Y /D /F
xcopy Release\net48\*.dll                  C:\DMS_Programs\MzidToTsvConverter /Y /D /F
xcopy ..\..\Readme.md                      C:\DMS_Programs\MzidToTsvConverter /Y /D /F

echo.
echo Copying to \\Proto-3\DMS_Programs_Dist\AnalysisToolManagerDistribution\MzidToTsvConverter
echo.
if not "%1"=="NoPause" pause

xcopy Release\net48\MzidToTsvConverter.exe \\Proto-3\DMS_Programs_Dist\AnalysisToolManagerDistribution\MzidToTsvConverter /Y /D /F
xcopy Release\net48\MzidToTsvConverter.pdb \\Proto-3\DMS_Programs_Dist\AnalysisToolManagerDistribution\MzidToTsvConverter /Y /D /F
xcopy Release\net48\*.dll                  \\Proto-3\DMS_Programs_Dist\AnalysisToolManagerDistribution\MzidToTsvConverter /Y /D /F
xcopy ..\..\Readme.md                      \\Proto-3\DMS_Programs_Dist\AnalysisToolManagerDistribution\MzidToTsvConverter /Y /D /F
if not "%1"=="NoPause" pause
