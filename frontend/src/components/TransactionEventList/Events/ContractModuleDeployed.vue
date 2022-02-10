<template>
	<span>
		Smart contract module was deployed with module reference
		{{ event.moduleRef }}
	</span>
</template>

<script setup lang="ts">
import type { ContractModuleDeployed } from '~/types/generated'

type Props = {
	event: ContractModuleDeployed
}

defineProps<Props>()
</script>
