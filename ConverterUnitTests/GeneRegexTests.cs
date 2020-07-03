using System;
using System.Collections.Generic;
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

            var success = MzidToTsvConverter.MzidToTsvConverter.TryGetGeneId(regex, searchString, out var geneId);
            if (!success)
            {
                if (string.IsNullOrEmpty(searchString))
                    return string.Empty;

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
