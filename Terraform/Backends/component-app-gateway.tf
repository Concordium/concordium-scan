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
    port = 80
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
    # TODO: change to https and add ssl
    protocol                       = "Http"
    host_name                      = local.envs[local.environment].public_host_name_mainnet_backend
  }

  http_listener {
    name                           = "testnet-listener"
    frontend_ip_configuration_name = "app-gateway-feip"
    frontend_port_name             = "app-gateway-feport"
    # TODO: change to https and add ssl
    protocol                       = "Http"
    host_name                      = local.envs[local.environment].public_host_name_testnet_backend
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
}

resource "azurerm_network_interface_application_gateway_backend_address_pool_association" "vm" {
  network_interface_id    = azurerm_network_interface.vm.id
  ip_configuration_name   = "ipc-vm"
  backend_address_pool_id = azurerm_application_gateway.this.backend_address_pool[0].id
}
