# This script simplifies ARM deployment.

[CmdletBinding()]
Param(
  
  [Parameter(Mandatory=$True)]
  [string]$SubscriptionName,
  
  [Parameter(Mandatory=$True)]
  [string]$DeploymentPrefix,

  [Parameter(Mandatory=$True)]
  [string]$Location

)


$ErrorActionPreference = "Stop"

#deployment of Resource group assumes identical prefix. change here if desired
$RGName = "$DeploymentPrefix-rg"
$scriptDir = Split-Path $MyInvocation.MyCommand.Path

# Check if user is already signed into Azure
try {
    Get-AzureRmContext
} catch {
    Write-Error "Cannot get Azure context, login to Azure using 'Login-AzureRm'"
    Exit
}

Select-AzureRmSubscription -SubscriptionName $SubscriptionName
Write-Host "Selected subscription: $SubscriptionName"

# Find existing or deploy new Resource Group:
$rg = Get-AzureRmResourceGroup -Name $RGName -ErrorAction SilentlyContinue
if (-not $rg)
{
    New-AzureRmResourceGroup -Name "$RGName" -Location "$Location"
    Write-Host "New resource group deployed: $RGName"   
}
else{ Write-Host "Resource group found: $RGName"}

Write-host "$scriptDir/templates/iot.json"
#deploy resource group
 New-AzureRmResourceGroupDeployment -Verbose -Force -ErrorAction Stop `
    -Name "iot" `
    -ResourceGroupName $RGName `
    -TemplateFile "$scriptDir/templates/master.json" `
    -deploymentPrefix $DeploymentPrefix


Write-Host "Finished provisioning the solution"



