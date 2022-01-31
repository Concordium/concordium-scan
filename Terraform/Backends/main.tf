data "azurerm_ssh_public_key" "ssh_vm" {
  # ----------------------------------------------------------------------------------------------------------
  # NOTE: The SSH key for the given environment must exist before provisioning!
  #
  #       If it doesn't: Create the SSH-key in the Azure Portal in the resource group mentioned below.
  #       (remember to upload the private key part (pem file) as a secret to the Azure KeyVault for safe keeping)
  # ----------------------------------------------------------------------------------------------------------
  name                = "ssh-vm-${local.environment}"
  resource_group_name = "ccscan-ssh-keys"
}

resource "azurerm_resource_group" "this" {
  name     = local.envs[local.environment].resource_group_name
  location = "northeurope"

  tags = {
    environment = local.environment
  }
}

resource "azurerm_virtual_network" "this" {
  name                = "vnet"
  address_space       = ["10.0.0.0/16"]
  location            = azurerm_resource_group.this.location
  resource_group_name = azurerm_resource_group.this.name

  tags = {
    environment = local.environment
  }
}

resource "azurerm_subnet" "frontend" {
  name                 = "snet-frontend"
  resource_group_name  = azurerm_resource_group.this.name
  virtual_network_name = azurerm_virtual_network.this.name
  address_prefixes     = ["10.0.0.0/24"]
}

resource "azurerm_subnet" "backend" {
  name                 = "snet-backend"
  resource_group_name  = azurerm_resource_group.this.name
  virtual_network_name = azurerm_virtual_network.this.name
  address_prefixes     = ["10.0.1.0/24"]
}

resource "azurerm_network_security_group" "this" {
  name                = "nsg"
  location            = azurerm_resource_group.this.location
  resource_group_name = azurerm_resource_group.this.name

  tags = {
    environment = local.environment
  }

  security_rule {
    name                       = "SSH"
    priority                   = 1001
    direction                  = "Inbound"
    access                     = "Allow"
    protocol                   = "Tcp"
    source_port_range          = "*"
    destination_port_range     = "22"
    source_address_prefix      = "*"
    destination_address_prefix = "*"
  }

  security_rule {
    name                       = "Conc_P2P"
    priority                   = 1002
    direction                  = "Inbound"
    access                     = "Allow"
    protocol                   = "Tcp"
    source_port_range          = "*"
    destination_port_range     = "8888"
    source_address_prefix      = "*"
    destination_address_prefix = "*"
  }

  security_rule {
    name                       = "Conc_P2PTestnet"
    priority                   = 1003
    direction                  = "Inbound"
    access                     = "Allow"
    protocol                   = "Tcp"
    source_port_range          = "*"
    destination_port_range     = "18888"
    source_address_prefix      = "*"
    destination_address_prefix = "*"
  }

  security_rule {
    name                       = "Conc_GRPC"
    priority                   = 1004
    direction                  = "Inbound"
    access                     = "Allow"
    protocol                   = "Tcp"
    source_port_range          = "*"
    destination_port_range     = "10000"
    source_address_prefix      = "*"
    destination_address_prefix = "*"
  }

  security_rule {
    name                       = "Conc_GRPC_test"
    priority                   = 1005
    direction                  = "Inbound"
    access                     = "Allow"
    protocol                   = "Tcp"
    source_port_range          = "*"
    destination_port_range     = "10111"
    source_address_prefix      = "*"
    destination_address_prefix = "*"
  }

  security_rule {
    name                       = "POSTGRES"
    priority                   = 1006
    direction                  = "Inbound"
    access                     = "Allow"
    protocol                   = "Tcp"
    source_port_range          = "*"
    destination_port_range     = "5432"
    source_address_prefix      = "*"
    destination_address_prefix = "*"
  }

  security_rule {
    name                       = "http-server"
    priority                   = 1007
    direction                  = "Inbound"
    access                     = "Allow"
    protocol                   = "Tcp"
    source_port_range          = "*"
    destination_port_range     = "5000"
    source_address_prefix      = "*"
    destination_address_prefix = "*"
  }

  security_rule {
    name                       = "http-server_testnet"
    priority                   = 1008
    direction                  = "Inbound"
    access                     = "Allow"
    protocol                   = "Tcp"
    source_port_range          = "*"
    destination_port_range     = "5001"
    source_address_prefix      = "*"
    destination_address_prefix = "*"
  }
}

