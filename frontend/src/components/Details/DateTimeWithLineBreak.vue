<template>
	<div>{{ timePartRef }}</div>
	<div>{{ datePartRef }}</div>
</template>
<script lang="ts" setup>
import { ref, watch } from 'vue'
import { formatTimestamp } from '../../utils/format'
type Props = {
	dateTime: string
}
const props = defineProps<Props>()
const datePartRef = ref('')
const timePartRef = ref('')

watch(
	props,
	newProps => {
		const formattedDate = formatTimestamp(newProps.dateTime)
		const [datePart, timePart] = formattedDate.split(',')
		datePartRef.value = datePart
		timePartRef.value = timePart
	},
	{ immediate: true }
)
</script>
