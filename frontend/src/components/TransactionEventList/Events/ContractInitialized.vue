<template>
	<span>
		Smart contract instance
		<ContractLink
			:address="event.contractAddress.asString"
			:contract-address-index="event.contractAddress.index"
			:contract-address-sub-index="event.contractAddress.subIndex"
		/>
		created from module
		<ModuleLink :module-reference="event.moduleRef" />
		{{ convertMicroCcdToCcd(event.amount) }}Ͼ
	</span>
</template>

<script setup lang="ts">
import ContractLink from '../../molecules/ContractLink.vue'
import ModuleLink from '../../molecules/ModuleLink.vue'
import { convertMicroCcdToCcd } from '~/utils/format'
import type { ContractInitialized } from '~/types/generated'

type Props = {
	event: ContractInitialized
}

defineProps<Props>()
</script>
