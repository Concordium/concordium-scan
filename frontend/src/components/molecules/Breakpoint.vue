<template>
	<div
		class="breakpoint fixed bottom-3 left-3 rounded p-1 text-sm pointer-events-none opacity-50"
	>
		{{ Breakpoint[breakpoint] }}
	</div>
</template>

<script setup lang="ts">
import { useBreakpoint, Breakpoint } from '~/composables/useBreakpoint'

const { breakpoint } = useBreakpoint()
</script>

<style scoped>
.breakpoint {
	background: var(--color-input-bg);
}
</style>
