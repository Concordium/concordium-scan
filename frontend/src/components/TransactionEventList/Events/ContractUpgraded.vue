<template>
	<span>
		Contract <Contract :address="event.contractAddress" /> upgraded module from
		<Hash :hash="event.from" /> to module <Hash :hash="event.to" />.
	</span>
</template>

<script setup lang="ts">
import { ContractUpgraded } from '~~/src/types/generated'
import Contract from '~/components/molecules/Contract.vue'
import Hash from '~/components/molecules/Hash.vue'

type Props = {
	event: ContractUpgraded
}
defineProps<Props>()
</script>
