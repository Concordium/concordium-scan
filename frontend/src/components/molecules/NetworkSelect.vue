<template>
	<div
		class="hidden lg:block border-2 border-solid text-sm rounded-full align-middle uppercase ml-4 px-4 py-2"
		:class="`select-container select-container--${selectedValue} ${
			state === 'focused' ? 'select-container--focused' : ''
		}`"
	>
		<select
			class="appearance-none uppercase"
			:class="`select`"
			@change="handleOnChange"
			@focus="handleOnFocus"
			@blur="handleOnBlur"
		>
			<option :selected="selectedValue === 'mainnet'" value="mainnet">
				Mainnet
			</option>
			<option :selected="selectedValue === 'testnet'" value="testnet">
				Testnet
			</option>
		</select>
		<SpinnerIcon
			v-if="state === 'loading'"
			class="h-4 w-4 ml-2 align-top animate-spin"
			data-testid="network-spinner"
		/>
		<ChevronForwardIcon
			v-else
			class="h-4 w-4 ml-2 align-top select-chevron"
			data-testid="network-chevron"
		/>
	</div>
</template>

<script lang="ts" setup>
import { ref } from 'vue'
import SpinnerIcon from '~/components/icons/SpinnerIcon.vue'
import ChevronForwardIcon from '~/components/icons/ChevronForwardIcon.vue'

const selectedValue = ref(
	location.host.includes('testnet') ? 'testnet' : 'mainnet'
)

const state = ref('idle')

const handleOnFocus = () => {
	state.value = 'focused'
}

const handleOnBlur = () => {
	state.value = 'idle'
}

const handleOnChange = (event: Event) => {
	// compiler does not know if `EventTarget` has a `value` (for example if it is a div)
	const target = event.target as HTMLSelectElement

	state.value = 'loading'

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
.select-container {
	border-color: currentColor;
	outline-color: currentColor;
	outline-offset: 0;
	transition: color 0.3s ease, outline-offset 0.3s ease, outline 0.3s ease;
}
.select-container--mainnet {
	color: hsl(var(--color-interactive));
}

.select-container--testnet {
	color: hsl(var(--color-info));
}

.select-container--focused {
	outline: solid 2px white;
	outline-offset: 2px;
	color: white;
}

.select {
	background: transparent;
}

.select:focus {
	outline: 0;
}

.select option {
	background: initial;
	color: initial;
}

.select-chevron {
	/* Tailwind class .rotate-90 seems to do nothing */
	transform: rotate(90deg);
}
</style>
