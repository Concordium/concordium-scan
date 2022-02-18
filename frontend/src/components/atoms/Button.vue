<template>
	<button
		class="bg-theme-button-primary px-8 py-3"
		:class="{
			'cursor-not-allowed': props.disabled,
			'bg-theme-button-primary-disabled': props.disabled,
			'hover:bg-theme-button-primary-hover': !props.disabled,
			'rounded-lg': !props.groupPosition,
			'rounded-lg-l': props.groupPosition === 'first',
			'rounded-none': props.groupPosition === 'middle',
			'rounded-lg-r': props.groupPosition === 'last',
		}"
		:disabled="props.disabled"
		@click="handleOnClick"
	>
		<slot />
	</button>
</template>

<script lang="ts" setup>
type Props = {
	disabled?: boolean
	groupPosition?: 'first' | 'middle' | 'last'
	onClick?: () => void
}

const props = defineProps<Props>()

const handleOnClick = () => {
	if (!props.disabled) {
		props.onClick?.()
	}
}
</script>
