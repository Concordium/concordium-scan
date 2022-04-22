<template>
	<div class="mb-4">
		<div class="text-xl">{{ title }}</div>
		<slot />
		<div v-if="hasMoreResults" class="text-theme-faded">
			&hellip; more than 3 results!
		</div>
	</div>
</template>

<script lang="ts" setup>
type Props = {
	title: string
	hasMoreResults: boolean
}

defineProps<Props>()
</script>
