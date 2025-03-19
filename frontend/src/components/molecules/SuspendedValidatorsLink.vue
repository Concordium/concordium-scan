<template>
	<div class="inline-block whitespace-nowrap">
		<LinkButton @blur="emitBlur" @click="() => handleOnClick()">
			<div text-class="text-theme-body">Suspended Validators</div>
		</LinkButton>
	</div>
</template>
<script lang="ts" setup>
import { useDrawer } from '~/composables/useDrawer'
import LinkButton from '~/components/atoms/LinkButton.vue'

const drawer = useDrawer()
const emit = defineEmits(['blur'])
const emitBlur = (newTarget: FocusEvent) => {
	emit('blur', newTarget)
}

const handleOnClick = () => {
	drawer.push({ entityTypeName: 'suspendedValidators' })
}
</script>
