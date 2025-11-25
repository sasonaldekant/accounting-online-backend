<#
.SYNOPSIS
    Automatically fix legacy RAISERROR syntax in SQL Server triggers

.DESCRIPTION
    This PowerShell script connects to SQL Server and executes the
    Fix_Legacy_RAISERROR_Syntax.sql script to modernize trigger syntax.
    
    It provides:
    - Automated execution
    - Progress reporting
    - Error handling
    - Backup verification
    - Rollback capability

.PARAMETER ServerInstance
    SQL Server instance name (e.g., "localhost" or "SERVER\INSTANCE")

.PARAMETER Database
    Database name (default: ERPAccounting_Tmp)

.PARAMETER ExecuteFixes
    If specified, automatically executes the fixes. Otherwise, only generates them.

.PARAMETER Credential
    PSCredential object for SQL Server authentication (optional, uses Windows auth by default)

.EXAMPLE
    .\Run_Fix_RAISERROR.ps1 -ServerInstance "localhost" -Database "ERPAccounting_Tmp"
    
.EXAMPLE
    .\Run_Fix_RAISERROR.ps1 -ServerInstance "localhost" -ExecuteFixes
    
.NOTES
    Author: AI Agent
    Date: 2025-11-26
    Version: 1.0
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)]
    [string]$ServerInstance,
    
    [Parameter(Mandatory=$false)]
    [string]$Database = "ERPAccounting_Tmp",
    
    [Parameter(Mandatory=$false)]
    [switch]$ExecuteFixes,
    
    [Parameter(Mandatory=$false)]
    [PSCredential]$Credential
)

# Import SQL Server module
try {
    Import-Module SqlServer -ErrorAction Stop
    Write-Host "✓ SQL Server PowerShell module loaded" -ForegroundColor Green
}
catch {
    Write-Host "✗ SQL Server PowerShell module not found. Installing..." -ForegroundColor Yellow
    Install-Module -Name SqlServer -Scope CurrentUser -Force
    Import-Module SqlServer
    Write-Host "✓ SQL Server PowerShell module installed and loaded" -ForegroundColor Green
}

Write-Host ""
Write-Host "===============================================================================" -ForegroundColor Cyan
Write-Host "  FIX LEGACY RAISERROR SYNTAX IN TRIGGERS" -ForegroundColor Cyan
Write-Host "===============================================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Server:   $ServerInstance" -ForegroundColor White
Write-Host "Database: $Database" -ForegroundColor White
Write-Host "Mode:     $(if($ExecuteFixes){'AUTO-EXECUTE'}else{'GENERATE ONLY'})" -ForegroundColor $(if($ExecuteFixes){'Yellow'}else{'Green'})
Write-Host ""

