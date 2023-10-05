<template>
    <Tooltip
		:text="text"
		position="bottom"
		x="50%"
		y="50%"
		tooltip-position="absolute"
	>
		<span style="padding-left: 10px;">
            ?
        </span>
	</Tooltip>
</template>
<script lang="ts" setup>
import Tooltip from '~~/src/components/atoms/Tooltip.vue'

type Props = {
    text: string
}
defineProps<Props>();

</script>
