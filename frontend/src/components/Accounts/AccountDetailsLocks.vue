<template>
	<div>
		<Table v-if="relatedLocks.length">
			<TableHead>
				<TableRow>
					<TableTh>Lock ID</TableTh>
					<TableTh>Balance</TableTh>
					<TableTh v-if="breakpoint >= Breakpoint.LG">Created</TableTh>
					<TableTh v-if="breakpoint >= Breakpoint.MD">Expiry</TableTh>
					<TableTh>Status</TableTh>
				</TableRow>
			</TableHead>
			<TableBody>
				<TableRow
					v-for="relatedLock in relatedLocks"
					:key="relatedLock.lock.lockId"
				>
					<TableTd>
						<LockLink :lock-id="relatedLock.lock.lockId" />
					</TableTd>
					<TableTd>
						<div
							v-if="relatedLock.accountBalances.length"
							class="space-y-1"
						>
							<div
								v-for="balance in relatedLock.accountBalances"
								:key="`${relatedLock.lock.lockId}-${balance.tokenId}`"
								class="whitespace-nowrap"
							>
								<PltAmount
									:value="balance.amount.value"
									:decimals="Number(balance.amount.decimals)"
								/>
								<span class="numerical text-theme-faded ml-1">
									{{ balance.tokenId }}
								</span>
							</div>
						</div>
						<span v-else class="text-theme-faded">No current balance</span>
					</TableTd>
					<TableTd v-if="breakpoint >= Breakpoint.LG">
						{{ formatOptionalTimestamp(relatedLock.lock.createdAt) }}
					</TableTd>
					<TableTd v-if="breakpoint >= Breakpoint.MD">
						{{ formatOptionalTimestamp(relatedLock.lock.expiry) }}
					</TableTd>
					<TableTd>
						<Badge :type="getStatusBadgeType(relatedLock.lock.status)">
							{{ relatedLock.lock.status.toLowerCase() }}
						</Badge>
					</TableTd>
				</TableRow>
			</TableBody>
		</Table>
		<div v-else class="p-4">No locks</div>
		<div class="mt-8">
			<Pagination
				v-if="pageInfo"
				:page-info="pageInfo"
				:go-to-page="goToPage"
			/>
		</div>
	</div>
</template>

<script lang="ts" setup>
import LockLink from '~/components/molecules/LockLink.vue'
import PltAmount from '~/components/atoms/PltAmount.vue'
import Badge from '~/components/Badge.vue'
import { Breakpoint, useBreakpoint } from '~/composables/useBreakpoint'
import type {
	AccountRelatedLock,
	PageInfo,
} from '~/types/generated'
import { LockStatus } from '~/types/generated'
import { formatTimestamp } from '~/utils/format'
import type { PaginationTarget } from '~/composables/usePagination'

type Props = {
	relatedLocks: AccountRelatedLock[]
	pageInfo: PageInfo
	goToPage: (page: PageInfo) => (target: PaginationTarget) => void
}

defineProps<Props>()

const { breakpoint } = useBreakpoint()

const formatOptionalTimestamp = (timestamp?: string | null) =>
	timestamp ? formatTimestamp(timestamp) : '-'

const getStatusBadgeType = (status: LockStatus) => {
	if (status === LockStatus.Active) return 'success'
	if (status === LockStatus.Expired) return 'info'
	return 'failure'
}
</script>
