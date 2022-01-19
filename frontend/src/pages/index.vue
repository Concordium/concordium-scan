<template>
	<div>
		<Title>CCDScan | Blocks</Title>
		<main class="p-4">
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
					<TableRow v-for="block in data?.blocks.nodes" :key="block.blockHash">
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
							{{ convertTimestampToRelative(block.blockSlotTime, NOW) }}
						</TableTd>
						<TableTd>
							<LinkButton
								:class="$style.numerical"
								@click="selectedBlockId = block.id"
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

			<Pagination
				v-if="data?.blocks.pageInfo"
				:page-info="data?.blocks.pageInfo"
				:go-to-page="goToPage"
			/>
		</main>
	</div>
</template>

<script lang="ts" setup>
import { useQuery, gql } from '@urql/vue'
import { HashtagIcon, UserIcon } from '@heroicons/vue/solid/index.js'
import { convertTimestampToRelative } from '~/utils/format'
import type { Block } from '~/types/blocks'
import type { PageInfo } from '~/types/pageInfo'
import { usePagination } from '~/composables/usePagination'

const { afterCursor, beforeCursor, paginateFirst, paginateLast, goToPage } =
	usePagination()

const selectedBlockId = useBlockDetails()

type BlockList = {
	blocks: {
		nodes: Block[]
		pageInfo: PageInfo
	}
}

const BlocksQuery = gql<BlockList>`
	query ($after: String, $before: String, $first: Int, $last: Int) {
		blocks(after: $after, before: $before, first: $first, last: $last) {
			nodes {
				id
				bakerId
				blockHash
				blockHeight
				blockSlotTime
				finalized
				transactionCount
			}
			pageInfo {
				startCursor
				endCursor
				hasPreviousPage
				hasNextPage
			}
		}
	}
`

const { data } = useQuery({
	query: BlocksQuery,
	requestPolicy: 'cache-and-network',
	variables: {
		after: afterCursor,
		before: beforeCursor,
		first: paginateFirst,
		last: paginateLast,
	},
})

const NOW = new Date()
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
