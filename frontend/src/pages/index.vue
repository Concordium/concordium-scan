<template>
	<div>
		<Suspense>
			<main class="p-4">
				<Drawer :is-open="isDrawerOpen" :on-close="closeDrawer">
					<h1 class="text-2xl">Content</h1>
				</Drawer>
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
								<LinkButton @click="openDrawer">
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
	</div>
</template>

<script lang="ts" setup>
import { ref } from 'vue'

import { useQuery, gql } from '@urql/vue'
import { formatDistance, parseISO } from 'date-fns'
import { HashtagIcon, UserIcon } from '@heroicons/vue/solid'
import Drawer from '../components/Drawer/Drawer.vue'

const isDrawerOpen = ref(false)

const openDrawer = () => {
	isDrawerOpen.value = true
}

const closeDrawer = () => {
	isDrawerOpen.value = false
}

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

const convertTimestampToRelative = (timestamp: string) =>
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
