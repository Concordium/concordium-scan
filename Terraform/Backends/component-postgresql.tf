resource "azurerm_postgresql_server" "this" {
  name                = local.postgres_hostname
  location            = azurerm_resource_group.this.location
  resource_group_name = azurerm_resource_group.this.name

  administrator_login          = local.postgres_user
  administrator_login_password = data.azurerm_key_vault_secret.postgres-password.value

  sku_name   = "B_Gen5_1"
  version    = "11"
  storage_mb = 131072

  backup_retention_days        = 7
  auto_grow_enabled            = true

  public_network_access_enabled    = true
  ssl_enforcement_enabled          = true
  ssl_minimal_tls_version_enforced = "TLS1_2"
  
  tags = {
    environment = local.environment
  }
}

resource "azurerm_postgresql_firewall_rule" "vm" {
  name                = "vm"
  resource_group_name = azurerm_resource_group.this.name
  server_name         = azurerm_postgresql_server.this.name
  start_ip_address    = azurerm_public_ip.vm.ip_address
  end_ip_address      = azurerm_public_ip.vm.ip_address
}

data "azurerm_key_vault_secret" "postgres-password" {
  name         = "${local.environment}-postgres-password"
  key_vault_id = data.azurerm_key_vault.ccscan.id
}

resource "azurerm_postgresql_configuration" "timescaledb" {
  name                = "shared_preload_libraries"
  resource_group_name = azurerm_resource_group.this.name
  server_name         = azurerm_postgresql_server.this.name
  value               = "timescaledb"

  provisioner "local-exec" {
    command = "az postgres server restart -g ${azurerm_resource_group.this.name} -n ${azurerm_postgresql_server.this.name}"
  }
} 