resource "azurerm_public_ip" "app-gw" {
  name                = "pip-app-gw"
  resource_group_name = azurerm_resource_group.this.name
  location            = azurerm_resource_group.this.location
  allocation_method   = "Dynamic"
  domain_name_label   = "${local.environment}-ccdscan-api"

  tags = {
    environment = local.environment
  }
}

resource "azurerm_public_ip" "vm" {
  name                = "pip-vm"
  location            = azurerm_resource_group.this.location
  resource_group_name = azurerm_resource_group.this.name
  allocation_method   = "Static"
  domain_name_label   = "${local.environment}-ccdscan-vm"

  tags = {
    environment = local.environment
  }
}

resource "azurerm_network_interface" "vm" {
  name                = "nic-vm"
  location            = azurerm_resource_group.this.location
  resource_group_name = azurerm_resource_group.this.name

  ip_configuration {
    name                          = "ipc-vm"
    subnet_id                     = azurerm_subnet.backend.id
    private_ip_address_allocation = "Dynamic"
    public_ip_address_id          = azurerm_public_ip.vm.id
  }

  tags = {
    environment = local.environment
  }
}

resource "azurerm_network_interface_security_group_association" "vm_nic" {
  network_interface_id      = azurerm_network_interface.vm.id
  network_security_group_id = azurerm_network_security_group.this.id
}

resource "azurerm_linux_virtual_machine" "vm" {
  name                            = "vm"
  location                        = azurerm_resource_group.this.location
  resource_group_name             = azurerm_resource_group.this.name
  network_interface_ids           = [azurerm_network_interface.vm.id]
  size                            = local.envs[local.environment].azure_vm_size
  custom_data                     = base64encode(local.vm_entrypoint_script)
  computer_name                   = "vm-concNodeVM"
  admin_username                  = local.vm_admin_user
  disable_password_authentication = true
  
  os_disk {
    name                 = "disk_vm_os"
    caching              = "ReadWrite"
    storage_account_type = "Premium_LRS"
  }

  source_image_reference {
    publisher = "Canonical"
    offer     = "0001-com-ubuntu-server-focal-daily"
    sku       = "20_04-daily-lts-gen2"
    version   = "latest"
  }

  admin_ssh_key {
    username   = local.vm_admin_user
    public_key = data.azurerm_ssh_public_key.ssh_vm.public_key
  }

  tags = {
    environment = local.environment
  }
}

# Changes to the disk resource that leads to replacement will NOT trigger a replacement of the VM
# However, the VM needs to be replaced when the data disk is replaced.
# This needs to be done manually by running apply with -replace:
# terraform apply -replace="azurerm_linux_virtual_machine.vm"
resource "azurerm_managed_disk" "vm_data" {
  name                 = "disk_vm_data_${azurerm_linux_virtual_machine.vm.virtual_machine_id}"
  location             = azurerm_resource_group.this.location
  resource_group_name  = azurerm_resource_group.this.name
  storage_account_type = "StandardSSD_LRS"
  create_option        = "Empty"
  disk_size_gb         = "128"

  tags = {
    environment = local.environment
  }
}

resource "azurerm_virtual_machine_data_disk_attachment" "vm_data" {
  managed_disk_id    = azurerm_managed_disk.vm_data.id
  virtual_machine_id = azurerm_linux_virtual_machine.vm.id
  lun                = "1"
  caching            = "ReadWrite"
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