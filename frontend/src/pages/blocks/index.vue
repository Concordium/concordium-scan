<template>
	<div>
		<Title>CCDScan | Blocks</Title>
		<main class="p-4 pb-0">
			<Table>
				<TableHead>
					<TableRow>
						<TableTh width="20%">Height</TableTh>
						<TableTh width="20%">Status</TableTh>
						<TableTh width="30%">Timestamp</TableTh>
						<TableTh width="10%">Block hash</TableTh>
						<TableTh width="10%">Baker</TableTh>
						<TableTh width="10%" align="right">Transactions</TableTh>
					</TableRow>
				</TableHead>
				<TableBody>
					<TableRow>
						<TableTd colspan="6" align="center" class="p-0 tdlol">
							<ShowMoreButton :new-item-count="newItems" :refetch="refetch" />
						</TableTd>
					</TableRow>
					<TableRow v-for="block in pagedData" :key="block.blockHash">
						<TableTd :class="$style.numerical">{{ block.blockHeight }}</TableTd>
						<TableTd>
							<StatusCircle
								:class="[
									'h-4 mr-2 text-theme-interactive',
									{ 'text-theme-info': !block.finalized },
								]"
							/>
							{{ block.finalized ? 'Finalised' : 'Pending' }}
						</TableTd>
						<TableTd>
							{{ convertTimestampToRelative(block.blockSlotTime) }}
						</TableTd>
						<TableTd>
							<LinkButton
								:class="$style.numerical"
								@click="
									() => {
										drawer.push('block', block.blockHash, block.id)
									}
								"
							>
								<HashtagIcon :class="$style.cellIcon" />
								{{ block.blockHash.substring(0, 6) }}
							</LinkButton>
						</TableTd>
						<TableTd :class="$style.numerical">
							<UserIcon
								v-if="block.bakerId || block.bakerId === 0"
								:class="$style.cellIcon"
							/>
							{{ block.bakerId }}
						</TableTd>
						<TableTd align="right" :class="$style.numerical">
							{{ block.transactionCount }}
						</TableTd>
					</TableRow>
				</TableBody>
			</Table>

			<LoadMore
				v-if="data?.blocks.pageInfo"
				:page-info="data?.blocks.pageInfo"
				:on-load-more="loadMore"
			/>
		</main>
	</div>
</template>

<script lang="ts" setup>
import { HashtagIcon, UserIcon } from '@heroicons/vue/solid/index.js'
import { convertTimestampToRelative } from '~/utils/format'
import { usePagedData } from '~/composables/usePagedData'
import { useBlockListQuery } from '~/queries/useBlockListQuery'
import { useBlockSubscription } from '~/subscriptions/useBlockSubscription'
import type { BlockSubscriptionResponse, Block } from '~/types/blocks'
import { useDrawer } from '~/composables/useDrawer'

const { pagedData, first, last, after, addPagedData, fetchNew, loadMore } =
	usePagedData<Block>()

const newItems = ref(0)
const subscriptionHandler = (
	_prevData: void,
	_newData: BlockSubscriptionResponse
) => {
	newItems.value++
}

const before = ref<string | undefined>(undefined)

useBlockSubscription(subscriptionHandler)

const refetch = () => {
	fetchNew(newItems.value)
	newItems.value = 0
}

const { data } = useBlockListQuery({
	first,
	last,
	after,
	before,
})

watch(
	() => data.value,
	value => {
		addPagedData(value?.blocks.nodes || [], value?.blocks.pageInfo)
	}
)
const drawer = useDrawer()
</script>

<style module>
.cellIcon {
	@apply h-4 text-theme-white inline align-baseline;
}

.numerical {
	@apply font-mono;
	font-variant-ligatures: none;
}
</style>

<style>
.tdlol {
	padding-left: 0 !important;
	padding-right: 0 !important;
}
.hello {
	transition: background-color 0.2s ease-in;
}

.hello:hover {
	background-color: hsl(0deg 0% 83% / 6%);
}
</style>
