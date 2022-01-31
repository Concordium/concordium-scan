resource "azurerm_resource_group" "this" {
  name     = local.envs[local.environment].resource_group_name
  location = "northeurope"
}

resource "azurerm_virtual_network" "this" {
  name                = "vnet-concNodeVM"
  address_space       = ["10.0.0.0/16"]
  location            = azurerm_resource_group.this.location
  resource_group_name = azurerm_resource_group.this.name
}

resource "azurerm_subnet" "this" {
  name                 = "snet-concNodeVM"
  resource_group_name  = azurerm_resource_group.this.name
  virtual_network_name = azurerm_virtual_network.this.name
  address_prefixes     = ["10.0.1.0/24"]
}

resource "azurerm_network_security_group" "this" {
  name                = "nsg-concNodeVM"
  location            = azurerm_resource_group.this.location
  resource_group_name = azurerm_resource_group.this.name

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

resource "azurerm_public_ip" "vm" {
  name                = "pip-concNodeVM"
  location            = azurerm_resource_group.this.location
  resource_group_name = azurerm_resource_group.this.name
  allocation_method   = "Static"
  domain_name_label   = "ftbccscantestnode"
}

resource "azurerm_network_interface" "vm" {
  name                = "nic-concNodeVM"
  location            = azurerm_resource_group.this.location
  resource_group_name = azurerm_resource_group.this.name

  ip_configuration {
    name                          = "ipc-concNodeVM"
    subnet_id                     = azurerm_subnet.this.id
    private_ip_address_allocation = "Dynamic"
    public_ip_address_id          = azurerm_public_ip.vm.id
  }
}

resource "azurerm_network_interface_security_group_association" "vm_nic" {
  network_interface_id      = azurerm_network_interface.vm.id
  network_security_group_id = azurerm_network_security_group.this.id
}

data "azurerm_ssh_public_key" "ssh_vm" {
  name                = "ssh-vm"
  resource_group_name = azurerm_resource_group.this.name
}

resource "azurerm_linux_virtual_machine" "vm" {
  name                            = "vm-concNodeVM"
  location                        = azurerm_resource_group.this.location
  resource_group_name             = azurerm_resource_group.this.name
  network_interface_ids           = [azurerm_network_interface.vm.id]
  size                            = local.envs[local.environment].azure_vm_size
  custom_data                     = base64encode(local.vm_entrypoint_script)
  computer_name                   = "vm-concNodeVM"
  admin_username                  = local.vm_admin_user
  disable_password_authentication = true
  
  os_disk {
    name                 = "disk-concNodeVM"
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

resource "azurerm_managed_disk" "vm_data" {
  name                 = "storage_disk_concNodeVM"
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