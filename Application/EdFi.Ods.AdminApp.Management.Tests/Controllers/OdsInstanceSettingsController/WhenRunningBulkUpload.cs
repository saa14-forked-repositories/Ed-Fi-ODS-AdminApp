// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;
#if NET48
using System.Web.Hosting;
using System.Web.Mvc;
#else
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
#endif
using EdFi.Ods.AdminApp.Management.Api;
using EdFi.Ods.AdminApp.Management.Helpers;
using EdFi.Ods.AdminApp.Management.Instances;
using EdFi.Ods.AdminApp.Web.Infrastructure.IO;
using EdFi.Ods.AdminApp.Web.Infrastructure.Jobs;
using EdFi.Ods.AdminApp.Web.Models.ViewModels;
using EdFi.Ods.AdminApp.Web.Models.ViewModels.OdsInstanceSettings;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApp.Management.Tests.Controllers.OdsInstanceSettingsController
{
    [TestFixture]
    public class WhenRunningBulkUpload : OdsInstanceSettingsControllerFixture
    {
        private readonly CloudOdsEnvironment _environment = CloudOdsEnvironment.Production;
        private static readonly InstanceContext OdsInstanceContext = new InstanceContext
        {
            Id = 1234,
            Name = "Ed_Fi_Ods_1234"
        };
        private static readonly OdsSecretConfiguration OdsSecretConfig = new OdsSecretConfiguration()
        {
            BulkUploadCredential = new BulkUploadCredential()
            {
                ApiKey = "key",
                ApiSecret = "secret"
            }
        };
        private readonly OdsApiConnectionInformation _connectionInformation = new OdsApiConnectionInformation(OdsInstanceContext.Name, ApiMode.Sandbox) {
            ApiServerUrl = "http://example.com", ClientKey = OdsSecretConfig.BulkUploadCredential.ApiKey, ClientSecret = OdsSecretConfig.BulkUploadCredential.ApiSecret
        };

        [Test]
        public async Task When_Perform_Get_Request_To_BulkUploadForm_Return_PartialView_With_Expected_Model()
        {
            // Arrange
            OdsSecretConfigurationProvider.Setup(x => x.GetSecretConfiguration(It.IsAny<int>()))
                .Returns(Task.FromResult(new OdsSecretConfiguration()));

            // Act
            var result = (ViewResult) await SystemUnderTest.BulkLoad();

            // Assert
            result.ShouldNotBeNull();
            var model = (OdsInstanceSettingsModel) result.Model;
            model.ShouldNotBeNull();
            model.BulkFileUploadModel.CloudOdsEnvironment.ShouldBeSameAs(_environment);
            model.BulkFileUploadModel.ApiKey.ShouldBeEmpty();
            model.BulkFileUploadModel.ApiSecret.ShouldBeEmpty();
            model.BulkFileUploadModel.CredentialsSaved.ShouldBeFalse();
        }

        [Test]
        public async Task When_Perform_Get_Request_To_BulkUploadForm_Return_PartialView_With_Expected_ModelData()
        {
            // Arrange
            const string expectedKey = "key";
            const string expectedSecret = "secret";

            OdsSecretConfigurationProvider.Setup(x => x.GetSecretConfiguration(It.IsAny<int>()))
                .Returns(Task.FromResult(new OdsSecretConfiguration
                {
                    BulkUploadCredential = new BulkUploadCredential
                    {
                        ApiKey = expectedKey,
                        ApiSecret = expectedSecret
                    }
                }));

            // Act
            var result = (ViewResult) await SystemUnderTest.BulkLoad();

            // Assert
            result.ShouldNotBeNull();
            var model = (OdsInstanceSettingsModel) result.Model;
            model.ShouldNotBeNull();
            model.BulkFileUploadModel.CloudOdsEnvironment.ShouldBeSameAs(_environment);
            model.BulkFileUploadModel.ApiKey.ShouldBe(expectedKey);
            model.BulkFileUploadModel.ApiSecret.ShouldBe(expectedSecret);
            model.BulkFileUploadModel.CredentialsSaved.ShouldBeTrue();
        }

        [Test]
        public async Task When_Perform_Get_Request_To_BulkUploadForm_With_Null_SecretConfig_Returns_JsonError()
        {
            // Arrange
            OdsSecretConfigurationProvider.Setup(x => x.GetSecretConfiguration(It.IsAny<int>()))
                .Returns(Task.FromResult<OdsSecretConfiguration>(null));

            // Act
            var result = (ContentResult)await SystemUnderTest.BulkLoad();

            // Assert
            result.ShouldNotBeNull();
            result.Content.Contains("ODS secret configuration can not be null").ShouldBeTrue();
        }

        [Test]
        public async Task When_Perform_Post_Request_To_BulkFileUpload_With_No_File_Returns_NoContent()
        {
            // Arrange
            var model = new OdsInstanceSettingsModel
            {
                BulkFileUploadModel = new BulkFileUploadModel
                {
#if NET48
                    BulkFiles = new List<HttpPostedFileBase>()
#else
                    BulkFiles = new List<IFormFile>()
#endif
                }
            };

            // Act
#if NET48
            var result = (HttpStatusCodeResult) await SystemUnderTest.BulkFileUpload(model);
#else
            var result = (NoContentResult)await SystemUnderTest.BulkFileUpload(model);
#endif

            // Assert
            result.ShouldNotBeNull();
            result.StatusCode.ShouldBe((int) HttpStatusCode.NoContent);
        }

        [Test]
        public void When_Perform_Post_Request_To_BulkFileUpload_With_Greater_File_ContentLength_ThrowsException()
        {
            // Arrange
#if NET48
            var file = new Mock<HttpPostedFileBase>();
            file.Setup(x => x.ContentLength).Returns(20000002);
#else
            var file = new Mock<IFormFile>();
            file.Setup(x => x.Length).Returns(20000002);
#endif
            var model = new OdsInstanceSettingsModel
            {
                BulkFileUploadModel = new BulkFileUploadModel
                {
#if NET48
                    BulkFiles = new List<HttpPostedFileBase>
                    {
                        file.Object
                    }
#else
                    BulkFiles = new List<IFormFile>
                    {
                        file.Object
                    }
#endif
                }
            };

            // Assert
            Assert.ThrowsAsync<Exception>(() => SystemUnderTest.BulkFileUpload(model)).Message
                .Contains("Upload exceeds maximum limit").ShouldBeTrue();
        }

        [Test]
        public void When_Perform_Post_Request_To_BulkFileUpload_With_Multiple_Files_ThrowsException()
        {
            // Arrange
#if NET48
            var file1 = new Mock<HttpPostedFileBase>();
            file1.Setup(x => x.ContentLength).Returns(200);
            var file2 = new Mock<HttpPostedFileBase>();
            file2.Setup(x => x.ContentLength).Returns(200);
#else
            var file1 = new Mock<IFormFile>();
            file1.Setup(x => x.Length).Returns(200);
            var file2 = new Mock<IFormFile>();
            file2.Setup(x => x.Length).Returns(200);
#endif

            var model = new OdsInstanceSettingsModel
            {
                BulkFileUploadModel = new BulkFileUploadModel
                {
#if NET48
                    BulkFiles = new List<HttpPostedFileBase>
                    {
                        file1.Object, file2.Object
                    }
#else
                    BulkFiles = new List<IFormFile>
                    {
                        file1.Object, file2.Object
                    }
#endif
                }
            };

            // Assert
            Assert.ThrowsAsync<Exception>(() => SystemUnderTest.BulkFileUpload(model)).Message
                .Contains("Currently, the bulk import process only supports a single file at a time").ShouldBeTrue();
        }

        [Test]
        public async Task When_Perform_Post_Request_To_BulkFileUpload_With_Valid_File_Job_Should_Be_Enqueued()
        {
            const string odsApiVersion = "5.0.0";
            const string edfiStandardVersion = "3.2.0-c";
            InferOdsApiVersion.Setup(x => x.Version("http://example.com")).Returns(odsApiVersion);
            InferOdsApiVersion.Setup(x => x.EdFiStandardVersion("http://example.com")).Returns(edfiStandardVersion);

#if NET48
            var schemaBasePath = HostingEnvironment.MapPath(ConfigurationHelper.GetAppSettings().XsdFolder);
#else
            var schemaBasePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Schema");
#endif
            var schemaPath = $"{schemaBasePath}\\{edfiStandardVersion}";

            var model = SetupBulkUpload(out var fileUploadResult);

            BulkUploadJob.Setup(x => x.IsJobRunning()).Returns(false);
            BulkUploadJob.Setup(x => x.IsSameOdsInstance(OdsInstanceContext.Id, typeof(BulkUploadJobContext))).Returns(true);

            var result = (PartialViewResult) await SystemUnderTest.BulkFileUpload(model);

            // Assert
            Func<BulkUploadJobContext, bool> bulkUploadJobEnqueueVerifier = actual =>
            {
                actual.ShouldSatisfyAllConditions(
                    () => actual.Environment.ShouldBe(CloudOdsEnvironment.Production.Value),
                    () => actual.DataDirectoryFullPath.ShouldBe(fileUploadResult.Directory),
                    () => actual.OdsInstanceId.ShouldBe(OdsInstanceContext.Id),
                    () => actual.ApiBaseUrl.ShouldBe(_connectionInformation.ApiBaseUrl),
                    () => actual.ClientKey.ShouldBe(_connectionInformation.ClientKey),
                    () => actual.ClientSecret.ShouldBe(_connectionInformation.ClientSecret),
                    () => actual.DependenciesUrl.ShouldBe(_connectionInformation.DependenciesUrl),
                    () => actual.MetadataUrl.ShouldBe(_connectionInformation.MetadataUrl),
                    () => actual.OauthUrl.ShouldBe(_connectionInformation.OAuthUrl),
                    () => actual.SchemaPath.ShouldBe(schemaPath),
                    () => actual.MaxSimultaneousRequests.ShouldBe(20)
                );
                return true;
            };
            result.ShouldNotBeNull();
            result.ViewName.ShouldBe("_SignalRStatus_BulkLoad");
            result.Model.ShouldNotBeNull();
            var settingsModel = (OdsInstanceSettingsModel) result.Model;
            settingsModel.BulkFileUploadModel.ShouldNotBeNull();
            settingsModel.BulkFileUploadModel.IsSameOdsInstance.ShouldBeTrue();
            BulkUploadJob.Verify(
                x => x.EnqueueJob(It.Is<BulkUploadJobContext>(y => bulkUploadJobEnqueueVerifier(y))),
                Times.Once);
        }

        [Test]
        public async Task When_Perform_Post_Request_To_BulkFileUpload_Against_ODS3_Job_Should_Be_Enqueued_With_Pessimistic_Throttling()
        {
            const string odsApiVersion = "3.4.0";
            const string edfiStandardVersion = "3.2.0-b";
            InferOdsApiVersion.Setup(x => x.Version("http://example.com")).Returns(odsApiVersion);
            InferOdsApiVersion.Setup(x => x.EdFiStandardVersion("http://example.com")).Returns(edfiStandardVersion);

            var model = SetupBulkUpload(out var fileUploadResult);

            BulkUploadJob.Setup(x => x.IsJobRunning()).Returns(false);
            BulkUploadJob.Setup(x => x.IsSameOdsInstance(OdsInstanceContext.Id, typeof(BulkUploadJobContext))).Returns(true);

            var result = (PartialViewResult)await SystemUnderTest.BulkFileUpload(model);

            // Assert
            Func<BulkUploadJobContext, bool> bulkUploadJobEnqueueVerifier = actual =>
            {
                actual.MaxSimultaneousRequests.ShouldBe(1);
                return true;
            };
            result.ShouldNotBeNull();
            result.ViewName.ShouldBe("_SignalRStatus_BulkLoad");
            result.Model.ShouldNotBeNull();
            var settingsModel = (OdsInstanceSettingsModel)result.Model;
            settingsModel.BulkFileUploadModel.ShouldNotBeNull();
            settingsModel.BulkFileUploadModel.IsSameOdsInstance.ShouldBeTrue();
            BulkUploadJob.Verify(
                x => x.EnqueueJob(It.Is<BulkUploadJobContext>(y => bulkUploadJobEnqueueVerifier(y))),
                Times.Once);
        }

        [Test]
        public async Task When_Job_Is_Already_Running_New_Job_Should_Not_Be_Enqueued()
        {
            const string odsApiVersion = "3.4.0";
            const string edfiStandardVersion = "3.2.0-b";
            InferOdsApiVersion.Setup(x => x.Version("http://example.com")).Returns(odsApiVersion);
            InferOdsApiVersion.Setup(x => x.EdFiStandardVersion("http://example.com")).Returns(edfiStandardVersion);

            var model = SetupBulkUpload(out var fileUploadResult);

            BulkUploadJob.Setup(x => x.IsJobRunning()).Returns(true);
            BulkUploadJob.Setup(x => x.IsSameOdsInstance(OdsInstanceContext.Id, typeof(BulkUploadJobContext))).Returns(true);

            var result = (PartialViewResult)await SystemUnderTest.BulkFileUpload(model);

            // Assert
            result.ShouldNotBeNull();
            result.ViewName.ShouldBe("_SignalRStatus_BulkLoad");
            result.Model.ShouldNotBeNull();
            var settingsModel = (OdsInstanceSettingsModel)result.Model;
            settingsModel.BulkFileUploadModel.ShouldNotBeNull();
            settingsModel.BulkFileUploadModel.IsJobRunning.ShouldBeTrue();
            settingsModel.BulkFileUploadModel.IsSameOdsInstance.ShouldBeTrue();
            BulkUploadJob.Verify(
                x => x.EnqueueJob(It.IsAny<BulkUploadJobContext>()),
                Times.Never);
        }

        private OdsInstanceSettingsModel SetupBulkUpload(out FileUploadResult fileUploadResult)
        {
            const string filename = "test.xml";

#if NET48
            var file = new Mock<HttpPostedFileBase>();
            file.Setup(x => x.ContentLength).Returns(200);
            file.Setup(x => x.FileName).Returns("test.xml");
            var model = new OdsInstanceSettingsModel
            {
                BulkFileUploadModel = new BulkFileUploadModel
                {
                    BulkFiles = new List<HttpPostedFileBase>
                    {
                        file.Object
                    }
                }
            };
#else
            var file = new Mock<IFormFile>();
            file.Setup(x => x.Length).Returns(200);
            file.Setup(x => x.FileName).Returns("test.xml");
            var model = new OdsInstanceSettingsModel
            {
                BulkFileUploadModel = new BulkFileUploadModel
                {
                    BulkFiles = new List<IFormFile>
                    {
                        file.Object
                    }
                }
            };
#endif
            fileUploadResult = new FileUploadResult
            {
                Directory = "directoryPath",
                FileNames = new[] {filename}
            };

            InstanceContext.Id = OdsInstanceContext.Id;
            InstanceContext.Name = OdsInstanceContext.Name;

#if NET48
            FileUploadHandler.Setup(x =>
                    x.SaveFilesToUploadDirectory(It.IsAny<HttpPostedFileBase[]>(), It.IsAny<Func<string, string>>()))
                .Returns(fileUploadResult);
#else
            FileUploadHandler.Setup(x =>
                    x.SaveFilesToUploadDirectory(It.IsAny<IFormFile[]>(), It.IsAny<Func<string, string>>()))
                .Returns(fileUploadResult);
#endif

            ApiConnectionInformationProvider
                .Setup(x => x.GetConnectionInformationForEnvironment(CloudOdsEnvironment.Production))
                .ReturnsAsync(_connectionInformation);

            OdsSecretConfigurationProvider.Setup(x => x.GetSecretConfiguration(It.IsAny<int>()))
                .Returns(Task.FromResult(OdsSecretConfig));
            return model;
        }

        [Test]
        public async Task When_Perform_Post_Request_To_SaveBulkLoadCredentials_With_BulkUpload_Credentials_Return_Json_Success()
        {
            // Arrange
            const string expectedKey = "key";
            const string expectedSecret = "secret";
            var model = new SaveBulkUploadCredentialsModel
            {
                ApiKey = expectedKey,
                ApiSecret = expectedSecret
            };
          
            OdsSecretConfigurationProvider.Setup(x => x.GetSecretConfiguration(It.IsAny<int>()))
                .Returns(Task.FromResult(new OdsSecretConfiguration()));

            // Act
            var result = (ContentResult) await SystemUnderTest.SaveBulkLoadCredentials(model);

            // Assert
            result.Content.Contains("Credentials successfully saved").ShouldBeTrue();
        }

        [Test]
        public async Task When_Perform_Post_Request_To_ResetCredentials_With_Valid_OdsSecretConfig_Returns_Json_Success()
        {
            // Arrange
            const string expectedKey = "key";
            const string expectedSecret = "secret";
            var odsConfig = new OdsSecretConfiguration
            {
                BulkUploadCredential = new BulkUploadCredential
                {
                    ApiKey = expectedKey,
                    ApiSecret = expectedSecret
                }
            };
            OdsSecretConfigurationProvider.Setup(x => x.GetSecretConfiguration(It.IsAny<int>()))
                .Returns(Task.FromResult(odsConfig));

            // Act
            var result = (ContentResult) await SystemUnderTest.ResetCredentials();

            // Assert
            result.Content.Contains("Credentials successfully reset").ShouldBeTrue();
        }

        [Test]
        public async Task When_Perform_Post_Request_To_ResetCredentials_With_No_BulkUploadCredentials_Returns_Json_Error()
        {
            // Arrange
            OdsSecretConfigurationProvider.Setup(x => x.GetSecretConfiguration(It.IsAny<int>()))
                .Returns(Task.FromResult(new OdsSecretConfiguration()));

            // Act
            var result = (ContentResult) await SystemUnderTest.ResetCredentials();

            // Assert
            result.Content.Contains("Missing bulk load credentials").ShouldBeTrue();
        }
    }
}
