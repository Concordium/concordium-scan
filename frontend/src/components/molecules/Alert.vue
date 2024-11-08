<template>
	<aside class="w-full border-l-4 flex rounded alert" role="alert">
		<div class="p-4 icon-container flex items-center">
			<InfoIcon class="w-6 h-6" />
		</div>

		<div class="p-4 flex flex-col items-center">
			<h3 class="font-bold w-full"><slot name="default" /></h3>
			<p class="w-full"><slot name="secondary" /></p>
		</div>
	</aside>
</template>

<script lang="ts" setup>
import InfoIcon from '~/components/icons/InfoIcon.vue'
</script>

<style scoped>
.alert {
	border-color: hsl(var(--color-info));
	background-color: var(--color-background-elevated);
}

.icon-container {
	color: hsl(var(--color-info));
}
</style>
