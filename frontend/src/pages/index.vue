<template>
	<div>
		<BlockDetails :is-open="isDrawerOpen" :on-close="closeDrawer" />
		<main class="p-4">
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
					<TableRow v-for="block in data?.blocks.nodes" :key="block.blockHash">
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
						<TableTd>
							{{ convertTimestampToRelative(block.blockSlotTime, NOW) }}
						</TableTd>
						<TableTd :class="$style.numerical">
							<LinkButton @click="openDrawer">
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
		</main>
	</div>
</template>

<script lang="ts" setup>
import { ref } from 'vue'
import { useQuery, gql } from '@urql/vue'
import { HashtagIcon, UserIcon } from '@heroicons/vue/solid'
import BlockDetails from '~/components/BlockDetails.vue'
import { convertTimestampToRelative } from '~/utils/format'

// Splitting the types out will cause an import error, as they are are not
// bundled by Nuxt. See more in README.md under "Known issues"
type Block = {
	id: number
	bakerId?: number
	blockHash: string
	blockHeight: number
	blockSlotTime: string
	finalized: boolean
	transactionCount: number
}

type BlockList = {
	blocks: {
		nodes: Block[]
	}
}

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
