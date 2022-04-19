import { ref, onMounted, onUnmounted } from 'vue'

/**
 * Hook to get a updating Date.Now() value
 * Returns current timestamp as Date, always updated at intervals
 */
export const useDateNow = () => {
	const NOW = ref(new Date())

	let updateInterval: NodeJS.Timeout

	const setTimestamp = () => {
		NOW.value = new Date()
	}

	onMounted(() => {
		updateInterval = setInterval(setTimestamp, 15000)
	})
	onUnmounted(() => {
		clearInterval(updateInterval)
	})

	return { NOW }
}
