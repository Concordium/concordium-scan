<template>
	<Suspense>
		<main class="p-4">
			<Table>
				<TableHead>
					<TableRow>
						<TableTh>Status</TableTh>
						<TableTh>Timestamp</TableTh>
						<TableTh>Hash</TableTh>
						<TableTh align="right">Transactions</TableTh>
						<TableTh>Baker</TableTh>
						<TableTh align="right">Reward (Ͼ)</TableTh>
					</TableRow>
				</TableHead>
				<TableBody>
					<TableRow v-for="block in data" :key="block.blockHash">
						<TableTd>
							<StatusCircle
								:class="[
									$style.statusIcon,
									{ 'text-blue-600': !block.finalized },
								]"
							/>
							{{ block.finalized ? 'Finalised' : 'Pending' }}
						</TableTd>
						<TableTd>{{
							convertTimestampToRelative(block.blockSlotTime)
						}}</TableTd>
						<TableTd :class="$style.numerical">
							<LinkButton>
								<HashtagIcon :class="$style.cellIcon" />
								{{ block.blockHash.substring(0, 6) }}
							</LinkButton>
						</TableTd>
						<TableTd align="right" :class="$style.numerical">
							{{ block.transactionCount }}
						</TableTd>
						<TableTd :class="$style.numerical">
							<LinkButton>
								<UserIcon :class="$style.cellIcon" />
								eb0c31
							</LinkButton>
						</TableTd>
						<TableTd align="right" :class="$style.numerical">
							0.006438
						</TableTd>
					</TableRow>
				</TableBody>
			</Table>
		</main>
	</Suspense>
</template>

<script lang="ts" setup>
import { useQuery, gql } from '@urql/vue'
import { formatDistance, parseISO } from 'date-fns'
import { HashtagIcon, UserIcon } from '@heroicons/vue/solid'

const BlocksQuery = gql`
	query {
		blocks {
			nodes {
				blockHash
				blockHeight
				blockSlotTime
				finalized
				transactionCount
			}
		}
	}
`

const result = await useQuery({
	query: BlocksQuery,
})

const NOW = new Date()

const convertTimestampToRelative = (timestamp: DateTime) =>
	formatDistance(parseISO(timestamp), NOW, { addSuffix: true })

const data =
	result.data.blocks?.nodes || result.data._rawValue.blocks?.nodes || []
</script>

<style module>
.statusIcon {
	@apply h-4 mr-2 text-green-600;
}
.cellIcon {
	@apply h-4 text-white inline align-baseline;
}

.numerical {
	@apply font-mono;
}
</style>
