# MzidToTsvConverter

## Overview

Converts a [mzIdentML](http://www.psidev.info/mzidentml) file (.mzid) created by MS-GF+ to a tab-delimited text file.

Although MS-GF+ has this option (see [MzidToTsv.html](https://msgfplus.github.io/msgfplus/MzidToTsv.html)),
MzidToTsvConverter.exe can convert the .mzid file faster, using less memory.

## Details

MzidToTsvConverter reads in the .mzid file created by MS-GF+ and creates a [tsv file](https://en.wikipedia.org/wiki/Tab-separated_values) with the peptide identifications. 
The program can also read .mzid.gz files, and it includes other options for how to handle peptides that are associated with multiple proteins.

The MzidToTsvConverter uses [PSI_Interface.dll](https://github.com/PNNL-Comp-Mass-Spec/PSI-Interface) to read the .mzid file.

## Syntax

```
MzidToTsvConverter -mzid:"mzid path" [-tsv:"tsv output path"] 
  [-unroll] [-showDecoy] [-noExtended] [-singleResult]
  [-maxSpecEValue] [-maxEValue] [-maxQValue]
  [-recurse] [-skipDupIds]
  [-geneId] [-geneIdCS]
  [-proteinList] [-delim]
```

### Required parameters:

`-mzid:path`
* Path to the .mzid or .mzid.gz file.  If the path has spaces, it must be in quotes.
* Alternatively, the path to a directory with .mzid or .mzid.gz files.  In this case, all .mzid files in the directory will be converted to .tsv

### Optional parameters:

`-tsv:path`
* Path to tab-separated values file to be written
  * Can be a filename, a directory path, or a file path
* If not specified, will be output to the same location as the mzid
* If mzid path is a directory, this will be treated as a directory path

`-unroll` or `-u`
* Signifies that results should be unrolled, giving one line per unique peptide/protein combination in each spectrum identification

`-showDecoy` or `-sd`
* Signifies that decoy results should be included in the output .tsv file
* Decoy results have protein names that start with XXX_

`-noExtended` or `-ne`
* Do not output the extended fields (e.g., Scan Time)
* When this flag is specified, the output will have the same columns as the MS-GF+ MzidToTsv output, and be a near match when run with the same parameters

`-singleResult` or `-1`
* Only output one result per spectrum

`-maxSpecEValue` or `-MaxSpecE` or `-SpecEValue`
* Filter the results, excluding those with a SpecEValue greater than this threshold

`-maxEValue`or `-MaxE` or `-EValue`
* Filter the results, excluding those with an EValue greater than this threshold

`-maxQValue` or `-MaxQ` or `-QValue`
* Filter the results, excluding those with a QValue greater than this threshold
* For example, `-qvalue:0.001`

`-recurse` or `-r`
* If mzid path is a directory, specifying this will cause .mzid files in subdirectories to also be converted

`-skipDupIds`
* If there are issues converting a file due to \"duplicate ID\" errors, specifying this will cause the duplicate IDs to be ignored, at the likely cost of some correctness

`-geneId`
* If specified, adds a 'GeneID' column to the output for non-decoy identifications
* Optionally supply a regular expression (RegEx) to extract the gene name from the protein identifier and/or the protein description
* The default expression supports the UniProt SwissProt format, extracting just the gene name and not the species
  * `-geneId:"(?<=(sp|tr)\|[0-9A-Z\-]{6,}\|)([A-Z0-9]{2,})(?=_[A-Z0-9]{2,})"`
  * For example, given `sp|P00760|TRYP_BOVIN`, the extracted gene names is `TRYP`

`-geneIdCS`
* When specified, use case-sensitive pattern matching when extracting gene names

`-proteinList`or `-proteins` or `-delimitedProteins`
* For each PSM, include a comma-separated list of proteins in the Protein column
* This setting takes precedence over `-unroll`

`-delim`
* Separator to use for the delimited protein names (defaults to a comma and space)

## Example regular expressions for -geneId

Note that many of these examples use positive lookbehind to match text before the gene name, but not capture it
* The default RegEx uses both positive lookbehind and positive lookahead

Match sp proteins, and include organism name in the gene name
* `-geneId:"(?<=sp\|[0-9A-Z\-]{6,}\|)([A-Z0-9_]{2,})"`
* Example protein name: `sp|P02438|KR2A_SHEEP`
* Extracted gene name: `KR2A_SHEEP`

Match sp proteins, but do not include organism name in the gene name 
* `-geneId:"(?<=sp\|[0-9A-Z\-]{6,}\|)([A-Z0-9]{2,})(?=_[A-Z0-9]{2,})"`
* Example protein name: `sp|P02438|KR2A_SHEEP`
* Extracted gene name: `KR2A`

Match sp or tr proteins and do not include organism name in the gene name
* `-geneId:"(?<=(sp|tr)\|[0-9A-Z\-]{6,}\|)([A-Z0-9]{2,})(?=_[A-Z0-9]{2,})"`
* Example protein name: `tr|E9PNT2|E9PNT2_HUMAN`
* Extracted gene name: `E9PNT2`

Match gene name in the protein description, as specified by `GN=GeneName`
* `-geneId:"GN=[^\s|]+"`
* Example protein name: `GN=PNPLA8`
* Extracted gene name: `GN=PNPLA8`

Match gene name in the protein description, but do not include `GN=`
* `-geneId:"(?<=GN=)[^\s|]+"`
* Example protein name: `GN=PNPLA8`
* Extracted gene name: `PNPLA8`

The previous RegEx can also match `GN=GeneName` when terms are separated by vertical bars
* `-geneId:"(?<=GN=)[^\s|]+"`
* Example protein name: `|GN=KRTAP5-1|chr=11|`
* Extracted gene name: `KRTAP5-1`


## Output Columns

The columns in the .tsv file created by the MzidToTsvConverter are:

|Column          | Description                                                             | Example               |
|----------------|-------------------------------------------------------------------------|-----------------------|
| #SpecFile      | Spectrum file name                                                      | Dataset.mzML          |
| SpecID         | Spectrum ID                                                             | controllerType=0 controllerNumber=1 scan=16231 |
| ScanNum        | Scan number                                                             | 16231                 |
| ScanTime(Min)  | (Can be disable with switch `-ne`) Scan Start time, minutes             | 52.534                |
| FragMethod     | Fragmentation method for the given MS/MS spectrum. Will be CID, ETD, or HCD. However, when spectra from the same precursor are merged, fragmentation methods of merged spectra will be shown in the form "FragMethod1/FragMethod2/..." (e.g. CID/ETD, CID/HCD/ETD). | HCD     |
| Precursor      | m/z value of the precursor ion                                          | 767.04388             |
| IsotopeError   | Isotope Error, indicating which isotope in the isotopic distribution the parent ion m/z corresponds to.  Typically 0, indicating the first isotope.  If 1, that means the second isotope was chosen for fragmentation.                                              | 0       |
| PrecursorError(ppm) | Mass Difference (in ppm) between the observed parent ion and the computed mass of the identified peptide. This value is automatically corrected if the second or third isotope is chosen for fragmentation.                                                    | -0.8753 |
| Charge         | Charge state of the parent ion                                          | 3                     |
| Peptide        | The identified peptide, with prefix and suffix residues. Also includes a numeric representation of both static and dynamic post translational  modifications.                                                                     | K.VPPAPVPC+57.021PPPS+79.966PGPSAVPSSPK.S |
| Protein        | Name of the protein this peptide comes from                             | BAG3_HUMAN            |
| GeneID         | (Enable with switch `-geneid`) Gene ID, parsed from protein description. For this example, a parameter of `-geneid "^([A-Z0-9]{2,})(?=_[A-Z0=9]{2,})"` would be used. | BAG3                  |
| DeNovoScore    | The MSGFScore of the optimal scoring peptide. Larger scores are better. | 110                   |
| MSGFScore      | This is MS-GF+'s main scoring value for the identified peptide. Larger scores are better.  | 99 |
| SpecEValue     | This is MS-GF+'s main scoring value related to peptide confidence (spectrum level e-value) of the peptide-spectrum match. MS-GF+ assumes that the peptide with the lowest SpecEValue value (closest to 0) is correct, and all others are incorrect.                 | 4.23E-21  |
| EValue         | Probability that a match with this SpecEValue is spurious; the lower this number (closer to 0), the better the match. This is a database level e-value, representing the probability that a random PSM has an equal or better score against a random database of the same size. | 9.29E-14 |
| QValue         | If MS-GF+ searches a target/decoy database, the QValue (FDR) is computed based on the distribution of SpecEValue values for forward and reverse hits. If the target/decoy search was not used, this column will be EFDR and is an estimated FDR.                    | 0         |
| PepQValue      | Peptide-level QValue (FDR) estimated using the target-decoy approach; only shown if a target/decoy search was used. If multiple spectra are matched to the same peptide, only the best-scoring match is retained and used to compute FDR.                           | 0         |

Notes on QValue and PepQValue
* QValue is defined as the minimum false discovery rate (FDR) at which the test may be called significant
  * If the value is 0, that means that no reverse hit peptides had a SpecEValue less than or equal to the current peptide's SpecEValue
* QValue is a spectrum-level FDR and is computed using the formula ReversePeptideCount ÷ ForwardPeptideCount
  * If you filter on QValue < 0.01 you are applying a 1% FDR filter
* PepQValue is a peptide-level FDR threshold, and will always be lower than QValue

## Contacts

Written by Bryson Gibbons and Matthew Monroe for the Department of Energy (PNNL, Richland, WA) \
E-mail: proteomics@pnnl.gov \
Website: https://omics.pnl.gov/ or https://panomics.pnnl.gov/

## License

The MzidToTsvConverter is licensed under the 2-Clause BSD License;
you may not use this file except in compliance with the License.  You may obtain
a copy of the License at https://opensource.org/licenses/BSD-2-Clause

Copyright 2018 Battelle Memorial Institute
