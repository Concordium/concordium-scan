<template>
	<div class="inline">
		<StatusCircle
			:class="[
				'h-4 inline mr-2 text-theme-interactive align-text-top',
				{ 'text-theme-info': !finalized },
			]"
		/>
		<span v-if="showText && breakpoint >= Breakpoint.SM">
			{{ finalized ? 'Finalised' : 'Pending' }}
		</span>
	</div>
</template>

<script setup lang="ts">
import { useBreakpoint, Breakpoint } from '~/composables/useBreakpoint'
const { breakpoint } = useBreakpoint()
type Props = {
	finalized: boolean
	iconClass?: string
	showText?: boolean
}

defineProps<Props>()
</script>
