<template>
	<div class="inline-block">
		<BakerIcon class="h-4 inline align-text-top" :class="iconClass" />
		<LinkButton class="numerical px-2" @click="() => handleOnClick(id)">
			{{ id }}
		</LinkButton>
	</div>
</template>

<script setup lang="ts">
import { useDrawer } from '~/composables/useDrawer'
import LinkButton from '~/components/atoms/LinkButton.vue'
import BakerIcon from '~/components/icons/BakerIcon.vue'

type Props = {
	id: number
	iconClass?: string
}

defineProps<Props>()

const drawer = useDrawer()

const handleOnClick = (bakerId: number) => {
	drawer.push({ entityTypeName: 'validator', bakerId })
}
</script>
