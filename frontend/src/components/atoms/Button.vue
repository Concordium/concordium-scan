<template>
	<button
		class="bg-theme-button-primary"
		:class="{
			'px-8': !props.size,
			'py-3': !props.size,
			'py-0': props.size === 'sm',
			'px-4': props.size === 'sm',
			'cursor-not-allowed': props.disabled,
			'bg-theme-button-primary-disabled': props.disabled,
			'hover:bg-theme-button-primary-hover': !props.disabled,
			rounded: !props.groupPosition,
			first: props.groupPosition === 'first',
			middle: props.groupPosition === 'middle',
			last: props.groupPosition === 'last',
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
	size?: 'sm' | 'md'
	groupPosition?: 'first' | 'middle' | 'last'
	onClick?: () => void
}

const props = defineProps<Props>()

const handleOnClick = () => {
	if (!props.disabled) {
		props.onClick?.()
	}
}

const borderRadiusSize = props.size === 'sm' ? '4px' : '8px'
</script>

<style scoped>
.first {
	border-radius: v-bind(borderRadiusSize) 0 0 v-bind(borderRadiusSize);
}

.middle {
	border-radius: 0;
}

.last {
	border-radius: 0 v-bind(borderRadiusSize) v-bind(borderRadiusSize) 0;
}
</style>
