<template>
	<div>
		<DrawerTitle v-if="props.transaction" class="font-mono">
			<div v-if="$route.name != 'transactions-transactionHash'" class="inline">
				<DetailsLinkButton
					:id="props.transaction.id"
					entity="transaction"
					:hash="props.transaction?.transactionHash"
				>
					{{ props.transaction?.transactionHash.substring(0, 6) }}
					<DocumentSearchIcon class="h-5 inline align-baseline mr-3" />
				</DetailsLinkButton>
			</div>
			<div v-else class="inline">
				{{ props.transaction?.transactionHash.substring(0, 6) }}
			</div>

			<TextCopy
				:text="props.transaction?.transactionHash"
				label="Click to copy transaction hash to clipboard"
				class="h-5 inline align-baseline mr-3"
			/>

			<Badge
				:type="props.transaction?.result.successful ? 'success' : 'failure'"
			>
				{{ props.transaction?.result.successful ? 'Success' : 'Rejected' }}
			</Badge>
		</DrawerTitle>
		<DrawerContent v-if="props.transaction">
			<div class="grid gap-6 grid-cols-2 mb-16">
				<DetailsCard>
					<template #title>Block height / block hash</template>
					<template #default>
						{{ props.transaction?.block.blockHeight }}
					</template>
					<template #secondary>
						<DetailsLinkButton
							entity="block"
							:hash="props.transaction?.block.blockHash"
						>
							{{ props.transaction?.block.blockHash.substring(0, 6) }}
						</DetailsLinkButton>
					</template>
				</DetailsCard>
				<DetailsCard v-if="props.transaction?.block.blockSlotTime">
					<template #title>Timestamp</template>
					<template #default>
						{{
							convertTimestampToRelative(props.transaction?.block.blockSlotTime)
						}}
					</template>
					<template #secondary>
						{{ props.transaction?.block.blockSlotTime }}
					</template>
				</DetailsCard>
				<DetailsCard v-if="props.transaction?.transactionType">
					<template #title>Transaction type / cost (Ͼ)</template>
					<template #default>
						{{ translateTransactionType(props.transaction?.transactionType) }}
					</template>
					<template #secondary>
						{{ convertMicroCcdToCcd(props.transaction?.ccdCost) }}
					</template>
				</DetailsCard>
				<DetailsCard v-if="props.transaction?.senderAccountAddress">
					<template #title>Sender</template>
					<template #default>
						<UserIcon class="h-5 inline align-baseline mr-3" />
						{{ props.transaction?.senderAccountAddress.substring(0, 6) }}
					</template>
				</DetailsCard>
			</div>
			<Accordion>
				Events
				<span
					v-if="props.transaction?.result.successful"
					class="text-theme-faded ml-1"
				>
					({{ props.transaction?.result.events?.totalCount }})
				</span>
				<template #content>
					<TransactionEventList
						v-if="props.transaction?.result.successful"
						:events="props.transaction.result.events?.nodes"
						:total-count="props.transaction?.result.events?.totalCount"
						:page-info="props.transaction.result.events?.pageInfo"
						:go-to-page="props.goToPage"
					/>
				</template>
			</Accordion>
		</DrawerContent>
	</div>
</template>

<script lang="ts" setup>
import { UserIcon, DocumentSearchIcon } from '@heroicons/vue/solid/index.js'
import DrawerTitle from '~/components/Drawer/DrawerTitle.vue'
import DrawerContent from '~/components/Drawer/DrawerContent.vue'
import DetailsCard from '~/components/DetailsCard.vue'
import Badge from '~/components/Badge.vue'
import TextCopy from '~/components/atoms/TextCopy.vue'
import Accordion from '~/components/Accordion.vue'
import TransactionEventList from '~/components/TransactionEventList.vue'
import {
	convertMicroCcdToCcd,
	convertTimestampToRelative,
} from '~/utils/format'
import { translateTransactionType } from '~/utils/translateTransactionTypes'
import type { Transaction } from '~/types/transactions'
import type { PageInfo } from '~/types/pageInfo'
import type { PaginationTarget } from '~/composables/usePagination'

const selectedTxId = useTransactionDetails()

type Props = {
	transaction: Transaction
	goToPage: (page: PageInfo) => (target: PaginationTarget) => void
}

const props = defineProps<Props>()
const route = useRoute()
// Since this is used in both the drawer and other places, this is a quick way to make sure the drawer closes on route change.
watch(route, _to => {
	selectedTxId.value = ''
})
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
