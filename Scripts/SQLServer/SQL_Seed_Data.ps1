
. "$PSScriptRoot\..\var-env-setting-Dev.ps1"

$SeedUsers = @(
    @{ NationalId = '1000000001'; FullName = 'مقاول تجريبي';     Role = 'ContractorConsultant' },
    @{ NationalId = '1000000002'; FullName = 'موظف P3O تجريبي';   Role = 'P3OOfficer' },
    @{ NationalId = '1000000003'; FullName = 'مدير النظام تجريبي'; Role = 'SystemAdministrator' }
)


function Read-MenuChoice {
    param(
        [string]$Prompt,
        [hashtable]$Options
    )
    Write-Host $Prompt
    foreach ($key in ($Options.Keys | Sort-Object)) {
        Write-Host "  $key - $($Options[$key])"
    }
    while ($true) {
        $choice = Read-Host "Enter choice"
        if (-not [string]::IsNullOrEmpty($choice) -and $Options.ContainsKey($choice)) {
            return $choice
        }
        Write-Host "Invalid choice, try again."
    }
}

function Read-OptionalField {
    param([string]$Prompt)
    $value = Read-Host "$Prompt (optional, press Enter to skip)"
    if ([string]::IsNullOrWhiteSpace($value)) { return $null }
    return $value
}

function ConvertTo-SqlLiteral {
    param([string]$Value)
    if ([string]::IsNullOrWhiteSpace($Value)) { return 'NULL' }
    return "N'$($Value.Replace("'", "''"))'"
}

function Invoke-SqlPayload {
    param(
        [string]$Sql,
        [string]$Label
    )
    $content = "SET QUOTED_IDENTIFIER ON;`r`n" + $Sql
    $utf8NoBom = New-Object System.Text.UTF8Encoding $false
    $tempFile = Join-Path $env:TEMP "sql-seed-data-$([System.Guid]::NewGuid().ToString('N')).sql"
    [System.IO.File]::WriteAllText($tempFile, $content, $utf8NoBom)

    $containerFile = "/tmp/$([System.IO.Path]::GetFileName($tempFile))"
    docker cp $tempFile "sql1:$containerFile" | Out-Null
    Remove-Item $tempFile

    Write-Host "Running $Label..."
    docker exec sql1 /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$env:DB_PASSWORD" -C -d PMIS -i $containerFile
    $exitCode = $LASTEXITCODE

    docker exec sql1 rm -f $containerFile 2>$null | Out-Null

    if ($exitCode -ne 0) {
        Write-Error "$Label failed (exit code $exitCode) - see output above."
        exit $exitCode
    }
}

function Invoke-SqlScalar {
    param([string]$Sql)

    $content = "SET QUOTED_IDENTIFIER ON;`r`nSET NOCOUNT ON;`r`n" + $Sql
    $utf8NoBom = New-Object System.Text.UTF8Encoding $false
    $tempFile = Join-Path $env:TEMP "sql-seed-query-$([System.Guid]::NewGuid().ToString('N')).sql"
    [System.IO.File]::WriteAllText($tempFile, $content, $utf8NoBom)

    $containerFile = "/tmp/$([System.IO.Path]::GetFileName($tempFile))"
    docker cp $tempFile "sql1:$containerFile" | Out-Null
    Remove-Item $tempFile

    $result = docker exec sql1 /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$env:DB_PASSWORD" -C -d PMIS -h -1 -i $containerFile
    $exitCode = $LASTEXITCODE

    docker exec sql1 rm -f $containerFile 2>$null | Out-Null

    if ($exitCode -ne 0) {
        Write-Error "Query failed (exit code $exitCode) - see output above."
        exit $exitCode
    }

    ($result | Where-Object { $_ -and $_.Trim() -ne '' } | Select-Object -First 1)
}


