<template>
	<button
		class="hover:text-theme-white"
		:class="
			selectedTab === tabId
				? 'selected rounded px-2 py-1 inline-block'
				: 'text-theme-faded px-2 py-1 cursor-pointer'
		"
		@click="() => handleOnClick(tabId)"
	>
		<slot />
	</button>
</template>

<script lang="ts" setup>
type Props = {
	tabId: string
	selectedTab: string
	onClick: (id: string) => void
}

const props = defineProps<Props>()

const handleOnClick = (tabId: string) => props.onClick(tabId)
</script>

<style scoped>
.selected {
	background-color: hsl(var(--color-primary));
}
</style>
