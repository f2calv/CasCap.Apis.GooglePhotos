using System;
using Xunit;
namespace CasCap.Apis.GooglePhotos.Tests
{
    public sealed class SkipIfAzureDevOpsBuildTheory : TheoryAttribute
    {
        public SkipIfAzureDevOpsBuildTheory()
        {
            if (IsAzureDevOps())
                Skip = "Ignore test when running in Azure DevOps";
        }

        static bool IsAzureDevOps() => Environment.GetEnvironmentVariable("TF_BUILD") != null;
    }
}