<template>
	<dl class="grid grid-cols-2 col-span-2 px-4">
		<dt id="blockstatistics-blocktime">
			Block time
			<Tooltip text="Time since last block">
				<InfoIcon class="h-4 w-4" />
			</Tooltip>
		</dt>
		<dd class="text-right mb-2" aria-labelledby="blockstatistics-blocktime">
			<span class="numerical">
				{{ formatSeconds(blockStatistics.blockTime) }} </span
			>s
		</dd>
		<dt
			v-if="blockStatistics.finalizationTime"
			id="blockstatistics-finalizationtime"
		>
			Finalization time
			<Tooltip
				text="Time before proof of finalization of this block was included in a subsequent block"
			>
				<InfoIcon class="h-4 w-4" />
			</Tooltip>
		</dt>
		<dd
			v-if="blockStatistics.finalizationTime"
			class="text-right mb-2"
			aria-labelledby="blockstatistics-finalizationtime"
		>
			<span class="numerical">
				{{ formatSeconds(blockStatistics.finalizationTime) }} </span
			>s
		</dd>
	</dl>
</template>

<script lang="ts" setup>
import InfoIcon from '~/components/icons/InfoIcon.vue'
import Tooltip from '~/components/atoms/Tooltip.vue'
import { formatSeconds } from '~/utils/format'
import type { BlockStatistics } from '~/types/generated'

type Props = {
	blockStatistics: BlockStatistics
}

defineProps<Props>()
</script>
