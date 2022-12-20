<template>
	<div class="bg-theme-background-table pt-2 px-2 pb-1 rounded-xl">
		<SortIcon class="w-3 h-3" />
		<select
			class="form-select bg-theme-background-table"
			:value="refVal"
			@input="handleOnChange"
		>
			<option
				class="bg-theme-background-table text-theme-white"
				:selected="refVal === NodeSortField.NodeName"
				:value="NodeSortField.NodeName"
			>
				Node Name
			</option>
			<option
				class="bg-theme-background-table text-theme-white"
				:selected="refVal === NodeSortField.ConsensusBakerId"
				:value="NodeSortField.ConsensusBakerId"
			>
				Baker Id
			</option>
			<option
				class="bg-theme-background-table text-theme-white"
				:selected="refVal === NodeSortField.Uptime"
				:value="NodeSortField.Uptime"
			>
				Uptime
			</option>
			<option
				class="bg-theme-background-table text-theme-white"
				:selected="refVal === NodeSortField.ClientVersion"
				:value="NodeSortField.ClientVersion"
			>
				Node Version
			</option>
			<option
				class="bg-theme-background-table text-theme-white"
				:selected="refVal === NodeSortField.AveragePing"
				:value="NodeSortField.AveragePing"
			>
				Avg. Ping
			</option>
			<option
				class="bg-theme-background-table text-theme-white"
				:selected="refVal === NodeSortField.PeersCount"
				:value="NodeSortField.PeersCount"
			>
				Peers
			</option>
			<option
				class="bg-theme-background-table text-theme-white"
				:selected="refVal === NodeSortField.FinalizedBlockHeight"
				:value="NodeSortField.FinalizedBlockHeight"
			>
				Fin. Length
			</option>
			<option
				class="bg-theme-background-table text-theme-white"
				:selected="refVal === NodeSortField.BlocksReceivedCount"
				:value="NodeSortField.BlocksReceivedCount"
			>
				Rec. Length
			</option>
		</select>
	</div>
</template>
<script lang="ts" setup>
import { toRef } from 'vue'
import { NodeSortField } from '~/types/generated'
import SortIcon from '~/components/icons/SortIcon.vue'
const emit = defineEmits(['update:modelValue'])

type Props = {
	modelValue: string
}

const props = defineProps<Props>()
const refVal = toRef(props, 'modelValue')

const handleOnChange = (event: Event) => {
	// compiler does not know if `EventTarget` has a `value` (for example if it is a div)
	const target = event.target as HTMLSelectElement
	emit('update:modelValue', target.value)
}
</script>
