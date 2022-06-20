
module "kv_shared_access_policy_app_webapi" {
  source                    = "git::https://github.com/Energinet-DataHub/geh-terraform-modules.git//azure/key-vault-access-policy?ref=6.1.0"

  key_vault_id              = data.azurerm_key_vault.kv_shared_resources.id
  app_identity              = module.app_webapi.identity.0
}

module "kv_shared_access_policy_func_entrypoint_messagearchive" {
  source                    = "git::https://github.com/Energinet-DataHub/geh-terraform-modules.git//azure/key-vault-access-policy?ref=6.1.0"

  key_vault_id              = data.azurerm_key_vault.kv_shared_resources.id
  app_identity              = module.func_entrypoint_messagearchive.identity.0
}


