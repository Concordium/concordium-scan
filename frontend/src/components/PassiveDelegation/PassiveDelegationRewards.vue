<template>
	<div>
		<Table>
			<TableHead>
				<TableRow>
					<TableTh>Time</TableTh>
					<TableTh v-if="breakpoint >= Breakpoint.LG">Type</TableTh>
					<TableTh v-if="breakpoint >= Breakpoint.XL">Reference</TableTh>
					<TableTh class="text-right">Amount (Ͼ)</TableTh>
				</TableRow>
			</TableHead>
			<TableBody>
				<TableRow v-for="reward in rewards" :key="reward.id">
					<TableTd>
						<Tooltip :text="convertTimestampToRelative(reward.timestamp, NOW)">
							{{ formatTimestamp(reward.timestamp) }}
						</Tooltip>
					</TableTd>
					<TableTd v-if="breakpoint >= Breakpoint.LG">
						<div class="whitespace-normal">
							<span class="pl-2">
								<RewardIcon
									class="h-4 text-theme-white inline align-text-top"
								/>
								<span class="pl-2">
									{{ translateBakerRewardType(reward.rewardType) }}
								</span>
							</span>
						</div>
					</TableTd>
					<TableTd v-if="breakpoint >= Breakpoint.XL">
						<BlockLink :hash="reward.block.blockHash" />
					</TableTd>
					<TableTd class="numerical" align="right">
						<Amount :amount="reward.totalAmount" />
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
import { formatTimestamp, convertTimestampToRelative } from '~/utils/format'
import { useBreakpoint, Breakpoint } from '~/composables/useBreakpoint'
import type { PaginationTarget } from '~/composables/usePagination'
import { useDateNow } from '~/composables/useDateNow'
import type { PageInfo, PoolReward } from '~/types/generated'
import Amount from '~/components/atoms/Amount.vue'
import { translateBakerRewardType } from '~/utils/translateBakerRewardType'

const { NOW } = useDateNow()
const { breakpoint } = useBreakpoint()

type Props = {
	rewards: PoolReward[]
	pageInfo: PageInfo
	totalCount: number
	goToPage: (page: PageInfo) => (target: PaginationTarget) => void
}
defineProps<Props>()
</script>
