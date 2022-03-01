locals {
  environment = terraform.workspace
  envs = {
    "dev": {
        resource_group_name = "ccscan-env-dev"
        azure_vm_size = "Standard_D8s_v4"
        container_repository_backend = "backend.dev"
        domain_name = "dev-api.ccdscan.io"
        app_gateway_sku: {
          name     = "Standard_Small"
          tier     = "Standard"
          capacity = 1
        }
    }
    "test": {
        resource_group_name = "ccscan-env-test"
        azure_vm_size = "Standard_D8s_v4"
        container_repository_backend = "backend.test"
        domain_name = "test-api.ccdscan.io"
        app_gateway_sku: {
          name     = "Standard_Small"
          tier     = "Standard"
          capacity = 1
        }
    }
    "prod": {
        resource_group_name = "ccscan-env-prod"
        azure_vm_size = "Standard_D8s_v4"
        container_repository_backend = "backend.prod"
        domain_name = "api.ccdscan.io"
        app_gateway_sku: {
          name     = "Standard_Medium"
          tier     = "Standard"
          capacity = 1
        }
    }
  }

  vm_admin_user = "concNodeVMuser"

  vm_entrypoint_script = templatefile("${path.cwd}/content/entrypoint.tpl", {
    vm_user = local.vm_admin_user
    environment_name = local.environment
    container_repository_backend = local.envs[local.environment].container_repository_backend
    container_registry_username = data.azurerm_container_registry.ccscan.admin_username
    container_registry_password = data.azurerm_container_registry.ccscan.admin_password 
    postgres_user = "postgres"
    postgres_password = data.azurerm_key_vault_secret.postgres-password.value
    cc_node_auth_token = data.azurerm_key_vault_secret.ccnode-auth-token.value
  })
}
