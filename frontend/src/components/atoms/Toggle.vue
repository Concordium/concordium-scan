<template>
	<label>
		<slot />
		<input
			type="checkbox"
			:checked="checked"
			class="ml-2 toggle"
			@change="handleOnChange"
		/>
	</label>
</template>

<script lang="ts" setup>
type Props = {
	onToggle: (checked: boolean) => void
	checked: boolean
}

const props = defineProps<Props>()

const handleOnChange = (event: Event) => {
	// compiler does not know if `EventTarget` has a `value` (for example if it is a div)
	const target = event.target as HTMLInputElement
	props.onToggle(target.checked)
}
</script>

<style scoped>
.toggle {
	appearance: none;
	display: inline-block;
	width: 32px;
	height: 16px;
	border: solid 1px white;
	border-radius: 8px;
	position: relative;
	vertical-align: middle;
}

.toggle::after {
	content: '';
	display: block;
	height: 10px;
	width: 10px;
	border-radius: 50%;
	background-color: white;
	position: absolute;
	top: 2px;
	left: 2px;
	transform: translateX(0);
	transition: transform 0.3s;
}

.toggle:checked {
	background-color: hsl(var(--color-primary));
}

.toggle:checked::after {
	transform: translateX(calc(150% + 1px));
}
</style>
