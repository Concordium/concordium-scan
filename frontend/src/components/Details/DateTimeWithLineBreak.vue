<template>
	<div>{{ timePartRef }}</div>
	<div>{{ datePartRef }}</div>
</template>
<script lang="ts" setup>
import { ref, watch } from 'vue'
type Props = {
	dateTime: string
}
const props = defineProps<Props>()
const datePartRef = ref('')
const timePartRef = ref('')

watch(
	props,
	newProps => {
		const datePart = new Intl.DateTimeFormat('default', {
			year: 'numeric',
			month: 'short',
			day: 'numeric',
		}).format(new Date(newProps.dateTime))
		const timePart = new Intl.DateTimeFormat('default', {
			hour: 'numeric',
			minute: 'numeric',
		}).format(new Date(newProps.dateTime))
		datePartRef.value = datePart
		timePartRef.value = timePart
	},
	{ immediate: true }
)
</script>
