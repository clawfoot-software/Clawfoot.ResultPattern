// The analyzers run in this project (ProjectReference with OutputItemType="Analyzer").
// Removing the pragma below makes CFRESULT001 report the discarded call.
// Analyzer behaviour is tested in Clawfoot.Result.Analyzers.Tests.

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
