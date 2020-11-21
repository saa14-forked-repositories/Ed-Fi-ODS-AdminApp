// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Configuration;
using System.Threading.Tasks;
#if NET48
using System.Web.Mvc;
#else
using Microsoft.AspNetCore.Mvc;
#endif
using EdFi.Ods.AdminApp.Management.Api;
using EdFi.Ods.AdminApp.Management.Helpers;
using EdFi.Ods.AdminApp.Management.Instances;
using EdFi.Ods.AdminApp.Web;
using EdFi.Ods.AdminApp.Web.Infrastructure.Jobs;
using EdFi.Ods.AdminApp.Web.Models.ViewModels;
using EdFi.Ods.AdminApp.Web.Models.ViewModels.OdsInstanceSettings;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApp.Management.Tests.Controllers.OdsInstanceSettingsController
{
    [TestFixture]
    public class WhenRunningTheLearningStandardsSync
    {
        [TestFixture]
        public class GivenNotUsingYearSpecificMode : OdsInstanceSettingsControllerFixture
        {
            private const string ProductionUrl = "http://example.com";
            private const string ApiKey = "key";
            private const string ApiSecret = "secret";
            private OdsInstanceSettingsModel _settingsModel;

            protected override void AdditionalSetup()
            {
                _settingsModel = new OdsInstanceSettingsModel
                {
                    LearningStandardsModel = new LearningStandardsModel
                    {
                        ApiKey = ApiKey,
                        ApiSecret = ApiSecret
                    }
                };

                ApiModeProvider
                    .Setup(x => x.GetApiMode())
                    .Returns(ApiMode.Sandbox);

                ApiConnectionInformationProvider
                    .Setup(x => x.GetConnectionInformationForEnvironment(CloudOdsEnvironment.Production))
                    .ReturnsAsync(new OdsApiConnectionInformation ("Ods Instance", ApiMode.Sandbox) { ApiServerUrl = ProductionUrl });

                LearningStandardsSetupCommand.Setup(x => x.Execute(It.IsAny<AcademicBenchmarkConfig>()))
                    .Returns(Task.CompletedTask);

                LearningStandardsJob.Setup(x => x.EnqueueJob(It.IsAny<LearningStandardsJobContext>()));

            }

            [Test]
            public async Task ThenItShouldRespondWithStatusCode200()
            {
                // Act
                var result = await SystemUnderTest.LearningStandards(_settingsModel);

#if NET48
                // Assert
                result.ShouldBeOfType<HttpStatusCodeResult>();
                ((HttpStatusCodeResult)result).StatusCode.ShouldBe(200);
#else
                // Assert
                result.ShouldBeOfType<OkResult>();
                ((OkResult)result).StatusCode.ShouldBe(200);
#endif
            }

            [Test]
            public async Task ThenItShouldHaveSetupAValidLearningStandardsCommand()
            {
                // Act
                var _ = await SystemUnderTest.LearningStandards(_settingsModel);

                // Assert
                Func<AcademicBenchmarkConfig, bool> learningStandardsSetupCommandExecuteVerifier = actual =>
                {
                    actual.ShouldSatisfyAllConditions(
                        () => actual.ApiKey.ShouldBe(ApiKey),
                        () => actual.ApiSecret.ShouldBe(ApiSecret)
                    );
                    return true;
                };
                LearningStandardsSetupCommand.Verify(
                    x => x.Execute(It.Is<AcademicBenchmarkConfig>(y => learningStandardsSetupCommandExecuteVerifier(y))),
                    Times.Once);
            }

            [Test]
            public async Task ThenItShouldHaveEnqueuedALearningStandardsSyncJobWithoutYear()
            {
                // Act
                var _ = await SystemUnderTest.LearningStandards(_settingsModel);

                // Assert
                Func<LearningStandardsJobContext, bool> learningStandardsJobEnqueueVerifier = actual =>
                {
                    actual.ShouldSatisfyAllConditions(
                        () => actual.Environment.ShouldBe(CloudOdsEnvironment.Production.Value),
                        () => actual.ApiUrl.ShouldBe(ProductionUrl),
                        () => actual.SchoolYear.ShouldBeNull()
                    );
                    return true;
                };
                LearningStandardsJob.Verify(
                    x => x.EnqueueJob(It.Is<LearningStandardsJobContext>(y => learningStandardsJobEnqueueVerifier(y))),
                    Times.Once);
            }
        }

        [TestFixture]
        public class GivenIsUsingYearSpecificMode : OdsInstanceSettingsControllerFixture
        {
            private const string ProductionUrl = "http://example.com";
            private const string ApiKey = "key";
            private const string ApiSecret = "secret";
            private const int Year = 1234;
            private OdsInstanceSettingsModel _settingsModel;
            private readonly InstanceContext _instanceContext = new InstanceContext
            {
                Id = 1,
                Name = "Ed_Fi_Ods_1234"
            };

            protected override void AdditionalSetup()
            {
                _settingsModel = new OdsInstanceSettingsModel
                {
                    LearningStandardsModel = new LearningStandardsModel
                    {
                        ApiKey = ApiKey,
                        ApiSecret = ApiSecret
                    }
                };

                InstanceContext.Id = _instanceContext.Id;
                InstanceContext.Name = _instanceContext.Name;

                ApiModeProvider
                    .Setup(x => x.GetApiMode())
                    .Returns(ApiMode.YearSpecific);

                ApiConnectionInformationProvider
                    .Setup(x => x.GetConnectionInformationForEnvironment(CloudOdsEnvironment.Production))
                    .ReturnsAsync(new OdsApiConnectionInformation (_instanceContext.Name, ApiMode.YearSpecific) { ApiServerUrl = ProductionUrl });

                LearningStandardsSetupCommand.Setup(x => x.Execute(It.IsAny<AcademicBenchmarkConfig>()))
                    .Returns(Task.CompletedTask);

                LearningStandardsJob.Setup(x => x.EnqueueJob(It.IsAny<LearningStandardsJobContext>()));

            }

            [Test]
            public async Task ThenItShouldRespondWithStatusCode200()
            {
                // Act
                var result = await SystemUnderTest.LearningStandards(_settingsModel);

#if NET48
                // Assert
                result.ShouldBeOfType<HttpStatusCodeResult>();
                ((HttpStatusCodeResult)result).StatusCode.ShouldBe(200);
#else
                // Assert
                result.ShouldBeOfType<OkResult>();
                ((OkResult)result).StatusCode.ShouldBe(200);
#endif
            }

            [Test]
            public async Task ThenItShouldHaveSetupAValidLearningStandardsCommand()
            {
                // Act
                var _ = await SystemUnderTest.LearningStandards(_settingsModel);

                // Assert
                Func<AcademicBenchmarkConfig, bool> learningStandardsSetupCommandExecuteVerifier = actual =>
                {
                    actual.ShouldSatisfyAllConditions(
                        () => actual.ApiKey.ShouldBe(ApiKey),
                        () => actual.ApiSecret.ShouldBe(ApiSecret)
                    );
                    return true;
                };
                LearningStandardsSetupCommand.Verify(
                    x => x.Execute(It.Is<AcademicBenchmarkConfig>(y => learningStandardsSetupCommandExecuteVerifier(y))),
                    Times.Once);
            }

            [Test]
            public async Task ThenItShouldHaveEnqueuedALearningStandardsSyncJobWithYear()
            {
                // Act
                var _ = await SystemUnderTest.LearningStandards(_settingsModel);

                // Assert
                Func<LearningStandardsJobContext, bool> learningStandardsJobEnqueueVerifier = actual =>
                {
                    actual.ShouldSatisfyAllConditions(
                        () => actual.Environment.ShouldBe(CloudOdsEnvironment.Production.Value),
                        () => actual.ApiUrl.ShouldBe(ProductionUrl),
                        () => actual.SchoolYear.ShouldBe(Year)
                    );
                    return true;
                };
                LearningStandardsJob.Verify(
                    x => x.EnqueueJob(It.Is<LearningStandardsJobContext>(y => learningStandardsJobEnqueueVerifier(y))),
                    Times.Once);
            }
        }

        [TestFixture]
        public class GivenIsUsingMultiInstanceMode : OdsInstanceSettingsControllerFixture
        {
            private const string ProductionUrl = "http://example.com";
            private const string ApiKey = "key";
            private const string ApiSecret = "secret";
            private OdsInstanceSettingsModel _settingsModel;
            private readonly InstanceContext _instanceContext = new InstanceContext
            {
                Id = 1234,
                Name = "Ed_Fi_Ods_1234"
            };

            protected override void AdditionalSetup()
            {
                InstanceContext.Id = _instanceContext.Id;
                InstanceContext.Name = _instanceContext.Name;

                _settingsModel = new OdsInstanceSettingsModel
                {
                    LearningStandardsModel = new LearningStandardsModel
                    {
                        ApiKey = ApiKey,
                        ApiSecret = ApiSecret
                    }
                };

                ApiModeProvider
                    .Setup(x => x.GetApiMode())
                    .Returns(ApiMode.DistrictSpecific);

                ApiConnectionInformationProvider
                    .Setup(x => x.GetConnectionInformationForEnvironment(CloudOdsEnvironment.Production))
                    .ReturnsAsync(new OdsApiConnectionInformation (_instanceContext.Name, ApiMode.DistrictSpecific) { ApiServerUrl = ProductionUrl});

                LearningStandardsSetupCommand.Setup(x => x.Execute(It.IsAny<AcademicBenchmarkConfig>()))
                    .Returns(Task.CompletedTask);

                LearningStandardsJob.Setup(x => x.EnqueueJob(It.IsAny<LearningStandardsJobContext>()));

            }

            [Test]
            public async Task ThenItShouldRespondWithStatusCode200()
            {
                // Act
                var result = await SystemUnderTest.LearningStandards(_settingsModel);

#if NET48
                // Assert
                result.ShouldBeOfType<HttpStatusCodeResult>();
                ((HttpStatusCodeResult)result).StatusCode.ShouldBe(200);
#else
                // Assert
                result.ShouldBeOfType<OkResult>();
                ((OkResult)result).StatusCode.ShouldBe(200);
#endif
            }

            [Test]
            public async Task ThenItShouldHaveSetupAValidLearningStandardsCommand()
            {
                // Arrange
                UpdateConfiguration();

                // Act
                var _ = await SystemUnderTest.LearningStandards(_settingsModel);

                // Assert
                Func<AcademicBenchmarkConfig, bool> learningStandardsSetupCommandExecuteVerifier = actual =>
                {
                    actual.ShouldSatisfyAllConditions(
                        () => actual.ApiKey.ShouldBe(ApiKey),
                        () => actual.ApiSecret.ShouldBe(ApiSecret),
                        () => actual.OdsApiMode.ShouldBe(ApiMode.DistrictSpecific)
                    );
                    return true;
                };
                LearningStandardsSetupCommand.Verify(
                    x => x.Execute(It.Is<AcademicBenchmarkConfig>(y => learningStandardsSetupCommandExecuteVerifier(y))),
                    Times.Once);

                ResetConfiguration();
            }

            [Test]
            public async Task ThenItShouldHaveEnqueuedALearningStandardsSyncJobWithOdsInstanceId()
            {
                // Act
                var _ = await SystemUnderTest.LearningStandards(_settingsModel);

                // Assert
                Func<LearningStandardsJobContext, bool> learningStandardsJobEnqueueVerifier = actual =>
                {
                    actual.ShouldSatisfyAllConditions(
                        () => actual.Environment.ShouldBe(CloudOdsEnvironment.Production.Value),
                        () => actual.ApiUrl.ShouldBe(ProductionUrl),
                        () => actual.OdsInstanceId.ShouldBe(_instanceContext.Id)
                    );
                    return true;
                };
                LearningStandardsJob.Verify(
                    x => x.EnqueueJob(It.Is<LearningStandardsJobContext>(y => learningStandardsJobEnqueueVerifier(y))),
                    Times.Once);
            }

            private static void UpdateConfiguration()
            {
#if NET48
                ConfigurationHelper.SetApiMode("DistrictSpecific");
#else
                Startup.ConfigurationAppSettings.ApiStartupType = "DistrictSpecific";
#endif
            }

            private static void ResetConfiguration()
            {
#if NET48
                ConfigurationHelper.SetApiMode("sandbox");
#else
                Startup.ConfigurationAppSettings.ApiStartupType = "sandbox";
#endif
            }
        }
    }
}
