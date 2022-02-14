<template>
	<span>
		Smart contract instance <Contract :address="reason.contractAddress" /> does
		not exist
	</span>
</template>

<script setup lang="ts">
import Contract from '~/components/molecules/Contract.vue'
import type { InvalidContractAddress } from '~/types/generated'

type Props = {
	reason: InvalidContractAddress
}

defineProps<Props>()
</script>
