locals {
  environment = terraform.workspace
  envs = {
      "test": {
          resource_group_name = "ccscan-test"
          azure_vm_size = "Standard_B4ms"
      }
  }

  vm_admin_user = "concNodeVMuser"
  # TODO: Should probably also go to the key vault instead
  docker_registry_password = "G+ne4o=OfLOnl54VkbeFYO0U+AB2xQRc"

  vm_entrypoint_script = templatefile("${path.cwd}/entrypoint.tpl", {
    docker_registry_backend_container = "ccscan.azurecr.io/backend.test"
    docker_registry_password = local.docker_registry_password
    node_name = "FTB.Node.test"
    vm_user = local.vm_admin_user
    grpc_token = "FTBgrpc2021"
  })
}
