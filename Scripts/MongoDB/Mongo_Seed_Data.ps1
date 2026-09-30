
. "$PSScriptRoot\..\var-env-setting-Dev.ps1"

$SeedTag = 'dev-seed-notifications-by-nationalid-v1'


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

function Resolve-UserByNationalId {
    param([string]$NationalId)

    $escapedId = $NationalId.Replace("'", "''")
    $query = "SET QUOTED_IDENTIFIER ON;`r`nSELECT CAST([Id] AS NVARCHAR(36)) + N'|' + [FullName] FROM [Users] WHERE [NationalId] = N'$escapedId';"

    $utf8NoBom = New-Object System.Text.UTF8Encoding $false
    $tempFile = Join-Path $env:TEMP "mongo-seed-lookup-$([System.Guid]::NewGuid().ToString('N')).sql"
    [System.IO.File]::WriteAllText($tempFile, $query, $utf8NoBom)

    $containerFile = "/tmp/$([System.IO.Path]::GetFileName($tempFile))"
    docker cp $tempFile "sql1:$containerFile" | Out-Null
    Remove-Item $tempFile

    $result = docker exec sql1 /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$env:DB_PASSWORD" -C -d PMIS -h -1 -i $containerFile
    $exitCode = $LASTEXITCODE

    docker exec sql1 rm -f $containerFile 2>$null | Out-Null

    if ($exitCode -ne 0) {
        Write-Error "Lookup against sql1 failed (exit code $exitCode) - see output above."
        exit $exitCode
    }

    $line = ($result | Where-Object { $_ -and $_.Trim() -ne '' } | Select-Object -First 1)
    if ([string]::IsNullOrWhiteSpace($line)) {
        return $null
    }

    $parts = $line.Trim().Split('|', 2)
    if ($parts.Count -lt 2) {
        return $null
    }

    return @{ Id = $parts[0].Trim(); FullName = $parts[1].Trim() }
}

function Invoke-MongoScript {
    param(
        [string]$Script,
        [string]$Label
    )
    $tempFile = Join-Path $env:TEMP "mongo-seed-data-$([System.Guid]::NewGuid().ToString('N')).js"
    [System.IO.File]::WriteAllText($tempFile, $Script, (New-Object System.Text.UTF8Encoding $false))

    $containerFile = "/tmp/$([System.IO.Path]::GetFileName($tempFile))"
    docker cp $tempFile "mongodb:$containerFile" | Out-Null
    Remove-Item $tempFile

    Write-Host "Running $Label..."
    docker exec mongodb mongosh $env:ConnectionStrings__MongoDb --quiet $containerFile
    $exitCode = $LASTEXITCODE

    docker exec mongodb rm -f $containerFile 2>$null | Out-Null

    if ($exitCode -ne 0) {
        Write-Error "$Label failed (exit code $exitCode) - see output above."
        exit $exitCode
    }
}


function New-NotificationsForUser {
    while ($true) {
        $nationalId = Read-Host "Enter the National ID to seed notifications for (or press Enter to cancel)"
        if ([string]::IsNullOrWhiteSpace($nationalId)) {
            Write-Host "Cancelled - no National ID entered."
            return
        }

        $user = Resolve-UserByNationalId -NationalId $nationalId
        if ($null -eq $user) {
            Write-Host "No user found with National ID '$nationalId' - try again."
            continue
        }

        Write-Host "Found user: $($user.FullName) ($nationalId) - Id $($user.Id)."
        Seed-NotificationsForUser -UserId $user.Id -FullName $user.FullName -NationalId $nationalId
        return
    }
}

