locals {
  environment = terraform.workspace
  envs = {
      "test": {
          resource_group_name = "ccscan-env-test"
          azure_vm_size = "Standard_B4ms"
          container_repository_backend = "backend.test"
          public_host_name_mainnet_backend = "mainnet.test-api.ccdscan.io"
          public_host_name_testnet_backend = "testnet.test-api.ccdscan.io"
      }
  }

  vm_admin_user = "concNodeVMuser"

  postgres_hostname = "pg-ss"
  postgres_user = "postgres"
  postgres_password = "passwordFTB2021"

  # TODO: Secrets should probably go to the key vault instead
  vm_entrypoint_script = templatefile("${path.cwd}/entrypoint.tpl", {
    vm_user = local.vm_admin_user
    environment_name = local.environment
    container_repository_backend = local.envs[local.environment].container_repository_backend
    container_registry_password = "G+ne4o=OfLOnl54VkbeFYO0U+AB2xQRc"
    postgres_hostname = local.postgres_hostname
    postgres_user = local.postgres_user
    postgres_password = local.postgres_password
    cc_node_grpc_token = "FTBgrpc2021"
  })
}
