// CFRESULT001: This line would warn when the analyzer runs.
// The analyzer is included in the Clawfoot.ResultPattern NuGet package, so consumers see the warning.
// In this solution, analyzers from ProjectReference often do not run in the IDE or command-line build.
// To see the warning: pack the main project, then reference the .nupkg from another app and build.

using Clawfoot.ResultPattern;

namespace Clawfoot.ResultPattern.Tests
{
    internal static class AnalyzerVerification_DiscardedWithUsage
    {
#pragma warning disable CFRESULT001
        public static void DiscardedCallWouldWarn()
        {
            Result.Ok().WithError("discarded");
        }
#pragma warning restore CFRESULT001
    }
}
