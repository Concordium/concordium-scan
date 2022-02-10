resource "azurerm_public_ip" "app-gw" {
    name                = "app-gateway-pip"
    resource_group_name = azurerm_resource_group.this.name
    location            = azurerm_resource_group.this.location
    allocation_method   = "Dynamic"
    domain_name_label   = "${local.environment}-ccdscan-api"
  
    tags = {
      environment = local.environment
    }
}

resource "azurerm_application_gateway" "this" {
  name                = "app-gateway"
  resource_group_name = azurerm_resource_group.this.name
  location            = azurerm_resource_group.this.location

  sku {
    name     = "Standard_Small"
    tier     = "Standard"
    capacity = 1
  }

  gateway_ip_configuration {
    name      = "gateway-ip-configuration"
    subnet_id = azurerm_subnet.frontend.id
  }

  frontend_port {
    name = "app-gateway-feport"
    port = 443
  }

  frontend_ip_configuration {
    name                 = "app-gateway-feip"
    public_ip_address_id = azurerm_public_ip.app-gw.id
  }

  backend_address_pool {
    name = "app-gateway-backends"
  }

  backend_http_settings {
    name                  = "mainnet-http-settings"
    cookie_based_affinity = "Disabled"
    port                  = 5000
    protocol              = "Http"
    request_timeout       = 20
    probe_name            = "health-probe"
    pick_host_name_from_backend_address = true
  }

  backend_http_settings {
    name                  = "testnet-http-settings"
    cookie_based_affinity = "Disabled"
    port                  = 5001
    protocol              = "Http"
    request_timeout       = 20
    probe_name            = "health-probe"
    pick_host_name_from_backend_address = true
  }

  http_listener {
    name                           = "mainnet-listener"
    frontend_ip_configuration_name = "app-gateway-feip"
    frontend_port_name             = "app-gateway-feport"
    protocol                       = "Https"
    ssl_certificate_name           = "ssl"
    host_name                      = "mainnet.${local.envs[local.environment].domain_name}"
    require_sni                    = false
  }

  http_listener {
    name                           = "testnet-listener"
    frontend_ip_configuration_name = "app-gateway-feip"
    frontend_port_name             = "app-gateway-feport"
    protocol                       = "Https"
    ssl_certificate_name           = "ssl"
    host_name                      = "testnet.${local.envs[local.environment].domain_name}"
    require_sni                    = false
  }

  request_routing_rule {
    name                       = "mainnet-redirect"
    rule_type                  = "Basic"
    http_listener_name         = "mainnet-listener"
    backend_address_pool_name  = "app-gateway-backends"
    backend_http_settings_name = "mainnet-http-settings"
  }

  request_routing_rule {
    name                       = "testnet-redirect"
    rule_type                  = "Basic"
    http_listener_name         = "testnet-listener"
    backend_address_pool_name  = "app-gateway-backends"
    backend_http_settings_name = "testnet-http-settings"
  }

  probe {
    name = "health-probe"
    protocol = "Http"
    path = "/api/health"
    interval = 30
    timeout = 30
    unhealthy_threshold = 3
    pick_host_name_from_backend_http_settings = true
  }

  ssl_certificate {
      name = "ssl"
      data = data.azurerm_key_vault_secret.ssl-cert-data.value
      password = data.azurerm_key_vault_secret.ssl-cert-password.value
  }

  lifecycle {
    ignore_changes = [ssl_certificate]
  }
}

## -------------------------------------------------------------------------
## A bootstrap ssl-cert pfx must exist for the given environment in
## the key vault. Scripts for generating a bootstrap SSL cert and 
## push it to key vault is found here:
##
## ..\scripts\GenerateBootstrapSslCertAndPushToKeyVault.ps1
## -------------------------------------------------------------------------
data "azurerm_key_vault_secret" "ssl-cert-data" {
  name         = "${local.environment}-sslcert-pfx"
  key_vault_id = data.azurerm_key_vault.ccscan.id
}

data "azurerm_key_vault_secret" "ssl-cert-password" {
  name         = "${local.environment}-sslcert-pfx-password"
  key_vault_id = data.azurerm_key_vault.ccscan.id
}

resource "azurerm_network_interface_application_gateway_backend_address_pool_association" "vm" {
  network_interface_id    = azurerm_network_interface.vm.id
  ip_configuration_name   = "ipc-vm"
  backend_address_pool_id = azurerm_application_gateway.this.backend_address_pool[0].id
}

resource "azurerm_automation_account" "this" {
  name                = "automation-account-${local.environment}"
  location            = azurerm_resource_group.this.location
  resource_group_name = azurerm_resource_group.this.name

  sku_name = "Basic"

  tags = {
    environment = local.environment
  }
}

resource "azurerm_automation_module" "posh-acme" {
  name                    = "posh-acme"
  resource_group_name     = azurerm_resource_group.this.name
  automation_account_name = azurerm_automation_account.this.name

  module_link {
    uri = "https://www.powershellgallery.com/api/v2/package/Posh-ACME/4.12.0"
  }
}

resource "azurerm_automation_runbook" "renew-sslcert" {
  name                    = "automation-renew-sslcert"
  location                = azurerm_resource_group.this.location
  resource_group_name     = azurerm_resource_group.this.name
  automation_account_name = azurerm_automation_account.this.name
  log_verbose             = "false"
  log_progress            = "false"
  description             = "Renews the SSL certificate"
  runbook_type            = "PowerShell"

  content = file("${path.cwd}/content/runbook_renew_sslcert.ps1")

  tags = {
    environment = local.environment
  }
}

resource "azurerm_storage_account" "this" {
  name                     = "storageccscan${local.environment}"
  resource_group_name      = azurerm_resource_group.this.name
  location                 = azurerm_resource_group.this.location
  account_tier             = "Standard"
  account_replication_type = "LRS"

  tags = {
    environment = local.environment
  }
}

