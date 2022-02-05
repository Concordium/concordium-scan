param(
    [Parameter(Mandatory=$true, HelpMessage="The e-mail address to send with the request for the certificate.")][String] $contactEmail,
	[Parameter(Mandatory=$true, HelpMessage="The domain name to generate the SSL cert for. Enter the exact domain name, as the wild-card will be added automatically.")][String] $domainName,
    [Parameter(Mandatory=$true, HelpMessage="The name of the environment (dev, test, prod, etc...). Used as prefix for the names of the secrets stored in Azure Key Vault.")][String] $environmentName,
    [Parameter(Mandatory=$true, HelpMessage="The name of the resource group where all used/modified resources reside.")][String] $resourceGroupName,
    [Parameter(Mandatory=$true, HelpMessage="The name of the application gateway within the given resource group.")][String] $applicationGatewayName,
    [Parameter(Mandatory=$true, HelpMessage="The name of the SSL certificate to update with the given application gateway.")][String] $applicationGatewaySslName,
    [Parameter(Mandatory=$true, HelpMessage="The name of the storage account (where state of automation goes)")][String] $storageAccountName,
    [Parameter(Mandatory=$true, HelpMessage="The storage account access key to use when accessing the storage account.")][String] $storageAccountKey,
    [Parameter(Mandatory=$true, HelpMessage="The name of the container within the storage account where blobs are read/written.")][String] $storageContainer)

$ErrorActionPreference = "Stop"

Write-Output "Connecting Azure Identity"
Connect-AzAccount -Identity | Out-Null

Write-Output "Creating storage context"
$storageContext = New-AzStorageContext -StorageAccountName $storageAccountName -StorageAccountKey $storageAccountKey 

$acmeWorkdir = ".\posh-acme"
$blobFileName = "posh-acme.zip"
$keyVaultName = "ccscan"

try {
    Write-Output "About to retreive ACME context blob from storage"
    Get-AzStorageBlobContent -container $storageContainer -Blob $blobFileName -Destination . -Context $storageContext -Force | Out-Null
	$acmeContextExists = $true
    
    Expand-Archive ".\${blobFileName}" -DestinationPath .
    Remove-Item -Force ".\${blobFileName}" | Out-Null

    Write-Output "Blob retreived"
}
catch [Microsoft.WindowsAzure.Commands.Storage.Common.ResourceNotFoundException]
{
    Write-Output "ACME context blob did not exist in storage, creating new context..."
    $acmeContextExists = $false
    New-Item -Path $acmeWorkdir -ItemType Directory -Force | Out-Null
}

$env:POSHACME_HOME = $acmeWorkdir
Import-Module Posh-ACME 
Set-PAServer LE_PROD

Write-Output "Getting PFX password from key vault"
$pfxPassSecure = Get-AzKeyVaultSecret -VaultName $keyVaultName -name "${environmentName}-sslcert-pfx-password" 

if ($acmeContextExists)
{
    Write-Output "Submitting renewal request..."
    Set-PAOrder $domainName -PfxPassSecure $pfxPassSecure.SecretValue
	$acmeResponse = Submit-Renewal $domainName -Verbose
    Write-Output "Renewal completed!"
}
else
{
	Write-Output "Configuring PA-account"
	# Note regards -UseAltPluginEncryption: 
	#   Needed since we will probably run on a new computer on every run
	#   (https://poshac.me/docs/v4/FAQ/#key-not-valid-for-use-in-specified-state)
	New-PAAccount -UseAltPluginEncryption -Contact $contactEmail -AcceptTOS | Out-Null

    Write-Output "Creating new certificate..."
	$goDaddyApiKey = Get-AzKeyVaultSecret -VaultName $keyVaultName -name "godaddy-dns-api-key" -AsPlainText
	$goDaddySecretSecure = Get-AzKeyVaultSecret -VaultName $keyVaultName -name "godaddy-dns-api-key-secret" 
	$plugInArgs = @{
		GDKey = $goDaddyApiKey
		GDSecretSecure =$goDaddySecretSecure.SecretValue
	}

    $acmeResponse = New-PACertificate $domainName, "*.${domainName}" -AcceptTOS -Contact $contactEmail -Plugin GoDaddy -PluginArgs $plugInArgs -PfxPassSecure $pfxPassSecure.SecretValue -Verbose 
    Write-Output "Certificate created!"
}

if ($acmeResponse -ne $NULL) {
	Write-Output "Updating Azure Key Vault..."
	$pfxBase64 = [convert]::ToBase64String((Get-Content -path $acmeResponse.PfxFile -Encoding byte))
	$pfxBase64Secure = ConvertTo-SecureString -String $pfxBase64 -AsPlainText -Force
	Set-AzKeyVaultSecret -VaultName $keyVaultName -Name "${environmentName}-sslcert-pfx" -SecretValue $pfxBase64Secure -Expires $acmeResponse.NotAfter.ToUniversalTime() -NotBefore $acmeResponse.NotBefore.ToUniversalTime() -ContentType "Base64 encoded PFX" | Out-Null

	Write-Output "Updating application gateway..."
	$appGW = Get-AzApplicationGateway -Name $applicationGatewayName -ResourceGroupName $resourceGroupName
	$cert = Set-AzApplicationGatewaySslCertificate -ApplicationGateway $AppGW -Name $applicationGatewaySslName -CertificateFile $acmeResponse.PfxFile -Password $pfxPassSecure.SecretValue

	Write-Output "Compressing ACME context and sending to blob storage..."
	$zipPath = ".\${blobFileName}"
	Compress-Archive -Path $acmeWorkdir -DestinationPath $zipPath -CompressionLevel Fastest -Force
	Set-AzStorageBlobContent -File $zipPath -Container  $storageContainer -Blob $blobFileName -BlobType Block -Context $storageContext -Force | Out-Null
	Remove-Item -Force $zipPath
}
else {
	Write-Output "There was no response from the Posh-ACME operation, so will not update anything!"
}

Write-Output "Done"
