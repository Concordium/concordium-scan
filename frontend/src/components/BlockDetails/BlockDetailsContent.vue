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
					<Table>
						<TableHead>
							<TableRow>
								<TableTh>Status</TableTh>
								<TableTh>Transaction hash</TableTh>
								<TableTh>Sender</TableTh>
								<TableTh align="right">Cost (Ͼ)</TableTh>
							</TableRow>
						</TableHead>
						<TableBody>
							<TableRow
								v-for="transaction in props.block?.transactions.nodes"
								:key="transaction.transactionHash"
							>
								<TableTd>
									<StatusCircle
										:class="[
											'h-4 mr-2 text-theme-interactive',
											{ 'text-theme-error': !transaction.result.successful },
										]"
									/>
									{{ transaction.result.successful ? 'Success' : 'Rejected' }}
								</TableTd>
								<TableTd :class="$style.numerical">
									<HashtagIcon :class="$style.cellIcon" />
									<DetailsLinkButton
										entity="transaction"
										:hash="transaction.transactionHash"
									>
										{{ transaction.transactionHash.substring(0, 6) }}
									</DetailsLinkButton>
								</TableTd>
								<TableTd :class="$style.numerical">
									<UserIcon
										v-if="transaction.senderAccountAddress"
										:class="$style.cellIcon"
									/>
									{{ transaction.senderAccountAddress?.substring(0, 6) }}
								</TableTd>
								<TableTd align="right" :class="$style.numerical">
									{{ convertMicroCcdToCcd(transaction.ccdCost) }}
								</TableTd>
							</TableRow>
						</TableBody>
					</Table>
				</template>
			</Accordion>
		</DrawerContent>
	</div>
</template>

<script lang="ts" setup>
import {
	UserIcon,
	HashtagIcon,
	DocumentSearchIcon,
} from '@heroicons/vue/solid/index.js'
import DrawerTitle from '~/components/Drawer/DrawerTitle.vue'
import DrawerContent from '~/components/Drawer/DrawerContent.vue'
import DetailsCard from '~/components/DetailsCard.vue'
import Badge from '~/components/Badge.vue'
import Accordion from '~/components/Accordion.vue'
import MintDistribution from '~/components/Tokenomics/MintDistribution.vue'
import FinalizationRewards from '~/components/Tokenomics/FinalizationRewards.vue'
import BlockRewards from '~/components/Tokenomics/BlockRewards.vue'
import type { Block } from '~/types/blocks'
import {
	convertTimestampToRelative,
	convertMicroCcdToCcd,
} from '~/utils/format'

type Props = {
	block: Block
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
