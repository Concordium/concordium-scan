<template>
	<div class="flex flex-col items-center mb-4">
		<div class="text-sm text-theme-faded w-72 text-center">
			<slot name="title" />
		</div>

		<div
			class="text-xl md:text-3xl text-theme-interactive flex flex-row gap-2 items-center h-2/3"
		>
			<div class="mr-2 text-theme-interactive">
				<slot name="icon" />
			</div>
			<div class="numerical"><slot name="value" /></div>
			<div><slot name="unit" /></div>
			<Chip><slot name="chip" /></Chip>
		</div>
	</div>
</template>
<script lang="ts" setup>
import Chip from '~/components/atoms/Chip.vue'
</script>