function New-CustomUser {
    $nationalId = $null
    while ($true) {
        $candidate = Read-Host "Enter the new user's National ID - 10 digits, starting with 1 or 2 (or press Enter to cancel)"
        if ([string]::IsNullOrWhiteSpace($candidate)) {
            Write-Host "Cancelled - no National ID entered."
            return
        }
        if ($candidate -notmatch '^[12]\d{9}$') {
            Write-Host "'$candidate' is not a valid National ID (10 digits, starting with 1 or 2) - try again."
            continue
        }

        $escapedId = $candidate.Replace("'", "''")
        $existing = Invoke-SqlScalar -Sql "SELECT CAST([Id] AS NVARCHAR(36)) FROM [Users] WHERE [NationalId] = N'$escapedId';"
        if (-not [string]::IsNullOrWhiteSpace($existing)) {
            Write-Host "A user with National ID '$candidate' already exists - try a different one."
            continue
        }

        $nationalId = $candidate
        break
    }

    Write-Host "Enter the new user's details:"
    $fullName = Read-Host "Full name (required)"
    while ([string]::IsNullOrWhiteSpace($fullName)) {
        $fullName = Read-Host "Full name cannot be blank - enter a value"
    }
    $arabicFirstName       = Read-OptionalField "First name (Arabic)"
    $arabicFatherName      = Read-OptionalField "Father's name (Arabic)"
    $arabicGrandFatherName = Read-OptionalField "Grandfather's name (Arabic)"
    $arabicFamilyName      = Read-OptionalField "Family name (Arabic)"
    $arabicNationality     = Read-OptionalField "Nationality"
    $genderOptions = @{ "1" = "ذكر"; "2" = "أنثى"; "3" = "(skip - no gender on file)" }
    $genderChoice = Read-MenuChoice -Prompt "Gender for this user?" -Options $genderOptions
    $gender = if ($genderChoice -eq "3") { $null } else { $genderOptions[$genderChoice] }
    $email                 = Read-OptionalField "Email"
    $mobile                = Read-OptionalField "Mobile"

    $roleOptions = @{
        "1" = "ContractorConsultant"
        "2" = "P3OOfficer"
        "3" = "SystemAdministrator"
    }
    $roleChoice = Read-MenuChoice -Prompt "Role for this user?" -Options $roleOptions
    $role = $roleOptions[$roleChoice]

    $userId = [System.Guid]::NewGuid().ToString()

    $sql = @"
INSERT INTO [Users] ([Id], [FullName], [NationalId], [ArabicFirstName], [ArabicFatherName], [ArabicGrandFatherName], [ArabicFamilyName], [ArabicNationality], [Gender], [Email], [Mobile], [IsActive], [DeletedAccount], [CreatedDate])
VALUES ('$userId', $(ConvertTo-SqlLiteral $fullName), N'$nationalId', $(ConvertTo-SqlLiteral $arabicFirstName), $(ConvertTo-SqlLiteral $arabicFatherName), $(ConvertTo-SqlLiteral $arabicGrandFatherName), $(ConvertTo-SqlLiteral $arabicFamilyName), $(ConvertTo-SqlLiteral $arabicNationality), $(ConvertTo-SqlLiteral $gender), $(ConvertTo-SqlLiteral $email), $(ConvertTo-SqlLiteral $mobile), 1, 0, SYSUTCDATETIME());

INSERT INTO [UserRoleAssignments] ([UserId], [Role], [CreatedDate])
VALUES ('$userId', N'$role', SYSUTCDATETIME());
"@

    Invoke-SqlPayload -Sql $sql -Label "custom user creation"
    Write-Host "Created user: $nationalId - $fullName ($role)."

    $seedOrdersChoice = Read-MenuChoice -Prompt "Seed order/request data for this user now?" -Options @{ "1" = "yes"; "2" = "no" }
    if ($seedOrdersChoice -eq "1") {
        Seed-RequestsForExistingUser -UserId $userId -NationalId $nationalId
    }
}


function New-SeedUsers {
    $values = ($SeedUsers | ForEach-Object {
        "(N'$($_.FullName)', N'$($_.NationalId)', 1, SYSUTCDATETIME())"
    }) -join ",`r`n    "

    $roleInserts = ($SeedUsers | ForEach-Object {
        "INSERT INTO [UserRoleAssignments] ([UserId], [Role], [CreatedDate])`r`n" +
        "    SELECT u.[Id], N'$($_.Role)', SYSUTCDATETIME() FROM [Users] u`r`n" +
        "    WHERE u.[NationalId] = N'$($_.NationalId)'`r`n" +
        "      AND NOT EXISTS (SELECT 1 FROM [UserRoleAssignments] ra WHERE ra.[UserId] = u.[Id] AND ra.[Role] = N'$($_.Role)');"
    }) -join "`r`n"

    $sql = @"
-- Skip any seed user that already exists (NationalId unique filtered index) so this
-- operation is safe to re-run without duplicate-key errors.
MERGE [Users] AS target
USING (VALUES
    $values
) AS source ([FullName], [NationalId], [IsActive], [CreatedDate])
ON target.[NationalId] = source.[NationalId]
WHEN NOT MATCHED THEN
    INSERT ([FullName], [NationalId], [IsActive], [CreatedDate])
    VALUES (source.[FullName], source.[NationalId], source.[IsActive], source.[CreatedDate]);

$roleInserts
"@

    Invoke-SqlPayload -Sql $sql -Label "seed users creation"
    Write-Host "Seed users ready:"
    foreach ($u in $SeedUsers) {
        Write-Host "  $($u.NationalId) - $($u.FullName) ($($u.Role))"
    }
}


