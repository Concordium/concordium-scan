<template>
	<div>
		<Table>
			<TableHead>
				<TableRow>
					<TableTh>Releases</TableTh>
					<TableTh align="right">Amount (Ͼ)</TableTh>
				</TableRow>
			</TableHead>
			<TableBody>
				<TableRow
					v-for="scheduleItem in releaseScheduleItems"
					:key="scheduleItem.timestamp"
				>
					<TableTd>
						<Tooltip
							:text="convertTimestampToRelative(scheduleItem.timestamp, NOW)"
						>
							{{ formatTimestamp(scheduleItem.timestamp) }}
						</Tooltip>
					</TableTd>

					<TableTd align="right" class="numerical">
						{{ convertMicroCcdToCcd(scheduleItem.amount) }}
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
import type { PaginationTarget } from '~/composables/usePagination'
import { useDateNow } from '~/composables/useDateNow'
import type { PageInfo, TimestampedAmount } from '~/types/generated'

const { NOW } = useDateNow()

type Props = {
	releaseScheduleItems: TimestampedAmount[]
	pageInfo: PageInfo
	goToPage: (page: PageInfo) => (target: PaginationTarget) => void
}
defineProps<Props>()
</script>
