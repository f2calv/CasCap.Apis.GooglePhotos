using System;
using Xunit;
namespace CasCap.Apis.GooglePhotos.Tests
{
    public sealed class SkipIfAzureDevOpsBuildFact : FactAttribute
    {
        public SkipIfAzureDevOpsBuildFact()
        {
            if (IsAzureDevOps())
                Skip = "Ignore test when running in Azure DevOps";
        }

        static bool IsAzureDevOps() => Environment.GetEnvironmentVariable("TF_BUILD") != null;
    }
}