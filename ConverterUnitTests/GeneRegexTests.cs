using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;

// ReSharper disable ConvertToConstant.Local
// ReSharper disable StringLiteralTypo
namespace ConverterUnitTests
{
    public class GeneRegexTests
    {
        // Ignore Spelling: sp, Ubiquitin, carboxyl, hydrolase, Ankyrin, UniProt, SwissProt, chr, biotype, PrfC, Foreach

        [TestCase("sp|Q9NZD4|AHSP_HUMAN Alpha-hemoglobin-stabilizing protein", "AHSP")]
        [TestCase(">sp|Q9Y2K6|UBP20_HUMAN Ubiquitin carboxyl-terminal hydrolase 20 OS=Homo sapiens OX=9606 GN=USP20 PE=1 SV=2", "UBP20")]
        [TestCase("sp|P62266|RS23_HUMAN", "RS23")]
        [TestCase(">sp|P62266|RS23_HUMAN", "RS23")]
        [TestCase("sp|P62258|1433E_HUMAN", "1433E")]
        [TestCase("sp|P62258|1433E_XX", "1433E")]
        [TestCase("sp|P62258|1433E_X", "")]
        [TestCase("sp|P62258|1433E_", "")]
        [TestCase("sp|P62258|1433E", "")]
        [TestCase("sp|Q8WXK4", "")]
        [TestCase("sp|Q8WXK4 Ankyrin repeat and SOCS box protein 12 OS=Homo sapiens OX=9606 GN=ASB12 PE=1 SV=2", "")]
        [TestCase("sp|Q8WXK4|ASB12_HUMAN Ankyrin repeat and SOCS box protein 12 OS=Homo sapiens OX=9606 GN=ASB12 PE=1 SV=2", "ASB12")]
        public void TestDefaultRegEx(string input, string expected)
        {
            var match = GetMatch(MzidToTsvConverter.ConverterOptions.DefaultGeneIdRegexPattern, input, expected);
            Assert.AreEqual(expected, match);
        }

        [Test]
        [TestCase(@"(?<=sp\|[0-9A-Z\-]{6,}\|)([A-Z0-9_]{2,})", "KR2A_SHEEP")]
        [TestCase(@"(?<=(sp|tr)\|[0-9A-Z\-]{6,}\|)([A-Z0-9]{2,})(?=_[A-Z0-9]{2,})", "KR2A")]
        [TestCase(@"(?<=((sp|tr)\|[0-9a-zA-Z\-]{6,}\|)|^)([A-Z0-9]{2,})(?=_[A-Z0-9]{2,})", "KR2A")]
        [TestCase(@"(?<=(?<=(?<=sp|tr)\|[0-9a-zA-Z\-]{6,}\|)|^)([A-Z0-9]{2,})(?=_[A-Z0-9]{2,})", "KR2A")]
        [TestCase(@"(?<=(?<=(?<=sp|tr)\|[0-9A-Z\-]{6,}\|)|^)([A-Z0-9]{2,})(?=_[A-Z0-9]{2,})", "KR2A")]
        public void TestRegex1(string pattern, string expected)
        {
            var input = "sp|P02438|KR2A_SHEEP some other stuff";
            var match = GetMatch(pattern, input, expected);
            Assert.AreEqual(expected, match);
        }

        [Test]
        [TestCase(@"(?<=(sp|tr)\|[0-9A-Z\-]{6,}\|)([A-Z0-9_]{2,})", "E9PNT2_HUMAN")]
        [TestCase(@"(?<=(sp|tr)\|[0-9A-Z\-]{6,}\|)([A-Z0-9]{2,})(?=_[A-Z0-9]{2,})", "E9PNT2")]
        [TestCase(@"(?<=((sp|tr)\|[0-9a-zA-Z\-]{6,}\|)|^)([A-Z0-9]{2,})(?=_[A-Z0-9]{2,})", "E9PNT2")]
        [TestCase(@"(?<=(?<=(?<=sp|tr)\|[0-9A-Z\-]{6,}\|)|^)([A-Z0-9]{2,})(?=_[A-Z0-9]{2,})", "E9PNT2")]
        public void TestRegex2(string pattern, string expected)
        {
            var input = "tr|E9PNT2|E9PNT2_HUMAN some other stuff";
            var match = GetMatch(pattern, input, expected);
            Assert.AreEqual(expected, match);
        }

