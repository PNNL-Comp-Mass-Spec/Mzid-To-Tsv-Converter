@echo off

Set ProgramPath=MzidToTsvConverter.exe
If Exist ..\MzidToTsvConverter.exe                                       Set ProgramPath=..\MzidToTsvConverter.exe
If Exist ..\MzidToTsvConverter\bin\Release\net472\MzidToTsvConverter.exe Set ProgramPath=..\MzidToTsvConverter\bin\Release\net472\MzidToTsvConverter.exe
If Exist net472\MzidToTsvConverter.exe                                   Set ProgramPath=net472\MzidToTsvConverter.exe

echo.
echo -----------------------------------------------------------------
echo Option 1: do not unroll proteins: only the first protein for each PSM will be shown
echo -----------------------------------------------------------------
@echo on
%ProgramPath% QC_Mam_19_01_1a_25Feb21_Rage_Rep-21-01-02_msgfplus.mzid.gz
@echo off

echo.
echo -----------------------------------------------------------------
echo Option 2: unroll proteins: for PSMs with multiple proteins, write one line per protein
echo -----------------------------------------------------------------
@echo on
%ProgramPath% QC_Mam_19_01_1a_25Feb21_Rage_Rep-21-01-02_msgfplus.mzid.gz -unroll -tsv QC_Mam_19_01_1a_25Feb21_Rage_Rep-21-01-02_unrolled_msgfplus.tsv
@echo off

echo.
echo -----------------------------------------------------------------
echo Option 3: for PSMs with multiple proteins, combine protein names into a comma separated list
echo -----------------------------------------------------------------
@echo on
%ProgramPath% QC_Mam_19_01_1a_25Feb21_Rage_Rep-21-01-02_msgfplus.mzid.gz -proteinList -tsv QC_Mam_19_01_1a_25Feb21_Rage_Rep-21-01-02_delimited_msgfplus.tsv
@echo off

pause
