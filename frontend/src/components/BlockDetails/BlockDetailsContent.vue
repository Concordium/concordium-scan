<template>
	<div>
		<DrawerTitle class="font-mono">
			{{ data?.block?.blockHash.substring(0, 6) }}
			<Badge :type="data?.block?.finalized ? 'success' : 'failure'">
				{{ data?.block?.finalized ? 'Finalised' : 'Rejected' }}
			</Badge>
		</DrawerTitle>
		<DrawerContent>
			<div class="grid gap-6 grid-cols-2 mb-16">
				<DetailsCard>
					<template #title>Timestamp</template>
					<template #default>
						{{
							convertTimestampToRelative(data?.block?.blockSlotTime || '', NOW)
						}}
					</template>
					<template #secondary>{{ data?.block?.blockSlotTime }}</template>
				</DetailsCard>
				<DetailsCard>
					<template #title>Baker id</template>
					<template #default>
						<UserIcon class="h-5 inline align-baseline mr-3" />
						{{ data?.block?.bakerId }}
					</template>
				</DetailsCard>
			</div>
			<Accordion>
				Tokenomics
				<template #content> Tokenomics go here </template>
			</Accordion>
			<Accordion>
				Transactions
				<span class="text-theme-faded ml-1">
					({{ data?.block?.transactionCount }})
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
								v-for="transaction in data?.block?.transactions.nodes"
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
									{{ transaction.transactionHash.substring(0, 6) }}
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
import { useQuery, gql } from '@urql/vue'
import { UserIcon, HashtagIcon } from '@heroicons/vue/solid/index.js'
import DrawerTitle from '~/components/Drawer/DrawerTitle.vue'
import DrawerContent from '~/components/Drawer/DrawerContent.vue'
import DetailsCard from '~/components/DetailsCard.vue'
import Badge from '~/components/Badge.vue'
import Accordion from '~/components/Accordion.vue'
import {
	convertTimestampToRelative,
	convertMicroCcdToCcd,
} from '~/utils/format'
import type { Block } from '~/types/blocks'

type BlockResponse = {
	block?: Block
}

type Props = {
	id: string
}

const props = defineProps<Props>()

const BlockQuery = gql<BlockResponse>`
	query ($id: ID!) {
		block(id: $id) {
			id
			blockHash
			bakerId
			blockSlotTime
			finalized
			transactionCount
			transactions {
				nodes {
					transactionHash
					senderAccountAddress
					ccdCost
					result {
						successful
					}
				}
			}
		}
	}
`

const { data } = await useQuery({
	query: BlockQuery,
	variables: { id: props.id },
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
