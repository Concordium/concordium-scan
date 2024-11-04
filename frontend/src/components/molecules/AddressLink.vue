<template>
	<ContractLink
		v-if="address.__typename === 'ContractAddress'"
		:address="address.asString"
		:contract-address-index="address.index"
		:contract-address-sub-index="address.subIndex"
	/>
	<AccountLink
		v-else-if="address.__typename === 'AccountAddress'"
		:address="address.asString"
	/>
</template>
<script lang="ts" setup>
import AccountLink from './AccountLink.vue'
import ContractLink from './ContractLink.vue'
import type { Address } from '~/types/generated'
type Props = {
	address: Address
}
defineProps<Props>()
</script>
