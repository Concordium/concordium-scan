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
    host_name                      = local.envs[local.environment].public_host_name_mainnet_backend
    require_sni                    = false
  }

  http_listener {
    name                           = "testnet-listener"
    frontend_ip_configuration_name = "app-gateway-feip"
    frontend_port_name             = "app-gateway-feport"
    protocol                       = "Https"
    ssl_certificate_name           = "ssl"
    host_name                      = local.envs[local.environment].public_host_name_testnet_backend
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
}

## -------------------------------------------------------------------------
## A bootstrap ssl-cert pfx must exist for the given environment in
## the key vault. Scripts for generating a bootstrap SSL cert and 
## push it to key vault is found here:
##
## ..\scripts\GenerateBootstrapSslCertAndPushToKeyVault.ps1
## -------------------------------------------------------------------------
data "azurerm_key_vault" "ccscan" {
  name                = "ccscan"
  resource_group_name = "ccscan-key-vault"
}

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
