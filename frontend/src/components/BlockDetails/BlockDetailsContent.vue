<template>
	<div>
		<BlockDetailsHeader v-if="props.block" :block="props.block" />
		<DrawerContent v-if="props.block">
			<div class="grid gap-8 md:grid-cols-2 mb-16">
				<DetailsCard v-if="props.block.blockSlotTime">
					<template #title>Age</template>
					<template #default>
						{{
							convertTimestampToRelative(props.block.blockSlotTime || '', NOW)
						}}
					</template>
					<template #secondary>{{
						formatTimestamp(props.block.blockSlotTime)
					}}</template>
				</DetailsCard>
				<DetailsCard v-if="props.block.bakerId">
					<template #title>Baker id</template>
					<template #default>
						<BakerLink
							:id="props.block.bakerId"
							icon-class="h-5 inline align-baseline mr-3"
						/>
					</template>
				</DetailsCard>
			</div>
			<Accordion>
				Tokenomics
				<template #content>
					<MintDistribution
						v-if="props.block.specialEventsOld.mint"
						:data="props.block.specialEventsOld.mint"
					/>
					<FinalizationRewards
						v-if="
							props.block.specialEventsOld.finalizationRewards?.rewards?.nodes
						"
						:data="
							props.block.specialEventsOld.finalizationRewards.rewards.nodes
						"
						:page-info="
							props.block.specialEventsOld.finalizationRewards.rewards.pageInfo
						"
						:go-to-page="props.goToPageFinalizationRewards"
					/>
					<BlockRewards
						v-if="props.block.specialEventsOld.blockRewards"
						:data="props.block.specialEventsOld.blockRewards"
					/>
				</template>
			</Accordion>
			<Accordion>
				Transactions
				<span class="text-theme-faded ml-1">
					({{ props.block.transactionCount }})
				</span>
				<template #content>
					<BlockDetailsTransactions
						v-if="props.block.transactionCount > 0 && props.block.transactions"
						:transactions="props.block.transactions.nodes || []"
						:total-count="props.block.transactionCount"
						:page-info="props.block.transactions.pageInfo"
						:go-to-page="props.goToPageTx"
					/>
					<div v-if="!props.block.transactionCount" class="p-4">
						No transactions
					</div>
				</template>
			</Accordion>
		</DrawerContent>
	</div>
</template>

<script lang="ts" setup>
import BlockDetailsHeader from './BlockDetailsHeader.vue'
import BakerLink from '~/components/molecules/BakerLink.vue'
import DrawerContent from '~/components/Drawer/DrawerContent.vue'
import DetailsCard from '~/components/DetailsCard.vue'
import Accordion from '~/components/Accordion.vue'
import MintDistribution from '~/components/Tokenomics/MintDistribution.vue'
import FinalizationRewards from '~/components/Tokenomics/FinalizationRewards.vue'
import BlockRewards from '~/components/Tokenomics/BlockRewards.vue'
import { useDateNow } from '~/composables/useDateNow'
import { convertTimestampToRelative, formatTimestamp } from '~/utils/format'
import type { PageInfo, Block } from '~/types/generated'
import type { PaginationTarget } from '~/composables/usePagination'
import BlockDetailsTransactions from '~/components/BlockDetails/BlockDetailsTransactions.vue'

const { NOW } = useDateNow()

type Props = {
	block: Block
	goToPageTx: (page: PageInfo) => (target: PaginationTarget) => void
	goToPageFinalizationRewards: (
		page: PageInfo
	) => (target: PaginationTarget) => void
}

const props = defineProps<Props>()
</script>
