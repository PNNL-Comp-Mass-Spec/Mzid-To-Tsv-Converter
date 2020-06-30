using System;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace ConverterUnitTests
{
    public class GeneRegexTests
    {
        // @"GN=([^\s|]+)"
        // @"(?<=GN=)[^\s|]+"
        // @"(?<=((sp|tr)\|[0-9a-zA-Z\-]{6,}\|)|^)([A-Z0-9]{2,})(?=_[A-Z0-9]{2,})"
        // @"(?<=(sp|tr)\|[0-9A-Z\-]{6,}\|)([A-Z0-9]{2,})(?=_[A-Z0-9]{2,})"
        // @"(?<=sp\|[0-9A-Z\-]{6,}\|)([A-Z0-9_]{2,})"
        [Test]
        [TestCase(@"(?<=sp\|[0-9A-Z\-]{6,}\|)([A-Z0-9_]{2,})", "KR2A_SHEEP")]
        [TestCase(@"(?<=(sp|tr)\|[0-9A-Z\-]{6,}\|)([A-Z0-9]{2,})(?=_[A-Z0-9]{2,})", "KR2A")]
        [TestCase(@"(?<=((sp|tr)\|[0-9a-zA-Z\-]{6,}\|)|^)([A-Z0-9]{2,})(?=_[A-Z0-9]{2,})", "KR2A")]
        [TestCase(@"(?<=(?<=(?<=sp|tr)\|[0-9a-zA-Z\-]{6,}\|)|^)([A-Z0-9]{2,})(?=_[A-Z0-9]{2,})", "KR2A")]
        public void TestRegex1(string pattern, string expected)
        {
            var input = "sp|P02438|KR2A_SHEEP some other stuff";
            var match = GetMatch(pattern, input);
            Assert.AreEqual(expected, match);

        }

        [Test]
        [TestCase(@"(?<=(sp|tr)\|[0-9A-Z\-]{6,}\|)([A-Z0-9_]{2,})", "E9PNT2_HUMAN")]
        [TestCase(@"(?<=(sp|tr)\|[0-9A-Z\-]{6,}\|)([A-Z0-9]{2,})(?=_[A-Z0-9]{2,})", "E9PNT2")]
        [TestCase(@"(?<=((sp|tr)\|[0-9a-zA-Z\-]{6,}\|)|^)([A-Z0-9]{2,})(?=_[A-Z0-9]{2,})", "E9PNT2")]
        public void TestRegex2(string pattern, string expected)
        {
            var input = "tr|E9PNT2|E9PNT2_HUMAN some other stuff";
            var match = GetMatch(pattern, input);
            Assert.AreEqual(expected, match);
        }

        [Test]
        [TestCase(@"(?<=((sp|tr)\|[0-9a-zA-Z\-]{6,}\|)|^)([A-Z0-9]{2,})(?=_[A-Z0-9]{2,})", "KR2A")]
        [TestCase(@"(?<=(?<=(?<=sp|tr)\|[0-9a-zA-Z\-]{6,}\|)|^)([A-Z0-9]{2,})(?=_[A-Z0-9]{2,})", "KR2A")]
        public void TestRegex1Short(string pattern, string expected)
        {
            var input = "KR2A_SHEEP some other stuff";
            var match = GetMatch(pattern, input);
            Assert.AreEqual(expected, match);

        }

        [Test]
        [TestCase(@"(?<=((sp|tr)\|[0-9a-zA-Z\-]{6,}\|)|^)([A-Z0-9]{2,})(?=_[A-Z0-9]{2,})", "E9PNT2")]
        public void TestRegex2Short(string pattern, string expected)
        {
            var input = "E9PNT2_HUMAN some other stuff";
            var match = GetMatch(pattern, input);
            Assert.AreEqual(expected, match);
        }

        [Test]
        [TestCase(@"GN=[^\s|]+", "GN=PNPLA8")]
        [TestCase(@"GN=([^\s|]+)", "PNPLA8")]
        [TestCase(@"(?<=GN=)[^\s|]+", "PNPLA8")]
        public void TestRegex3(string pattern, string expected)
        {
            var input = "some stuff GN=PNPLA8 some other stuff";
            var match = GetMatch(pattern, input);
            Assert.AreEqual(expected, match);
        }

        [Test]
        [TestCase(@"GN=[^\s|]+", "GN=KRTAP5-1")]
        [TestCase(@"GN=([^\s|]+)\|chr", "KRTAP5-1")]
        [TestCase(@"(?<=GN=)[^\s|]+", "KRTAP5-1")]
        public void TestRegex4(string pattern, string expected)
        {
            var input = "some stuff |GN=KRTAP5-1|chr=11| some other stuff";
            var match = GetMatch(pattern, input);
            Assert.AreEqual(expected, match);
        }

        private string GetMatch(string regexPattern, string searchString)
        {
            var regex = new Regex(regexPattern);

            var geneMatch = regex.Match(searchString);

            var result = "";

            if (geneMatch.Groups.Count > 1)
            {
                result = geneMatch.Groups[geneMatch.Groups.Count - 1].Value;
            }
            else if (geneMatch.Captures.Count > 0)
                result = geneMatch.Captures[0].Value;
            else
                result = geneMatch.Value;

            for (var i = 0; i < geneMatch.Captures.Count; i++)
            {
                Console.WriteLine("Capture {0}: \"{1}\"", i, geneMatch.Captures[i].Value);
            }

            for (var i = 0; i < geneMatch.Groups.Count; i++)
            {
                Console.WriteLine("Group {0}: \"{1}\"", i, geneMatch.Groups[i].Value);
            }

            //if (geneMatch.Success)
            //{
            //    if (geneMatch.Groups.Count > 1)
            //        result = geneMatch.Groups[1].Value;
            //    else
            //        result = geneMatch.Value;
            //}

            return result;
        }
    }
}
