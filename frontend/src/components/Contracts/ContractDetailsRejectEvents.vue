<template>
	<div>
		<Table>
			<TableHead>
				<TableRow>
					<TableTh>Transaction Hash</TableTh>
					<TableTh>Age</TableTh>
					<TableTh>Type</TableTh>
					<TableTh>Details</TableTh>
				</TableRow>
			</TableHead>
			<TableBody>
				<TableRow
					v-for="contractRejectEvent in contractRejectEvents"
					:key="contractRejectEvent"
				>
					<TableTd class="numerical">
						<TransactionLink :hash="contractRejectEvent.transactionHash" />
					</TableTd>
					<TableTd>
						<Tooltip :text="formatTimestamp(contractRejectEvent.blockSlotTime)">
							{{
								convertTimestampToRelative(
									contractRejectEvent.blockSlotTime,
									NOW
								)
							}}
						</Tooltip>
					</TableTd>
					<TableTd>
						{{ contractRejectEvent.rejectedEvent.__typename }}
					</TableTd>
					<TableTd>
						<RejectedReceive
							v-if="
								contractRejectEvent.rejectedEvent.__typename ===
								'RejectedReceive'
							"
							:reason="contractRejectEvent.rejectedEvent"
						/>
					</TableTd>
				</TableRow>
			</TableBody>
		</Table>
	</div>
</template>

<script lang="ts" setup>
import { ContractRejectEvent } from '~~/src/types/generated'
import TransactionLink from '~~/src/components/molecules/TransactionLink.vue'
import RejectedReceive from '~/components/RejectionReason/Reasons/RejectedReceive.vue'
import {
	convertTimestampToRelative,
	formatTimestamp,
} from '~~/src/utils/format'

const { NOW } = useDateNow()

type Props = {
	contractRejectEvents: ContractRejectEvent[]
}
defineProps<Props>()
</script>
