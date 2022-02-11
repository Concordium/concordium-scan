<template>
	<span>
		Smart contract instance
		<Contract :address="event.contractAddress" /> created from module
		<Hash :hash="event.moduleRef" /> with amount
		{{ convertMicroCcdToCcd(event.amount) }}Ͼ
	</span>
</template>

<script setup lang="ts">
import { convertMicroCcdToCcd } from '~/utils/format'
import Contract from '~/components/molecules/Contract.vue'
import Hash from '~/components/molecules/Hash.vue'
import type { ContractInitialized } from '~/types/generated'

type Props = {
	event: ContractInitialized
}

defineProps<Props>()
</script>