        /// <summary>
        /// These extract text up to the first underscore, but also support preceding text in the UniProt/SwissProt format
        /// </summary>
        /// <param name="pattern"></param>
        /// <param name="expected"></param>
        [Test]
        [TestCase(@"(?<=((sp|tr)\|[0-9a-zA-Z\-]{6,}\|)|^)([A-Z0-9]{2,})(?=_[A-Z0-9]{2,})", "KR2A")]
        [TestCase(@"(?<=(?<=(?<=sp|tr)\|[0-9a-zA-Z\-]{6,}\|)|^)([A-Z0-9]{2,})(?=_[A-Z0-9]{2,})", "KR2A")]
        [TestCase(@"(?<=(?<=(?<=sp|tr)\|[0-9A-Z\-]{6,}\|)|^)([A-Z0-9]{2,})(?=_[A-Z0-9]{2,})", "KR2A")]
        public void TestRegex1Short(string pattern, string expected)
        {
            var input1 = "KR2A_SHEEP some other stuff";
            var match1 = GetMatch(pattern, input1, expected);
            Assert.AreEqual(expected, match1);

            Console.WriteLine();
            var input2 = "sp|P02438|KR2A_SHEEP some other stuff";
            var match2 = GetMatch(pattern, input2, expected);
            Assert.AreEqual(expected, match2);
        }

        /// <summary>
        /// These extract text up to the first underscore, but also support preceding text in the UniProt/SwissProt format
        /// </summary>
        /// <param name="pattern"></param>
        /// <param name="expected"></param>
        [Test]
        [TestCase(@"(?<=((sp|tr)\|[0-9a-zA-Z\-]{6,}\|)|^)([A-Z0-9]{2,})(?=_[A-Z0-9]{2,})", "E9PNT2")]
        public void TestRegex2Short(string pattern, string expected)
        {
            var input1 = "E9PNT2_HUMAN some other stuff";
            var match1 = GetMatch(pattern, input1, expected);
            Assert.AreEqual(expected, match1);

            Console.WriteLine();
            var input2 = "tr|E9PNT2|E9PNT2_HUMAN some other stuff";
            var match2 = GetMatch(pattern, input2, expected);
            Assert.AreEqual(expected, match2);
        }

        /// <summary>
        /// These demonstrate matching GN=GeneName when that is followed by a space
        /// </summary>
        /// <param name="pattern"></param>
        /// <param name="expected"></param>
        [Test]
        [TestCase(@"GN=[^\s|]+", "GN=PNPLA8")]
        [TestCase(@"GN=([^\s|]+)", "PNPLA8")]
        [TestCase(@"(?<=GN=)[^\s|]+", "PNPLA8")]
        public void TestRegex3(string pattern, string expected)
        {
            var input = "some stuff GN=PNPLA8 some other stuff";
            var match = GetMatch(pattern, input, expected);
            Assert.AreEqual(expected, match);
        }

        /// <summary>
        /// These demonstrate matching GN=GeneName when that is followed by a vertical bar
        /// </summary>
        /// <param name="pattern"></param>
        /// <param name="expected"></param>
        [Test]
        [TestCase(@"GN=[^\s|]+", "GN=KRTAP5-1")]
        [TestCase(@"GN=([^\s|]+)\|chr", "KRTAP5-1")]
        [TestCase(@"GN=([^\s]+)\|", "KRTAP5-1|chr=11")]
        [TestCase(@"(?<=GN=)[^\s|]+", "KRTAP5-1")]
        public void TestRegex4(string pattern, string expected)
        {
            var input = "some stuff |GN=KRTAP5-1|chr=11| some other stuff";
            var match = GetMatch(pattern, input, expected);
            Assert.AreEqual(expected, match);
        }

        /// <summary>
        /// These demonstrate matching gene:GeneName in the protein description
        /// </summary>
        /// <param name="pattern"></param>
        /// <param name="expected"></param>
        [Test]
        [TestCase("gene:ENS[^ ]+", "gene:ENSG00000072506")]
        [TestCase("gene:(ENS[^ ]+)", "ENSG00000072506")]
        [TestCase(@"gene:([^\s|]+)", "ENSG00000072506")]
        [TestCase(@"(?<=gene:)[^\s|]+", "ENSG00000072506")]
        public void TestRegex5(string pattern, string expected)
        {
            var input = "ENSP00000364453 pep:known chromosome:GRCh37:X:53458206:53461303:-1 gene:ENSG00000072506 transcript:ENST00000375304 gene_biotype:protein_coding transcript_biotype:protein_coding";
            var match = GetMatch(pattern, input, expected);
            Assert.AreEqual(expected, match);
        }