function New-RequestDataForUser {
    while ($true) {
        $nationalId = Read-Host "Enter the National ID to seed data for (or press Enter to cancel)"
        if ([string]::IsNullOrWhiteSpace($nationalId)) {
            Write-Host "Cancelled - no National ID entered."
            return
        }

        $escapedId = $nationalId.Replace("'", "''")
        $userId = Invoke-SqlScalar -Sql "SELECT CAST([Id] AS NVARCHAR(36)) FROM [Users] WHERE [NationalId] = N'$escapedId';"

        if ([string]::IsNullOrWhiteSpace($userId)) {
            Write-Host "No user found with National ID '$nationalId' - try again."
            continue
        }

        $fullName = Invoke-SqlScalar -Sql "SELECT [FullName] FROM [Users] WHERE [Id] = '$userId';"
        Write-Host "Found user: $fullName ($nationalId)."
        Seed-RequestsForExistingUser -UserId $userId -NationalId $nationalId
        return
    }
}

function Seed-RequestsForExistingUser {
    param(
        [string]$UserId,
        [string]$NationalId
    )

    $reviewerId = Invoke-SqlScalar -Sql @"
SELECT TOP 1 CAST(u.[Id] AS NVARCHAR(36))
FROM [Users] u
JOIN [UserRoleAssignments] ra ON ra.[UserId] = u.[Id]
WHERE ra.[Role] = N'P3OOfficer' AND u.[IsActive] = 1
ORDER BY CASE WHEN u.[NationalId] = N'$($SeedUsers[1].NationalId)' THEN 0 ELSE 1 END, u.[CreatedDate];
"@
    if ([string]::IsNullOrWhiteSpace($reviewerId)) {
        Write-Error "Cannot seed request data: no active P3OOfficer exists yet to act as reviewer. Run 'create seed users' first (or create one via the app), then retry."
        exit 1
    }
    $reviewerLiteral = "'$reviewerId'"

    $companyId = [System.Guid]::NewGuid().ToString()
    $projectId = [System.Guid]::NewGuid().ToString()
    $safeName = ($NationalId -replace '[^0-9]', '')

    $last6 = if ($safeName.Length -ge 6) { $safeName.Substring($safeName.Length - 6) } else { $safeName.PadLeft(6, '0') }
    $utcNow = [System.DateTime]::UtcNow
    $secondsSinceMidnight = [int]$utcNow.TimeOfDay.TotalSeconds
    $runToken = $secondsSinceMidnight.ToString('D6')
    $reqNumberPrefix = "REQ-$last6-$runToken"

    $statuses = @(
        @{ Status = 'Draft';                       DaysAgo = 3;  Review = $false; Note = $null;         License = $false }
        @{ Status = 'Submitted';                    DaysAgo = 10; Review = $false; Note = $null;         License = $false }
        @{ Status = 'ReturnedForCorrection';        DaysAgo = 20; Review = $true;  Note = 'Return';      License = $false }
        @{ Status = 'Rejected';                     DaysAgo = 30; Review = $true;  Note = 'Rejection';   License = $false }
        @{ Status = 'AwaitingPaymentConfirmation';  DaysAgo = 15; Review = $true;  Note = $null;         License = $false }
        @{ Status = 'PaymentConfirmed';             DaysAgo = 12; Review = $true;  Note = $null;         License = $false }
        @{ Status = 'Activated';                    DaysAgo = 45; Review = $true;  Note = $null;         License = $true  }
        @{ Status = 'Cancelled';                    DaysAgo = 25; Review = $true;  Note = $null;         License = $false }
    )

    $documentsReasonId = '00000000-0000-0000-0000-000000000101'
    $contractReasonId  = '00000000-0000-0000-0000-000000000102'

    $requestRows = @()
    $noteRows = @()
    $licenseRows = @()
    $index = 0

    foreach ($entry in $statuses) {
        $index++
        $reqId = [System.Guid]::NewGuid().ToString()
        $reqNumber = "$reqNumberPrefix-$('{0:D2}' -f $index)"
        $requestDate = (Get-Date).AddDays(-$entry.DaysAgo).ToString('yyyy-MM-dd')
        $reviewDateLiteral = if ($entry.Review) { "'$((Get-Date).AddDays(-$entry.DaysAgo + 1).ToString('yyyy-MM-dd'))'" } else { 'NULL' }
        $reviewedByLiteral = if ($entry.Review) { $reviewerLiteral } else { 'NULL' }
        $paymentConfirmedLiteral = if ($entry.Status -in @('PaymentConfirmed', 'Activated')) {
            "'$((Get-Date).AddDays(-$entry.DaysAgo + 2).ToString('yyyy-MM-dd'))'"
        } else { 'NULL' }

        $requestRows += "('$reqId', N'$reqNumber', '$UserId', '$companyId', '$projectId', @UnifierSystemId, '$requestDate', N'$($entry.Status)', $reviewDateLiteral, $reviewedByLiteral, NULL, N'Contractor', $paymentConfirmedLiteral, 1, '$requestDate')"

        if ($entry.Note -eq 'Return') {
            $noteDate = (Get-Date).AddDays(-$entry.DaysAgo + 1).ToString('yyyy-MM-dd')
            $noteRows += "(NEWID(), '$reqId', $reviewerLiteral, N'يرجى استكمال المستندات المطلوبة وإعادة تقديم الطلب.', N'Return', '$documentsReasonId', 1, '$noteDate')"
        } elseif ($entry.Note -eq 'Rejection') {
            $noteDate = (Get-Date).AddDays(-$entry.DaysAgo + 1).ToString('yyyy-MM-dd')
            $noteRows += "(NEWID(), '$reqId', $reviewerLiteral, N'لا يوجد عقد ساري مرتبط بالمشروع المذكور في سجلات الأمانة.', N'Rejection', '$contractReasonId', 1, '$noteDate')"
        }

        if ($entry.License) {
            $startDate = (Get-Date).AddDays(-$entry.DaysAgo + 3)
            $endDate = $startDate.AddMonths(12)
            $licenseRows += "(NEWID(), '$reqId', 1, '$($startDate.ToString('yyyy-MM-dd'))', '$($endDate.ToString('yyyy-MM-dd'))', '$($startDate.ToString('yyyy-MM-dd'))', N'Active', 1, '$($startDate.ToString('yyyy-MM-dd'))')"
        }
    }

    $requestValues = $requestRows -join ",`r`n    "
    $noteInsert = if ($noteRows.Count -gt 0) {
        "INSERT INTO [RequestNotes] ([Id], [RequestId], [UserId], [NoteText], [NoteType], [ReasonId], [IsActive], [CreatedDate])`r`nVALUES`r`n    " + ($noteRows -join ",`r`n    ") + ";"
    } else { "-- no notes for this batch" }
    $licenseInsert = if ($licenseRows.Count -gt 0) {
        "INSERT INTO [Licenses] ([Id], [RequestId], [NoOfLicenses], [StartDate], [EndDate], [ActivationDate], [Status], [IsActive], [CreatedDate])`r`nVALUES`r`n    " + ($licenseRows -join ",`r`n    ") + ";"
    } else { "-- no license for this batch" }

    $sql = @"
DECLARE @UnifierSystemId UNIQUEIDENTIFIER = '00000000-0000-0000-0000-000000000001';

INSERT INTO [Companies] ([Id], [CompanyName], [CommercialNo], [VatNo], [NationalAddress], [Iban], [Phone], [Email], [IsActive], [CreatedDate])
VALUES ('$companyId', N'شركة تجريبية مولّدة - $safeName', N'10$safeName', N'300${safeName}00003', N'المدينة المنورة - عنوان تجريبي', N'SA0310000${safeName}000001', N'0112220000', N'seed-$safeName@example.com', 1, SYSUTCDATETIME());

INSERT INTO [Projects] ([Id], [ProjectName], [ProjectNumber], [ContractNumber], [ProjectOwner], [IsActive], [CreatedDate])
VALUES ('$projectId', N'مشروع تجريبي مولّد - $safeName', N'PRJ-SEED-$safeName', N'CT-SEED-$safeName', N'أمانة منطقة المدينة المنورة', 1, SYSUTCDATETIME());

INSERT INTO [Requests] ([Id], [RequestNumber], [UserId], [CompanyId], [ProjectId], [SystemId], [RequestDate], [Status], [ReviewDate], [ReviewedById], [Notes], [UserTypeDisplay], [PaymentConfirmedDate], [IsActive], [CreatedDate])
VALUES
    $requestValues;

$noteInsert

$licenseInsert
"@

    Invoke-SqlPayload -Sql $sql -Label "request data seed for National ID '$NationalId'"
    Write-Host "Seeded $($statuses.Count) requests (one per status) for National ID '$NationalId'."
    Write-Host "Request numbers: $reqNumberPrefix-01 .. $reqNumberPrefix-$('{0:D2}' -f $statuses.Count)"
}


