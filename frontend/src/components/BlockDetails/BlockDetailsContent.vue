<template>
	<div>
		<DrawerTitle v-if="props.block" class="font-mono">
			<div v-if="$route.name != 'blocks-blockHash'" class="inline">
				<DetailsLinkButton
					:id="props.block.id"
					entity="block"
					:hash="props.block?.blockHash"
				>
					{{ props.block?.blockHash.substring(0, 6) }}
					<DocumentSearchIcon class="h-5 inline align-baseline mr-3" />
				</DetailsLinkButton>
			</div>
			<div v-else class="inline">
				{{ props.block?.blockHash.substring(0, 6) }}
			</div>
			<TextCopy
				:text="props.block?.blockHash"
				label="Click to copy block hash to clipboard"
				class="h-5 inline align-baseline mr-3"
			/>
			<Badge :type="props.block?.finalized ? 'success' : 'failure'">
				{{ props.block?.finalized ? 'Finalised' : 'Rejected' }}
			</Badge>
		</DrawerTitle>
		<DrawerContent v-if="props.block">
			<div class="grid gap-6 grid-cols-2 mb-16">
				<DetailsCard v-if="props.block?.blockSlotTime">
					<template #title>Timestamp</template>
					<template #default>
						{{
							convertTimestampToRelative(props.block?.blockSlotTime || '', NOW)
						}}
					</template>
					<template #secondary>{{ props.block?.blockSlotTime }}</template>
				</DetailsCard>
				<DetailsCard>
					<template #title>Baker id</template>
					<template #default>
						<UserIcon class="h-5 inline align-baseline mr-3" />
						{{ props.block?.bakerId }}
					</template>
				</DetailsCard>
			</div>
			<Accordion>
				Tokenomics
				<template #content>
					<MintDistribution
						v-if="props.block?.specialEvents.mint"
						:data="props.block.specialEvents.mint"
					/>
					<FinalizationRewards
						v-if="props.block?.specialEvents.finalizationRewards"
						:data="props.block.specialEvents.finalizationRewards.rewards.nodes"
					/>
					<BlockRewards
						v-if="props.block?.specialEvents.blockRewards"
						:data="props.block.specialEvents.blockRewards"
					/>
				</template>
			</Accordion>
			<Accordion>
				Transactions
				<span class="text-theme-faded ml-1">
					({{ props.block?.transactionCount }})
				</span>
				<template #content>
					<BlockDetailsTransactions
						:transactions="props.block?.transactions.nodes"
						:total-count="props.block?.transactionCount"
						:page-info="props.block.transactions?.pageInfo"
						:go-to-page="props.goToPage"
					/>
				</template>
			</Accordion>
		</DrawerContent>
	</div>
</template>

<script lang="ts" setup>
import { UserIcon, DocumentSearchIcon } from '@heroicons/vue/solid/index.js'
import BlockDetailsTransactions from './BlockDetailsTransactions.vue'
import DrawerTitle from '~/components/Drawer/DrawerTitle.vue'
import DrawerContent from '~/components/Drawer/DrawerContent.vue'
import DetailsCard from '~/components/DetailsCard.vue'
import Badge from '~/components/Badge.vue'
import TextCopy from '~/components/atoms/TextCopy.vue'
import Accordion from '~/components/Accordion.vue'
import MintDistribution from '~/components/Tokenomics/MintDistribution.vue'
import FinalizationRewards from '~/components/Tokenomics/FinalizationRewards.vue'
import BlockRewards from '~/components/Tokenomics/BlockRewards.vue'
import { convertTimestampToRelative } from '~/utils/format'
import type { Block } from '~/types/blocks'
import type { PageInfo } from '~/types/pageInfo'
import type { PaginationTarget } from '~/composables/usePagination'

type Props = {
	block: Block
	goToPage: (page: PageInfo) => (target: PaginationTarget) => void
}
const selectedBlockId = useBlockDetails()
const props = defineProps<Props>()
const route = useRoute()
// Since this is used in both the drawer and other places, this is a quick way to make sure the drawer closes on route change.
watch(route, _to => {
	selectedBlockId.value = ''
})

const NOW = new Date()
</script>
