<template>
	<div>
		<BlockDetailsHeader v-if="props.block" :block="props.block" />
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
import { UserIcon } from '@heroicons/vue/solid/index.js'
import BlockDetailsHeader from './BlockDetailsHeader.vue'
import DrawerContent from '~/components/Drawer/DrawerContent.vue'
import DetailsCard from '~/components/DetailsCard.vue'
import Accordion from '~/components/Accordion.vue'
import MintDistribution from '~/components/Tokenomics/MintDistribution.vue'
import FinalizationRewards from '~/components/Tokenomics/FinalizationRewards.vue'
import BlockRewards from '~/components/Tokenomics/BlockRewards.vue'
import { convertTimestampToRelative } from '~/utils/format'
import type { Block } from '~/types/blocks'
import type { PageInfo } from '~/types/generated'
import type { PaginationTarget } from '~/composables/usePagination'

type Props = {
	block: Block
	goToPage: (page: PageInfo) => (target: PaginationTarget) => void
}

const props = defineProps<Props>()

const NOW = new Date()
</script>

<style module>
.numerical {
	@apply font-mono;
	font-variant-ligatures: none;
}

.lol {
	max-width: calc(100% - 150px);
}
</style>
