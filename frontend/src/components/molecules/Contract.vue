<template>
	<span class="numerical">
		<ChipIcon class="h-4 inline align-baseline" :class="iconClass" />
		{{ `<${address.index}, ${address.subIndex}>` }}
	</span>
</template>

<script setup lang="ts">
import { ChipIcon } from '@heroicons/vue/solid/index.js'
import type { ContractAddress } from '~/types/generated'

type Props = {
	address: ContractAddress
	iconClass?: string
}

defineProps<Props>()
</script>
