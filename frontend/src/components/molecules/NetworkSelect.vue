<template>
	<div class="relative">
		<select
			class="hidden lg:block border-2 border-solid text-sm rounded-full align-middle uppercase ml-4 px-4 py-2 pr-8 appearance-none uppercase select"
			:class="`select--${selectedValue}`"
			@change="handleOnChange"
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
			class="animate-spin"
			:class="iconClasses"
			data-testid="network-spinner"
		/>
		<ChevronForwardIcon
			v-else
			class="select-chevron"
			:class="iconClasses"
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

const iconClasses =
	'h-4 w-4 absolute top-3 right-3 transition-colors select-icon'

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
.select {
	background: transparent;
	border-color: currentColor;
	outline-color: currentColor;
	outline-offset: 0;
	transition: color 0.3s ease, outline-offset 0.3s ease, outline 0.3s ease;
}
.select--mainnet,
.select--mainnet + svg {
	color: hsl(var(--color-interactive));
}

.select--testnet,
.select--testnet + svg {
	color: hsl(var(--color-info));
}

.select:focus {
	outline: solid 2px white;
	outline-offset: 2px;
	color: white;
}

.select:focus + svg {
	color: white;
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
