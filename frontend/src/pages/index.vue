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
							<TableTh>Height</TableTh>
							<TableTh>Status</TableTh>
							<TableTh>Timestamp</TableTh>
							<TableTh>Block hash</TableTh>
							<TableTh>Baker</TableTh>
							<TableTh align="right">Transactions</TableTh>
						</TableRow>
					</TableHead>
					<TableBody>
						<TableRow
							v-for="block in data?.blocks.nodes"
							:key="block.blockHash"
						>
							<TableTd>{{ block.blockHeight }}</TableTd>
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
								convertTimestampToRelative(block.blockSlotTime, NOW)
							}}</TableTd>
							<TableTd :class="$style.numerical">
								<LinkButton @click="openDrawer">
									<HashtagIcon :class="$style.cellIcon" />
									{{ block.blockHash.substring(0, 6) }}
								</LinkButton>
							</TableTd>
							<TableTd :class="$style.numerical">
								<UserIcon v-if="block.bakerId" :class="$style.cellIcon" />
								{{ block.bakerId }}
							</TableTd>
							<TableTd align="right" :class="$style.numerical">
								{{ block.transactionCount }}
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
import { HashtagIcon, UserIcon } from '@heroicons/vue/solid'
import Drawer from '../components/Drawer/Drawer.vue'
import { convertTimestampToRelative } from '~/utils/format'
import { BlockList } from '~/types/types'

const isDrawerOpen = ref(false)

const openDrawer = () => {
	isDrawerOpen.value = true
}

const closeDrawer = () => {
	isDrawerOpen.value = false
}

const BlocksQuery = gql<BlockList>`
	query {
		blocks {
			nodes {
				bakerId
				blockHash
				blockHeight
				blockSlotTime
				finalized
				transactionCount
			}
		}
	}
`

const { data } = await useQuery({
	query: BlocksQuery,
})

const NOW = new Date()
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