        /// <summary>
        /// These demonstrate how to extract either the entire description after a protein name, or just part of the description
        /// </summary>
        /// <param name="pattern"></param>
        /// <param name="expected"></param>
        [Test]
        [TestCase(@"[^\s]+ (.+)", "peptide chain release factor 3, PrfC")]
        [TestCase(".+, (.+)", "PrfC")]
        public void TestRegex6(string pattern, string expected)
        {
            var input = "SO_1211 peptide chain release factor 3, PrfC";
            var match = GetMatch(pattern, input, expected);
            Assert.AreEqual(expected, match);
        }

        private string GetMatch(string regexPattern, string searchString, string expectedMatch)
        {
            var regex = new Regex(regexPattern, RegexOptions.IgnoreCase);

            var success = MzidToTsvConverter.MzidToTsvConverter.TryGetGeneId(regex, searchString, out var geneId);
            if (!success)
            {
                if (string.IsNullOrEmpty(expectedMatch))
                {
                    // We expected there to not be a match; that's OK
                    Console.WriteLine("RegEx \n  {0}\ndid not match \n  {1}\n \nThis was expected",
                        regexPattern, searchString);

                    return string.Empty;
                }

                Assert.Fail("RegEx '{0}' did not match '{1}'", regexPattern, searchString);
            }

            var geneMatch = regex.Match(searchString);

            string result;
            string matchType;

            if (geneMatch.Groups.Count > 1)
            {
                matchType = "group";
                result = geneMatch.Groups[geneMatch.Groups.Count - 1].Value;
            }
            else if (geneMatch.Captures.Count > 0)
            {
                matchType = "capture";
                result = geneMatch.Captures[0].Value;
            }
            else
            {
                matchType = "full match";
                result = geneMatch.Value;
            }

            Assert.AreEqual(result, geneId, "geneId returned by TryGetGeneId does not match local code");

            Console.WriteLine("RegEx \n  {0}\nmatched to \n  {1}\ngives \n  {2}\nvia a {3}",
                regexPattern, searchString, result, matchType);

            Console.WriteLine();

            if (geneMatch.Captures.Count == 0 && geneMatch.Groups.Count == 0)
            {
                Console.WriteLine("There were no captures or groups");
                return result;
            }

            // Cache the groups and captures so that we can display them in a table
            var groupsByLine = new Dictionary<int, string>();
            var capturesByLine = new Dictionary<int, string>();

            if (geneMatch.Groups.Count > 0)
            {
                for (var i = 0; i < geneMatch.Groups.Count; i++)
                {
                    groupsByLine.Add(i, geneMatch.Groups[i].Value);
                }
            }

            if (geneMatch.Captures.Count > 0)
            {
                // Display captures and groups
                for (var i = 0; i < geneMatch.Captures.Count; i++)
                {
                    capturesByLine.Add(i, geneMatch.Captures[i].Value);
                }
            }

            var targetWidth = Math.Max(GetMaxWidth(groupsByLine), GetMaxWidth(capturesByLine)) + 2;

            Console.WriteLine("{0,-6} {1} {2}",
                "Index",
                "Group".PadRight(targetWidth),
                "Capture".PadRight(targetWidth));

            for (var i = 0; i < Math.Max(groupsByLine.Count, capturesByLine.Count); i++)
            {
                var groupValue = GetValueByKey(groupsByLine, i);
                var captureValue = GetValueByKey(capturesByLine, i);

                Console.WriteLine("{0,-6} {1} {2}",
                    i,
                    groupValue.PadRight(targetWidth),
                    captureValue.PadRight(targetWidth));
            }

            return result;
        }

        private int GetMaxWidth(Dictionary<int, string> valuesByLine)
        {
            var maxWidth = 0;

            // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
            foreach (var item in valuesByLine.Values)
            {
                if (string.IsNullOrEmpty(item))
                    continue;

                if (item.Length > maxWidth)
                    maxWidth = item.Length;
            }

            return maxWidth;
        }

        private string GetValueByKey(IReadOnlyDictionary<int, string> valuesByLine, int lineNumber)
        {
            if (valuesByLine.TryGetValue(lineNumber, out var value))
                return value;

            return string.Empty;
        }
    }
}
