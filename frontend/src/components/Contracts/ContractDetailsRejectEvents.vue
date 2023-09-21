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
						<div
							v-if="
								contractRejectEvent.rejectedEvent.__typename ===
								'RejectedReceive'
							"
						>
							<p>Reject Reason:</p>
							<p>
								{{ contractRejectEvent.rejectedEvent.rejectReason }}
							</p>
							<p>Receive Name:</p>
							<p>
								{{ contractRejectEvent.rejectedEvent.receiveName }}
							</p>
							<p>Message as HEX:</p>
							<p>
								{{ contractRejectEvent.rejectedEvent.messageAsHex }}
							</p>
							<p>Contract Address</p>
							<p>
								<ContractLink
									:address="
										contractRejectEvent.rejectedEvent.contractAddress.asString
									"
									:contract-address-index="
										contractRejectEvent.rejectedEvent.contractAddress.index
									"
									:contract-address-sub-index="
										contractRejectEvent.rejectedEvent.contractAddress.subIndex
									"
								/>
							</p>
						</div>
					</TableTd>
				</TableRow>
			</TableBody>
		</Table>
		<Pagination v-if="pageInfo" :page-info="pageInfo" :go-to-page="goToPage" />
	</div>
</template>

<script lang="ts" setup>
import ContractLink from '~/components/molecules/ContractLink.vue'
import { ContractRejectEvent, PageInfo } from '~~/src/types/generated'
import TransactionLink from '~~/src/components/molecules/TransactionLink.vue'
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
