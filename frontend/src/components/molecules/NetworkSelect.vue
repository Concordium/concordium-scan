<template>
	<select
		class="hidden lg:block border-r-8 border-solid text-sm rounded-full align-middle uppercase ml-4 px-4 py-2 pt-3"
		:class="`select select--${selectedValue}`"
		@change="handleOnChange"
	>
		<option :selected="selectedValue === 'mainnet'" value="mainnet">
			Mainnet
		</option>
		<option :selected="selectedValue === 'testnet'" value="testnet">
			Testnet
		</option>
	</select>
</template>

<script lang="ts" setup>
import { ref } from 'vue'

const selectedValue = ref(
	location.host.includes('testnet') ? 'testnet' : 'mainnet'
)

const handleOnChange = (event: Event) => {
	// compiler does not know if `EventTarget` has a `value` (for example if it is a div)
	const target = event.target as HTMLSelectElement

	if (target.value === 'mainnet') {
		location.assign(
			location.protocol +
				'//' +
				location.host.replace('testnet.', '') +
				location.pathname
		)
	} else {
		location.assign(
			location.protocol + '//testnet.' + location.host + location.pathname
		)
	}
}
</script>

<style scoped>
.select {
	/* Tailwind class .border-transparent seems not to apply any styles */
	border-color: transparent;
}
.select--mainnet {
	background-color: hsl(var(--color-interactive));
	color: hsl(var(--color-interactive-dark));
}

.select--testnet {
	background-color: hsl(var(--color-error));
	color: hsl(var(--color-error-dark));
}
</style>
