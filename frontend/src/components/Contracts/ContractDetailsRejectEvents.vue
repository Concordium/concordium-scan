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
		<Pagination v-if="pageInfo" :page-info="pageInfo" :go-to-page="goToPage" />
	</div>
</template>

<script lang="ts" setup>
import { ContractRejectEvent, PageInfo } from '~~/src/types/generated'
import TransactionLink from '~~/src/components/molecules/TransactionLink.vue'
import RejectedReceive from '~/components/RejectionReason/Reasons/RejectedReceive.vue'
import Tooltip from '~~/src/components/atoms/Tooltip.vue'
import {
	convertTimestampToRelative,
	formatTimestamp,
} from '~~/src/utils/format'
import { PaginationTarget } from '~~/src/composables/usePagination'
import Pagination from '~/components/Pagination.vue'

const { NOW } = useDateNow()

type Props = {
	contractRejectEvents: ContractRejectEvent[]
	pageInfo: PageInfo
	goToPage: (page: PageInfo) => (target: PaginationTarget) => void
}
defineProps<Props>()
</script>
