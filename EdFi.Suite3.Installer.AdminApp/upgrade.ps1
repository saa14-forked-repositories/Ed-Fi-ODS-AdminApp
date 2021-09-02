# SPDX-License-Identifier: Apache-2.0
# Licensed to the Ed-Fi Alliance under one or more agreements.
# The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
# See the LICENSE and NOTICES files in the project root for more information.

import-module -force "$PSScriptRoot/Install-EdFiOdsAdminApp.psm1"

<#
Admin App will be upgraded to provided version. Appsettings values and connection strings will be copied over from existing Admin App application.

.EXAMPLE
    $p = @{
    ToolsPath = "$PSScriptRoot/tools"
    PackageVersion = '2.2.1' }
#>

$p = @{
    ToolsPath = "$PSScriptRoot/tools"
    PackageVersion = '2.2.1'
}

Upgrade-EdFiOdsAdminApp @p

