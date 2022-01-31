locals {
  environment = terraform.workspace
  envs = {
      "test": {
          resource_group_name = "ccscan-test"
          azure_vm_size = "Standard_B4ms"
          container_repository_backend = "backend.test"
          public_host_name_mainnet_backend = "test.api-mainnet.ccdscan.io"
          public_host_name_testnet_backend = "test.api-testnet.ccdscan.io"
      }
  }

  vm_admin_user = "concNodeVMuser"

  # TODO: Secrets should probably go to the key vault instead
  vm_entrypoint_script = templatefile("${path.cwd}/entrypoint.tpl", {
    vm_user = local.vm_admin_user
    environment_name = local.environment
    container_repository_backend = local.envs[local.environment].container_repository_backend
    container_registry_password = "G+ne4o=OfLOnl54VkbeFYO0U+AB2xQRc"
    postgres_password = "passwordFTB2021"
    cc_node_grpc_token = "FTBgrpc2021"
  })
}
