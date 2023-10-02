<template>
	<TableHead>
		<TableRow>
			<TableTh>Transaction</TableTh>
			<TableTh>Age</TableTh>
			<TableTh>Type</TableTh>
			<TableTh>Details</TableTh>
		</TableRow>
	</TableHead>
	<TableBody>
		<TableRow
			v-for="moduleRejectEvent in moduleRejectEvents"
			:key="moduleRejectEvent"
		>
			<TableTd class="numerical">
				<TransactionLink :hash="moduleRejectEvent.transactionHash" />
			</TableTd>
			<TableTd>
				<Tooltip
					:text="
						convertTimestampToRelative(moduleRejectEvent.blockSlotTime, NOW)
					"
				>
					<DateTimeWithLineBreak :date-time="moduleRejectEvent.blockSlotTime" />
				</Tooltip>
			</TableTd>
			<TableTd>
				{{ moduleRejectEvent.rejectedEvent.__typename }}
			</TableTd>
			<TableTd>
				<div
					v-if="
						moduleRejectEvent.rejectedEvent.__typename === 'InvalidInitMethod'
					"
				>
					<div>Init Name:</div>
					<div>{{ moduleRejectEvent.rejectedEvent.initName }}</div>
					<div>Module Reference:</div>
					<div>
						<ModuleLink
							:module-reference="moduleRejectEvent.rejectedEvent.moduleRef"
						/>
					</div>
				</div>
				<div
					v-if="
						moduleRejectEvent.rejectedEvent.__typename ===
						'InvalidReceiveMethod'
					"
				>
					<div>Receive Name:</div>
					<div>{{ moduleRejectEvent.rejectedEvent.receiveName }}</div>
					<div>Module Reference:</div>
					<div>
						<ModuleLink
							:module-reference="moduleRejectEvent.rejectedEvent.moduleRef"
						/>
					</div>
				</div>
				<div
					v-if="
						moduleRejectEvent.rejectedEvent.__typename ===
						'ModuleHashAlreadyExists'
					"
				>
					<div>Module Reference:</div>
					<div>
						<ModuleLink
							:module-reference="moduleRejectEvent.rejectedEvent.moduleRef"
						/>
					</div>
				</div>
			</TableTd>
		</TableRow>
	</TableBody>
</template>

<script lang="ts" setup>
import DateTimeWithLineBreak from '../Details/DateTimeWithLineBreak.vue'
import { ModuleReferenceRejectEvent } from '~~/src/types/generated'
import ModuleLink from '~/components/molecules/ModuleLink.vue'
import TransactionLink from '~~/src/components/molecules/TransactionLink.vue'
import Tooltip from '~~/src/components/atoms/Tooltip.vue'
import { convertTimestampToRelative } from '~~/src/utils/format'

const { NOW } = useDateNow()

type Props = {
	moduleRejectEvents: ModuleReferenceRejectEvent[]
}
defineProps<Props>()
</script>
