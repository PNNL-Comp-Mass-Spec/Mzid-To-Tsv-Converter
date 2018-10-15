# MzidToTsvConverter

## Overview
Converts an .mzid file created by MS-GF+ to a tab-delimited text file.

Although MS-GF+ has this option (see [MzidToTsv.html](http://htmlpreview.github.io/?https://github.com/sangtaekim/msgfplus/blob/master/doc/MzidToTsv.html) ),
MzidToTsvConverter.exe can convert the .mzid file faster, using less memory.

## Details

MzidToTsvConverter reads in the mzid from MS-GF+ and creates a nearly identical tsv file as the MS-GF+ converter -- nearly identical because the number formatting is slightly different.

MzidToTsvConverter uses PSI_Interface.dll to read the mzid file.

## Syntax

`MzidToTsvConverter -mzid:"mzid path" [-tsv:"tsv output path"] [-unroll|-u] [-showDecoy|-sd]`

### Required parameters:
`-mzid:path` 
* Path to the .mzid or .mzid.gz file.  If the path has spaces, it must be in quotes.

### Optional parameters:
`-tsv:path`
* Path to the tsv file to be written. If not specified, will be created in the same location as the .mzid file.

`-unroll` or `-u`
* Signifies that results should be unrolled: one line per unique peptide/protein combination in each spectrum identification

`-showDecoy` or `-sd`
* Signifies that decoy results should be included in the output .tsv file.
* Decoy results have protein names that start with XXX_

## Contacts

Written by Bryson Gibbons and Matthew Monroe for the Department of Energy (PNNL, Richland, WA) \
E-mail: proteomics@pnnl.gov \
Website: https://omics.pnl.gov/ or https://panomics.pnnl.gov/

## License

The MzidToTsvConverter is licensed under the 2-Clause BSD License; 
you may not use this file except in compliance with the License.  You may obtain 
a copy of the License at https://opensource.org/licenses/BSD-2-Clause

Copyright 2018 Battelle Memorial Institute
