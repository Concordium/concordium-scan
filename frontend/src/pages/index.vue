<template>
	<div>
		<BlockDetails :block-id="selectedBlockId" :on-close="closeDrawer" />
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
								@click="openDrawer(block.id)"
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
		</main>
	</div>
</template>

<script lang="ts" setup>
import { ref } from 'vue'
import { useQuery, gql } from '@urql/vue'
import { HashtagIcon, UserIcon } from '@heroicons/vue/solid'
import BlockDetails from '~/components/BlockDetails/BlockDetails.vue'
import { convertTimestampToRelative } from '~/utils/format'

// Splitting the types out will cause an import error, as they are are not
// bundled by Nuxt. See more in README.md under "Known issues"
type Block = {
	id: string
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

const selectedBlockId = ref('')

const openDrawer = (id: string) => {
	selectedBlockId.value = id
}

const closeDrawer = () => {
	selectedBlockId.value = ''
}

const BlocksQuery = gql<BlockList>`
	query {
		blocks {
			nodes {
				id
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
	requestPolicy: 'cache-first',
})

const NOW = new Date()
</script>

<style module>
.statusIcon {
	@apply h-4 mr-2 text-theme-interactive;
}
.cellIcon {
	@apply h-4 text-theme-white inline align-baseline;
}

.numerical {
	@apply font-mono;
	font-variant-ligatures: none;
}
</style>
