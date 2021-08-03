// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using EdFi.Ods.AdminApp.Management.ClaimSetEditor;
using NUnit.Framework;
using Shouldly;
using static EdFi.Ods.AdminApp.Management.Tests.Testing;
using Application = EdFi.Security.DataAccess.Models.Application;
using ClaimSet = EdFi.Security.DataAccess.Models.ClaimSet;

namespace EdFi.Ods.AdminApp.Management.Tests.ClaimSetEditor
{
    public class ClaimSetCheckServiceTests : SecurityDataTestBase
    {
        [Test]
        public void ShouldNotRestartIfRequiredClaimSetsExist()
        {
            var testApplication = new Application
            {
                ApplicationName = $"Test Application {DateTime.Now:O}"
            };
            Save(testApplication);

            var testAbConnectClaimSet = new ClaimSet { ClaimSetName = "AB Connect", Application = testApplication };
            Save(testAbConnectClaimSet);

            var testAdminAppClaimSet = new ClaimSet { ClaimSetName = "Ed-Fi ODS Admin App", Application = testApplication };
            Save(testAdminAppClaimSet);

            Scoped<ClaimSetCheckService>(service =>
            {
                var result = service.IsRestartRequired();
                result.ShouldBeFalse();
            });
        }

        [Test]
        public void ShouldRestartIfRequiredClaimSetsDoNotExist()
        {
            var testApplication = new Application
            {
                ApplicationName = $"Test Application {DateTime.Now:O}"
            };
            Save(testApplication);

            Scoped<ClaimSetCheckService>(service =>
            {
                var result = service.IsRestartRequired();
                result.ShouldBeTrue();
            });
        }
    }
}