function Remove-SeedUsers {
    $idList = ($SeedUsers | ForEach-Object { "N'$($_.NationalId)'" }) -join ', '

    $sql = @"
DECLARE @SeedUserIds TABLE (Id UNIQUEIDENTIFIER);
INSERT INTO @SeedUserIds (Id) SELECT [Id] FROM [Users] WHERE [NationalId] IN ($idList);

DELETE l FROM [Licenses] l
    JOIN [Requests] r ON r.[Id] = l.[RequestId]
    WHERE r.[UserId] IN (SELECT Id FROM @SeedUserIds);

DELETE n FROM [RequestNotes] n
    JOIN [Requests] r ON r.[Id] = n.[RequestId]
    WHERE r.[UserId] IN (SELECT Id FROM @SeedUserIds);

DECLARE @SeedCompanyIds TABLE (Id UNIQUEIDENTIFIER);
INSERT INTO @SeedCompanyIds (Id) SELECT DISTINCT [CompanyId] FROM [Requests] WHERE [UserId] IN (SELECT Id FROM @SeedUserIds);

DECLARE @SeedProjectIds TABLE (Id UNIQUEIDENTIFIER);
INSERT INTO @SeedProjectIds (Id) SELECT DISTINCT [ProjectId] FROM [Requests] WHERE [UserId] IN (SELECT Id FROM @SeedUserIds);

DELETE FROM [Requests] WHERE [UserId] IN (SELECT Id FROM @SeedUserIds);

-- Only remove companies/projects this script itself generated (operation 2 always
-- creates one dedicated Company+Project per run, never reusing an existing one), so
-- this never deletes a company/project a real request also references.
DELETE c FROM [Companies] c
    JOIN @SeedCompanyIds sc ON sc.Id = c.[Id]
    WHERE NOT EXISTS (SELECT 1 FROM [Requests] r WHERE r.[CompanyId] = c.[Id]);

DELETE p FROM [Projects] p
    JOIN @SeedProjectIds sp ON sp.Id = p.[Id]
    WHERE NOT EXISTS (SELECT 1 FROM [Requests] r WHERE r.[ProjectId] = p.[Id]);

DELETE FROM [UserRoleAssignments] WHERE [UserId] IN (SELECT Id FROM @SeedUserIds);
DELETE FROM [Users] WHERE [NationalId] IN ($idList);

SELECT
    (SELECT COUNT(*) FROM [Users]) AS RemainingUsers,
    (SELECT COUNT(*) FROM [Requests]) AS RemainingRequests,
    (SELECT COUNT(*) FROM [Companies]) AS RemainingCompanies,
    (SELECT COUNT(*) FROM [Projects]) AS RemainingProjects;
"@

    Invoke-SqlPayload -Sql $sql -Label "seed-user undo"
    Write-Host "Removed seed users and everything generated for them."
}


$operation = Read-MenuChoice -Prompt "Which operation?" -Options @{
    "1" = "create seed users"
    "2" = "seed data to a user"
    "3" = "undo (remove seed users and their generated data)"
}

switch ($operation) {
    "1" {
        $createChoice = Read-MenuChoice -Prompt "Create seed users how?" -Options @{
            "1" = "create a new user with a National ID you choose"
            "2" = "seed 3 random users (fixed cast: contractor / P3O officer / admin)"
        }
        if ($createChoice -eq "1") { New-CustomUser } else { New-SeedUsers }
    }
    "2" {
        $answer = Read-MenuChoice -Prompt "Seed data to a user?" -Options @{ "1" = "yes"; "2" = "no" }
        if ($answer -eq "1") {
            New-RequestDataForUser
        } else {
            Write-Host "Nothing to do."
        }
    }
    "3" { Remove-SeedUsers }
}

Write-Host "Done."
exit 0