resource "azurerm_storage_container" "sslrenewal" {
  name                  = "sslrenewal"
  storage_account_name  = azurerm_storage_account.this.name
  container_access_type = "private"
}

resource "azurerm_automation_schedule" "sslrenewal" {
  name                    = "sslrenewal-automation-schedule"
  resource_group_name     = azurerm_resource_group.this.name
  automation_account_name = azurerm_automation_account.this.name
  frequency               = "Week"
  interval                = 1
  description             = "Schedule for running the SSL renewal runbook once every week."
  week_days               = ["Monday"]
  timezone               = "Etc/UTC"
}

resource "azurerm_automation_job_schedule" "this" {
  resource_group_name     = azurerm_resource_group.this.name
  automation_account_name = azurerm_automation_account.this.name
  schedule_name           = azurerm_automation_schedule.sslrenewal.name
  runbook_name            = azurerm_automation_runbook.renew-sslcert.name

  # Due to a bug in the implementation of Runbooks in Azure, the parameter names need to be specified in lowercase only. See: "https://github.com/Azure/azure-sdk-for-go/issues/4780" for more information.
  parameters = {
    contactemail = "martin@fintech.builders"
    domainname   = local.envs[local.environment].domain_name
    environmentname = local.environment
    resourcegroupname = azurerm_resource_group.this.name
    applicationgatewayname = azurerm_application_gateway.this.name
    applicationgatewaysslname = azurerm_application_gateway.this.ssl_certificate[0].name
    storageaccountname = azurerm_storage_account.this.name
    storageaccountkey = azurerm_storage_account.this.primary_access_key
    storagecontainer = azurerm_storage_container.sslrenewal.name
  }
}

locals {
  # HACK: With the current version of the provider (v2.94.0) it is not possible to create the automation account
  #       with a system assigned identity. Therefore this must be set manually after creation. Copy the principal
  #       ID here afterwards and run again to set access permissions.
  #       (https://github.com/hashicorp/terraform-provider-azurerm/issues/11503)
  #       (https://github.com/hashicorp/terraform-provider-azurerm/pull/15072) seems this will be added in v2.96.0
#  automation_account_system_assigned_user_identity_principal_id = "df547db4-6f83-41b5-ad09-f871e792e9a5"
  automation_account_system_assigned_user_identity_principal_id = "df547db4-6f83-41b5-ad09-f871e792e9a5"
}

resource "azurerm_role_assignment" "automation-account-key-vault-sslcert-pfx-password" {
  # HACK: For some reason the id (or versionless_id) of the secret is not valid as scope for role assignments (https://github.com/hashicorp/terraform-provider-azurerm/issues/11529)
  scope                = "${data.azurerm_key_vault_secret.ssl-cert-password.key_vault_id}/secrets/${data.azurerm_key_vault_secret.ssl-cert-password.name}"  
  role_definition_name = "Key Vault Secrets User"
  principal_id         = local.automation_account_system_assigned_user_identity_principal_id
}

resource "azurerm_role_assignment" "automation-account-key-vault-sslcert-pfx" {
  # HACK: For some reason the id (or versionless_id) of the secret is not valid as scope for role assignments (https://github.com/hashicorp/terraform-provider-azurerm/issues/11529)
  scope                = "${data.azurerm_key_vault_secret.ssl-cert-data.key_vault_id}/secrets/${data.azurerm_key_vault_secret.ssl-cert-data.name}"  
  role_definition_name = "Key Vault Secrets Officer"
  principal_id         = local.automation_account_system_assigned_user_identity_principal_id
}

data "azurerm_key_vault_secret" "godaddy-dns-api-key" {
  name         = "godaddy-dns-api-key"
  key_vault_id = data.azurerm_key_vault.ccscan.id
}

data "azurerm_key_vault_secret" "godaddy-dns-api-key-secret" {
  name         = "godaddy-dns-api-key-secret"
  key_vault_id = data.azurerm_key_vault.ccscan.id
}

resource "azurerm_role_assignment" "automation_account_key_vault_godaddy_dns_api_key" {
  # HACK: For some reason the id (or versionless_id) of the secret is not valid as scope for role assignments (https://github.com/hashicorp/terraform-provider-azurerm/issues/11529)
  scope                = "${data.azurerm_key_vault_secret.godaddy-dns-api-key.key_vault_id}/secrets/${data.azurerm_key_vault_secret.godaddy-dns-api-key.name}"  
  role_definition_name = "Key Vault Secrets User"
  principal_id         = local.automation_account_system_assigned_user_identity_principal_id
}

resource "azurerm_role_assignment" "automation-account-key-vault-godaddy-dns-api-key-secret" {
  # HACK: For some reason the id (or versionless_id) of the secret is not valid as scope for role assignments (https://github.com/hashicorp/terraform-provider-azurerm/issues/11529)
  scope                = "${data.azurerm_key_vault_secret.godaddy-dns-api-key-secret.key_vault_id}/secrets/${data.azurerm_key_vault_secret.godaddy-dns-api-key-secret.name}"  
  role_definition_name = "Key Vault Secrets User"
  principal_id         = local.automation_account_system_assigned_user_identity_principal_id
}

resource "azurerm_role_assignment" "automation-account-app-gateway" {
  # HACK: For some reason the id (or versionless_id) of the secret is not valid as scope for role assignments (https://github.com/hashicorp/terraform-provider-azurerm/issues/11529)
  scope                = azurerm_application_gateway.this.id
  role_definition_name = "Managed Application Operator Role"
  principal_id         = local.automation_account_system_assigned_user_identity_principal_id
}
