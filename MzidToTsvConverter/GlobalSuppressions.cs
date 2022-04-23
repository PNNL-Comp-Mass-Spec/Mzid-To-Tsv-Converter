// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Require class instances to use this method", Scope = "member", Target = "~M:MzidToTsvConverter.MzidToTsvConverter.ConvertToTsv(System.String,System.String,MzidToTsvConverter.ConverterOptions)~System.Boolean")]
[assembly: SuppressMessage("Readability", "RCS1123:Add parentheses when necessary.", Justification = "Parentheses not needed", Scope = "member", Target = "~P:MzidToTsvConverter.PeptideMatch.PrecursorErrorPpm")]
[assembly: SuppressMessage("Usage", "RCS1146:Use conditional access.", Justification = "Leave for readability", Scope = "member", Target = "~M:MzidToTsvConverter.ConverterOptions.AssureDirectoryExists(System.IO.DirectoryInfo)")]
