<template>
	<div :class="$style.container">
		<input
			:class="$style.input"
			placeholder="Search for block or transaction &hellip;"
			type="search"
		/>
		<button :class="$style.button">
			<SearchIcon :class="$style.icon" />
		</button>
	</div>
</template>

<script lang="ts">
import { SearchIcon } from '@heroicons/vue/outline'

export default {
	components: {
		SearchIcon,
	},
	data() {
		return {
			searchValue: '',
		}
	},
	methods: {
		onSearch() {
			// console.log('Searched for:', this.searchValue)
		},
	},
}
</script>

<style module>
.container {
	@apply relative w-1/3;
}

.input {
	@apply rounded p-2 w-full focus:ring-2 focus:ring-pink-500 outline-none;
	background: var(--color-input-bg);
}

.input::placeholder {
	@apply italic;
	color: var(--color-input-placeholder);
}

.button {
	@apply absolute top-1/2 right-3;
	transform: translateY(-50%);
}

.icon {
	@apply h-5;
}
</style>
