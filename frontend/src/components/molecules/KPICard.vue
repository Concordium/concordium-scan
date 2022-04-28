<template>
	<div
		class="flex flex-col items-center h-full w-full my-4 relative cardShadow rounded-2xl shadow-2xl overflow-hidden bg-theme-background-primary-elevated"
	>
		<div class="text-theme-faded pt-4 w-72 text-center">
			<slot name="title"></slot>
		</div>

		<div
			class="text-xl md:text-3xl text-theme-interactive flex flex-row gap-2 items-center"
		>
			<div class="w-6 h-6 mr-2 text-theme-interactive">
				<slot name="icon"></slot>
			</div>
			<div class="numerical"><slot name="value"></slot></div>
			<div><slot name="unit"></slot></div>
			<Chip><slot name="chip"></slot></Chip>
		</div>
	</div>
</template>
<script lang="ts" setup>
import Chip from '~/components/atoms/Chip.vue'
</script>
