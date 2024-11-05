<template>
	<div class="relative w-min" :class="colorClass">
		<select
			class="border-2 border-solid text-sm rounded-full align-middle uppercase ml-4 px-4 py-2 pr-8 appearance-none uppercase select"
			@change="handleOnChange"
		>
			<option selected>{{ explorer.name }}</option>
			<option
				v-for="external in externals"
				:key="external[1]"
				:value="external[1]"
			>
				{{ external[0] }}
			</option>
		</select>
		<ChevronForwardIcon
			class="select-chevron h-4 w-4 absolute top-3 right-3 transition-colors pointer-events-none select-icon"
			data-testid="network-chevron"
		/>
	</div>
</template>

<script lang="ts" setup>
import ChevronForwardIcon from '~/components/icons/ChevronForwardIcon.vue'

const {
	public: { explorer },
} = useRuntimeConfig()
const externals = explorer.external
	.split(';')
	.map(external => external.split('@'))
const colorClass =
	explorer.name.toLowerCase().trim() === 'mainnet'
		? 'select--green'
		: 'select--blue'
const handleOnChange = (event: Event) => {
	const target = event.target as HTMLSelectElement
	location.assign(target.value)
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
.select--green,
.select--green + svg {
	color: hsl(var(--color-interactive));
}

.select--blue,
.select--blue + svg {
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
