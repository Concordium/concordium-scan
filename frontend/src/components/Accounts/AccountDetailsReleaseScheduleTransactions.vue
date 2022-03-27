<template>
	<div>
		<Table>
			<TableHead>
				<TableRow>
					<TableTh>Amount (Ͼ)</TableTh>
					<TableTh>Releases in</TableTh>
					<TableTh v-if="breakpoint >= Breakpoint.LG">Transaction</TableTh>
					<TableTh v-if="breakpoint >= Breakpoint.XXL">Sender</TableTh>
				</TableRow>
			</TableHead>
			<TableBody>
				<TableRow
					v-for="scheduleItem in releaseScheduleItems"
					:key="scheduleItem.transaction.transactionHash"
				>
					<TableTd align="right" class="numerical">
						{{ convertMicroCcdToCcd(scheduleItem.amount) }}
					</TableTd>

					<TableTd>
						<Tooltip :text="formatTimestamp(scheduleItem.timestamp)">
							{{ convertTimestampToRelative(scheduleItem.timestamp, NOW) }}
						</Tooltip>
					</TableTd>
					<TableTd class="numerical">
						<TransactionLink
							:id="scheduleItem.transaction.id"
							:hash="scheduleItem.transaction.transactionHash"
						/>
					</TableTd>
					<TableTd v-if="breakpoint >= Breakpoint.XXL" class="numerical">
						<AccountLink
							:address="scheduleItem.transaction.senderAccountAddress"
						/>
					</TableTd>
				</TableRow>
			</TableBody>
		</Table>
		<Pagination
			v-if="pageInfo && (pageInfo.hasNextPage || pageInfo.hasPreviousPage)"
			:page-info="pageInfo"
			:go-to-page="goToPage"
		/>
	</div>
</template>

<script lang="ts" setup>
import Tooltip from '~/components/atoms/Tooltip.vue'
import {
	convertMicroCcdToCcd,
	formatTimestamp,
	convertTimestampToRelative,
} from '~/utils/format'
import { useBreakpoint, Breakpoint } from '~/composables/useBreakpoint'
import type { PaginationTarget } from '~/composables/usePagination'
import { useDateNow } from '~/composables/useDateNow'
import type { PageInfo, AccountReleaseScheduleItem } from '~/types/generated'

const { NOW } = useDateNow()
const { breakpoint } = useBreakpoint()

type Props = {
	releaseScheduleItems: AccountReleaseScheduleItem[]
	pageInfo: PageInfo
	goToPage: (page: PageInfo) => (target: PaginationTarget) => void
}
defineProps<Props>()
</script>
