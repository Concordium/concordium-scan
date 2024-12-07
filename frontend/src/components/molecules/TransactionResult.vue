<template>
	<div class="inline">
		<StatusCircle
			:class="[
				'h-4 inline mr-2 text-theme-interactive align-text-top',
				{
					'text-theme-error': result.__typename === 'Rejected',
				},
			]"
		/>
		<span v-if="showText && breakpoint >= Breakpoint.MD">
			{{ result.__typename === 'Success' ? 'Success' : 'Rejected' }}
		</span>
	</div>
</template>

<script setup lang="ts">
import { useBreakpoint, Breakpoint } from '~/composables/useBreakpoint'
import type { TransactionResult } from '~/types/generated'
const { breakpoint } = useBreakpoint()
type Props = {
	result: TransactionResult
	iconClass?: string
	showText?: boolean
}

defineProps<Props>()
</script>
