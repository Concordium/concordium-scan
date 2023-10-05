<template>
    <Tooltip
		:text="text"
		position="bottom"
		x="50%"
		y="50%"
		tooltip-position="absolute"
	>
        <QuestionMarkCircleIcon class="inline w-5 h-5" />
	</Tooltip>
</template>
<script lang="ts" setup>
import { QuestionMarkCircleIcon } from '@heroicons/vue/outline';
import Tooltip from '~~/src/components/atoms/Tooltip.vue'

type Props = {
    text: string
}
defineProps<Props>();

</script>
