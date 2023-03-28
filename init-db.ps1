# Use Cross-platform Powershell, not Windows PS
param(
	[parameter(Mandatory=$false)]
	[string]$SqlLocalPort		= 14330,

	[parameter(Mandatory=$false)]
	[string]$SqlLocalHost		= "localhost",

	[parameter(Mandatory=$false)]
	[string]$SqlSAPassword		= "G0neFishing",

	[parameter(Mandatory=$false)]
	[string]$SqlDBName		= "Outbox",

	[parameter(Mandatory=$false)]
	[string]$SqlInitScriptPath	= "./sql/Init.sql"
)

Function Execute-SqlCommand
{
	[CmdletBinding()]
	Param
	(
	[Parameter(Mandatory=$true)]
	[string]$Sql,

	[Parameter(Mandatory=$false)]
	[string]$SqlDBname	= "master",

	[Parameter(Mandatory=$false)]
	[int]$SqlLoginTimeoutSeconds	= 5
	)

	sqlcmd -S "$SqlLocalHost,$SqlLocalPort" -U sa -P $SqlSAPassword -d $SqlDBName -b -l $SqlLoginTimeoutSeconds -Q $Sql # sqlcmd must be lowercased to run on linux
	if (-not $?) { 
		Write-Error "SqlCmd failed to execute sql: '$Sql'" -ErrorAction "continue"
		throw "SqlCmd failed to execute sql: '$Sql'"
	}
}

Function Execute-SqlFile
{
	Param ($file)
	sqlcmd -S "$SqlLocalHost,$SqlLocalPort" -U sa -P $SqlSAPassword -d $SqlDBName -b -i $file
	if (-not $?) { Write-Error "SqlCmd failed to execute file '$file'." -ErrorAction "stop" }
}


if ($PSVersionTable.PSEdition -ne "Core") { Write-Error "Sorry, only cross-platform Powershell Core is supported." -ErrorAction "stop"  } 

Write-Host "SQL alive:"
Test-Connection -TargetName $SqlLocalHost -TcpPort $SqlLocalPort

# Check if SQL is available
# The SQL can take time spinning up in a container so we give it a few attempts
$SqlMaxAttempts = 10
$SqlAttemptWaitSeconds = 5
for ($num = 1 ; $num -le $SqlMaxAttempts ; $num++)
{
	try {
		Execute-SqlCommand -SqlLoginTimeout 1 -Sql "select @@version"
		Write-Host "Sql Server is up and running!" -ForegroundColor green
		break
	}
	catch {
		Write-Error "SQL Server is not available at $SqlLocalHost,$SqlLocalPort. Make sure you start your service dependencies with docker-compose."

		if ($num -ge $SqlMaxAttempts) { # last attempt
			Write-Error "Cannot access SQL Server. Check Docker output:" -ErrorAction "continue"
			docker ps -a
			docker logs --tail all test-sql
			Write-Error "Cannot access SQL Server." -ErrorAction "stop"
		}
	}

	Write-Warning "Attempt $num. Waiting for $SqlAttemptWaitSeconds seconds..."
	Start-Sleep -Seconds $SqlAttemptWaitSeconds
}

$ErrorActionPreference = "stop"

Write-Warning "Dropping db $SqlDBName.."
$SqlDBDropQuery = "use master; if ((select name from sysdatabases where name='$SqlDBName') is not null) begin alter database [$SqlDBName] set single_user with rollback immediate; drop database [$SqlDBName]; end;"
Execute-SqlCommand $SqlDBDropQuery

Write-Warning "Initializing db $SqlDBName.."
$SqlDBInitQuery = "create database $SqlDBName;"
Execute-SqlCommand $SqlDBInitQuery

Execute-SqlFile -File $SqlInitScriptPath -SqlDBName $SqlDBName

Write-Host "Database $SqlDBName is initialized." -ForegroundColor green