function Seed-NotificationsForUser {
    param(
        [string]$UserId,
        [string]$FullName,
        [string]$NationalId
    )

    $safeName = ($NationalId -replace "[^0-9]", '')
    $escapedName = $FullName.Replace("'", "\'")

    $types = @(
        @{ Type = 'RequestSubmitted';                Title = "تم إرسال الطلب REQ-SEED-$safeName-01"; Message = 'تم استلام طلبكم وهو الآن قيد المراجعة من قبل مكتب P3O.'; DaysAgo = 40; Read = $true;  Email = 'Sent';    Sms = 'Sent' }
        @{ Type = 'ReturnedForCorrection';            Title = "تم إعادة الطلب REQ-SEED-$safeName-03 لاستكمال المستندات"; Message = 'يرجى مراجعة ملاحظات مكتب P3O وإعادة إرسال الطلب.'; DaysAgo = 35; Read = $false; Email = 'Sent';    Sms = 'Failed' }
        @{ Type = 'Rejected';                         Title = "تم رفض الطلب REQ-SEED-$safeName-04"; Message = 'نأسف لإبلاغكم برفض الطلب. راجع سبب الرفض في تفاصيل الطلب.'; DaysAgo = 30; Read = $false; Email = 'Sent';    Sms = 'Sent' }
        @{ Type = 'AwaitingPayment';                  Title = "تم قبول الطلب REQ-SEED-$safeName-05 - بانتظار السداد"; Message = 'تم قبول طلبكم، يرجى إتمام إجراءات السداد لاستكمال التفعيل.'; DaysAgo = 25; Read = $true;  Email = 'Sent';    Sms = 'Sent' }
        @{ Type = 'PaymentConfirmed';                 Title = "تم تأكيد سداد الطلب REQ-SEED-$safeName-06"; Message = 'تم تأكيد استلام السداد، الرخصة الآن قيد التفعيل.'; DaysAgo = 20; Read = $false; Email = 'Pending'; Sms = 'Pending' }
        @{ Type = 'Activated';                        Title = "تم تفعيل الرخصة للطلب REQ-SEED-$safeName-07"; Message = 'تهانينا، تم تفعيل رخصتكم بنجاح وهي جاهزة للاستخدام.'; DaysAgo = 15; Read = $true;  Email = 'Sent';    Sms = 'Sent' }
        @{ Type = 'RequestCancelled';                 Title = "تم إلغاء الطلب REQ-SEED-$safeName-08"; Message = 'تم إلغاء هذا الطلب بناءً على طلبكم.'; DaysAgo = 12; Read = $true;  Email = 'Sent';    Sms = 'Sent' }
        @{ Type = 'NewRequestArrived';                 Title = "طلب جديد بانتظار المراجعة"; Message = 'تم إرسال طلب جديد ويتطلب المراجعة.'; DaysAgo = 10; Read = $false; Email = 'Sent';    Sms = 'Pending' }
        @{ Type = 'AccountChanged';                    Title = 'تم تعديل بيانات حساب مستخدم'; Message = 'تم تعديل بيانات أحد الحسابات بواسطة مسؤول النظام.'; DaysAgo = 8;  Read = $true;  Email = 'Sent';    Sms = 'Sent' }
        @{ Type = 'OwnAccountChanged';                 Title = 'تم تعديل بيانات حسابك'; Message = 'تم تعديل بيانات حسابك بنجاح.'; DaysAgo = 6;  Read = $false; Email = 'Sent';    Sms = 'Sent' }
        @{ Type = 'NewAuditLogEntry';                  Title = 'إدخال جديد في سجل العمليات'; Message = 'تم تسجيل عملية جديدة في سجل العمليات.'; DaysAgo = 5;  Read = $true;  Email = 'Sent';    Sms = 'Sent' }
        @{ Type = 'OverdueRequestsDigest';             Title = 'ملخص الطلبات المتأخرة'; Message = 'يوجد طلبات تجاوزت مدة الاستجابة المحددة وتحتاج للمراجعة.'; DaysAgo = 3;  Read = $false; Email = 'Sent';    Sms = 'Pending' }
        @{ Type = 'PaymentConfirmationNeededDigest';   Title = 'ملخص الطلبات بانتظار تأكيد السداد'; Message = 'يوجد طلبات بانتظار تأكيد السداد.'; DaysAgo = 1;  Read = $false; Email = 'Pending'; Sms = 'Pending' }
    )

    $docLines = @()
    foreach ($entry in $types) {
        $createdIso = (Get-Date).ToUniversalTime().AddDays(-$entry.DaysAgo).ToString('yyyy-MM-ddTHH:mm:ssZ')
        $readDateJs = if ($entry.Read) {
            $readIso = (Get-Date).ToUniversalTime().AddDays(-$entry.DaysAgo + 1).ToString('yyyy-MM-ddTHH:mm:ssZ')
            "new Date('$readIso')"
        } else { 'null' }
        $emailSentJs = if ($entry.Email -eq 'Sent') { "new Date('$createdIso')" } else { 'null' }
        $smsSentJs = if ($entry.Sms -eq 'Sent') { "new Date('$createdIso')" } else { 'null' }
        $smsErrorJs = if ($entry.Sms -eq 'Failed') { "'Invalid destination number'" } else { 'null' }

        $docLines += @"
  {
    userId: '$UserId', requestId: null, requestNumberSnapshot: null,
    type: '$($entry.Type)',
    title: '$($entry.Title -replace "'", "\'")',
    message: '$($entry.Message -replace "'", "\'")',
    isRead: $($entry.Read.ToString().ToLower()), readDate: $readDateJs, createdDate: new Date('$createdIso'),
    delivery: {
      email: { status: '$($entry.Email)', sentAt: $emailSentJs, error: null },
      sms:   { status: '$($entry.Sms)', sentAt: $smsSentJs, error: $smsErrorJs }
    },
    seedTag: SEED_TAG
  }
"@
    }

    $docsBody = $docLines -join ",`r`n"

    $script = @"
// Generated by Scripts\MongoDB\Mongo_Seed_Data.ps1 for National ID $NationalId ($escapedName).
const db = db.getSiblingDB('pmis');
const SEED_TAG = '$SeedTag';

const notificationDocs = [
$docsBody
];

const result = db.notifications.insertMany(notificationDocs);
print('Inserted ' + Object.keys(result.insertedIds).length + ' notifications documents for user $UserId.');
"@

    Invoke-MongoScript -Script $script -Label "notification seed for National ID '$NationalId'"
    Write-Host "Seeded $($types.Count) notifications (one per type) for '$FullName' ($NationalId)."
}


function Remove-SeededNotifications {
    $script = @"
const db = db.getSiblingDB('pmis');
const SEED_TAG = '$SeedTag';

const result = db.notifications.deleteMany({ seedTag: SEED_TAG });
print('Deleted ' + result.deletedCount + ' notifications documents.');
"@

    Invoke-MongoScript -Script $script -Label "notification seed undo"
    Write-Host "Removed all notifications seeded by this script."
}


$operation = Read-MenuChoice -Prompt "Which operation?" -Options @{
    "1" = "seed notifications for a user (by National ID)"
    "2" = "undo (remove notifications seeded by this script)"
}

switch ($operation) {
    "1" { New-NotificationsForUser }
    "2" { Remove-SeededNotifications }
}

Write-Host "Done."
exit 0
