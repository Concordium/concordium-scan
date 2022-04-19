<template>
	<div class="inline-block">
		<UserIcon class="h-4 inline align-text-top" :class="iconClass" />
		<LinkButton class="px-2" @click="() => handleOnClick(id)">
			{{ id }}
		</LinkButton>
	</div>
</template>

<script setup lang="ts">
import { UserIcon } from '@heroicons/vue/solid/index.js'
import { useDrawer } from '~/composables/useDrawer'
import LinkButton from '~/components/atoms/LinkButton.vue'

type Props = {
	id: number
	iconClass?: string
}

defineProps<Props>()

const drawer = useDrawer()

const handleOnClick = (bakerId: number) => {
	drawer.push({ entityTypeName: 'baker', bakerId })
}
</script>
