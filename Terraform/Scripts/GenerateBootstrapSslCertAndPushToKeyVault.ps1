param(
    [Parameter(Mandatory=$true, HelpMessage="The e-mail address to send with the request for the certificate")][String] $contactEmail,
    [Parameter(Mandatory=$true, HelpMessage="The password on the resulting PFX file")][String] $pfxPassword,
    [Parameter(Mandatory=$true, HelpMessage="The domain name to generate the SSL cert for. Enter the exact domain name, as the wild-card will be added automatically.")][String] $domain,
    [Parameter(Mandatory=$true, HelpMessage="The name of the environment (dev, test, prod, etc...). Used as prefix for the names of the secrets stored in Azure Key Vault.")][String] $environmentName)

Import-Module Posh-ACME

# Generate certificate
$goDaddyApiKey = az keyvault secret show --vault-name ccscan -n godaddy-dns-api-key --query "value" --output tsv
$goDaddySecret = az keyvault secret show --vault-name ccscan -n godaddy-dns-api-key-secret --query "value" --output tsv

$pArgs = @{
    GDKey = $goDaddyApiKey
    GDSecretSecure = ConvertTo-SecureString $goDaddySecret -AsPlainText -Force
}
New-PACertificate "*.${domain}", "${domain}" -AcceptTOS -Contact $contactEmail -Plugin GoDaddy -PluginArgs $pArgs -PfxPass $pfxPassword -Verbose -Force

# Push result to Azure Key Vault:
$result = Get-PACertificate *.dev-api.ccdscan.io | 
    Select-Object -Property PfxFile, @{name='Expires'; expression={$_.NotAfter.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssK")}}, @{name='NotBefore'; expression={$_.NotBefore.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssK")}}

az keyvault secret set --vault-name ccscan --name "${environmentName}-sslcert-pfx" --file $result.PfxFile --encoding base64 --not-before $result.NotBefore --expires $result.Expires
az keyvault secret set --vault-name ccscan --name "${environmentName}-sslcert-pfx-password" --value $pfxPassword 