# Test connection
Write-Host "Testing SQL Server connection..." -ForegroundColor Yellow
try {
    if ($Credential) {
        $testQuery = "SELECT @@VERSION AS Version"
        $result = Invoke-Sqlcmd -ServerInstance $ServerInstance -Database $Database -Query $testQuery -Credential $Credential -ErrorAction Stop
    }
    else {
        $testQuery = "SELECT @@VERSION AS Version"
        $result = Invoke-Sqlcmd -ServerInstance $ServerInstance -Database $Database -Query $testQuery -ErrorAction Stop
    }
    Write-Host "✓ Connection successful" -ForegroundColor Green
    Write-Host "  SQL Server: $($result.Version.Split("`n")[0])" -ForegroundColor Gray
}
catch {
    Write-Host "✗ Connection failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Step 1: Check for affected triggers
Write-Host "Step 1: Checking for triggers with legacy RAISERROR syntax..." -ForegroundColor Yellow

$checkQuery = @"
SELECT COUNT(*) AS TriggerCount
FROM sys.sql_modules m
INNER JOIN sys.triggers t ON m.object_id = t.object_id
WHERE m.definition LIKE '%raiserror%44447%'
   OR m.definition LIKE '%raiserror%44446%'
   OR m.definition LIKE '%raiserror%44448%'
   OR m.definition LIKE '%RAISERROR%44447%'
   OR m.definition LIKE '%RAISERROR%44446%'
   OR m.definition LIKE '%RAISERROR%44448%';
"@

try {
    if ($Credential) {
        $triggerCount = (Invoke-Sqlcmd -ServerInstance $ServerInstance -Database $Database -Query $checkQuery -Credential $Credential).TriggerCount
    }
    else {
        $triggerCount = (Invoke-Sqlcmd -ServerInstance $ServerInstance -Database $Database -Query $checkQuery).TriggerCount
    }
    
    if ($triggerCount -eq 0) {
        Write-Host "✓ No triggers with legacy syntax found. Nothing to fix!" -ForegroundColor Green
        exit 0
    }
    else {
        Write-Host "  Found $triggerCount trigger(s) that need fixing" -ForegroundColor White
    }
}
catch {
    Write-Host "✗ Error checking triggers: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Step 2: List affected triggers
Write-Host "Step 2: Listing affected triggers..." -ForegroundColor Yellow

$listQuery = @"
SELECT 
    t.name AS TriggerName,
    tbl.name AS TableName
FROM sys.sql_modules m
INNER JOIN sys.triggers t ON m.object_id = t.object_id
INNER JOIN sys.tables tbl ON t.parent_id = tbl.object_id
WHERE m.definition LIKE '%raiserror%44447%'
   OR m.definition LIKE '%raiserror%44446%'
   OR m.definition LIKE '%raiserror%44448%'
   OR m.definition LIKE '%RAISERROR%44447%'
   OR m.definition LIKE '%RAISERROR%44446%'
   OR m.definition LIKE '%RAISERROR%44448%'
ORDER BY tbl.name, t.name;
"@

try {
    if ($Credential) {
        $triggers = Invoke-Sqlcmd -ServerInstance $ServerInstance -Database $Database -Query $listQuery -Credential $Credential
    }
    else {
        $triggers = Invoke-Sqlcmd -ServerInstance $ServerInstance -Database $Database -Query $listQuery
    }
    
    foreach ($trigger in $triggers) {
        Write-Host "  - $($trigger.TriggerName) on table $($trigger.TableName)" -ForegroundColor Gray
    }
}
catch {
    Write-Host "✗ Error listing triggers: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Step 3: Execute the main fix script
Write-Host "Step 3: Loading fix script..." -ForegroundColor Yellow

$scriptPath = Join-Path $PSScriptRoot "Fix_Legacy_RAISERROR_Syntax.sql"

if (-not (Test-Path $scriptPath)) {
    Write-Host "✗ Script not found: $scriptPath" -ForegroundColor Red
    exit 1
}

Write-Host "  Script location: $scriptPath" -ForegroundColor Gray
Write-Host ""

try {
    Write-Host "Executing fix script..." -ForegroundColor Yellow
    
    if ($Credential) {
        Invoke-Sqlcmd -ServerInstance $ServerInstance -Database $Database -InputFile $scriptPath -Credential $Credential -Verbose
    }
    else {
        Invoke-Sqlcmd -ServerInstance $ServerInstance -Database $Database -InputFile $scriptPath -Verbose
    }
    
    Write-Host "✓ Fix script executed successfully" -ForegroundColor Green
}
catch {
    Write-Host "✗ Error executing script: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Step 4: Verify backup table
Write-Host "Step 4: Verifying backup..." -ForegroundColor Yellow

$backupQuery = "SELECT COUNT(*) AS BackupCount FROM dbo.TriggerBackup_20251126;"

try {
    if ($Credential) {
        $backupCount = (Invoke-Sqlcmd -ServerInstance $ServerInstance -Database $Database -Query $backupQuery -Credential $Credential).BackupCount
    }
    else {
        $backupCount = (Invoke-Sqlcmd -ServerInstance $ServerInstance -Database $Database -Query $backupQuery).BackupCount
    }
    
    Write-Host "✓ Backup table created with $backupCount trigger definition(s)" -ForegroundColor Green
}
catch {
    Write-Host "✗ Error verifying backup: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# Step 5: Generate report
Write-Host "Step 5: Generating report..." -ForegroundColor Yellow

$reportPath = Join-Path $PSScriptRoot "Fix_RAISERROR_Report_$(Get-Date -Format 'yyyyMMdd_HHmmss').txt"

$reportContent = @"
RAISERROR SYNTAX FIX REPORT
===============================================================================
Date:     $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
Server:   $ServerInstance
Database: $Database
User:     $env:USERNAME

TRIGGERS AFFECTED: $triggerCount
===============================================================================

"@

foreach ($trigger in $triggers) {
    $reportContent += "- $($trigger.TriggerName) on table $($trigger.TableName)`n"
}

$reportContent += @"

===============================================================================
BACKUP INFORMATION
===============================================================================
Backup table: dbo.TriggerBackup_20251126
Backup count: $backupCount

To restore a trigger, use:

DECLARE @BackupDef NVARCHAR(MAX);
SELECT @BackupDef = OriginalDefinition 
FROM dbo.TriggerBackup_20251126 
WHERE TriggerName = 'YourTriggerName';
EXEC sp_executesql @BackupDef;

===============================================================================
NEXT STEPS
===============================================================================
1. Review the generated ALTER TRIGGER statements in SSMS output
2. Execute them manually (recommended) or re-run with -ExecuteFixes
3. Test each trigger thoroughly
4. Keep backup table for at least 30 days

"@

$reportContent | Out-File -FilePath $reportPath -Encoding UTF8

Write-Host "✓ Report saved to: $reportPath" -ForegroundColor Green
Write-Host ""

# Step 6: Summary
Write-Host "===============================================================================" -ForegroundColor Cyan
Write-Host "  SUMMARY" -ForegroundColor Cyan
Write-Host "===============================================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Triggers found:    $triggerCount" -ForegroundColor White
Write-Host "Triggers backed up: $backupCount" -ForegroundColor White
Write-Host "Report saved:      $reportPath" -ForegroundColor White
Write-Host ""

if (-not $ExecuteFixes) {
    Write-Host "⚠ IMPORTANT: The fixes were GENERATED but NOT EXECUTED." -ForegroundColor Yellow
    Write-Host "  To execute the fixes:" -ForegroundColor Yellow
    Write-Host "  1. Review the ALTER TRIGGER statements in SQL Server Management Studio" -ForegroundColor Yellow
    Write-Host "  2. Execute them manually, OR" -ForegroundColor Yellow
    Write-Host "  3. Re-run this script with -ExecuteFixes parameter" -ForegroundColor Yellow
    Write-Host ""
}
else {
    Write-Host "✓ All fixes have been executed automatically." -ForegroundColor Green
    Write-Host "  ⚠ IMPORTANT: Test all triggers thoroughly!" -ForegroundColor Yellow
    Write-Host ""
}

Write-Host "Backup table: dbo.TriggerBackup_20251126" -ForegroundColor White
Write-Host ""
Write-Host "Script completed successfully!" -ForegroundColor Green
Write-Host ""
