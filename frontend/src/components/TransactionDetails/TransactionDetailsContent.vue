<template>
	<div>
		<TransactionDetailsHeader
			v-if="props.transaction"
			:transaction="props.transaction"
		/>
		<DrawerContent v-if="props.transaction">
			<div class="grid gap-8 md:grid-cols-2 mb-16">
				<DetailsCard>
					<template #title>Block height / block hash</template>
					<template #default>
						<span class="numerical">
							{{ props.transaction?.block.blockHeight }}
						</span>
					</template>
					<template #secondary>
						<BlockLink
							:id="props.transaction?.block.id"
							icon-size="big"
							:hash="props.transaction?.block.blockHash"
						/>
					</template>
				</DetailsCard>
				<DetailsCard v-if="props.transaction?.block.blockSlotTime">
					<template #title>Age</template>
					<template #default>
						{{
							convertTimestampToRelative(
								props.transaction?.block.blockSlotTime,
								NOW
							)
						}}
					</template>
					<template #secondary>
						{{ formatTimestamp(props.transaction?.block.blockSlotTime) }}
					</template>
				</DetailsCard>
				<DetailsCard v-if="props.transaction?.transactionType">
					<template #title>Transaction type / cost (Ͼ)</template>
					<template #default>
						{{ translateTransactionType(props.transaction?.transactionType) }}
					</template>
					<template #secondary>
						<Amount :amount="props.transaction?.ccdCost" />
					</template>
				</DetailsCard>
				<DetailsCard v-if="props.transaction?.senderAccountAddress?.asString">
					<template #title>Sender</template>
					<template #default>
						<AccountLink
							icon-size="big"
							:address="props.transaction.senderAccountAddress.asString"
						/>
					</template>
				</DetailsCard>
			</div>
			<Accordion
				v-if="
					props.transaction?.result.__typename === 'Success' &&
					props.transaction.result.events
				"
				:is-initial-open="true"
			>
				Events
				<span class="numerical text-theme-faded ml-1">
					({{ props.transaction?.result.events?.totalCount }})
				</span>
				<template #content>
					<TransactionEventList
						:transaction="props.transaction"
						:events="props.transaction.result.events"
						:total-count="props.transaction?.result.events?.totalCount"
						:page-info="props.transaction.result.events?.pageInfo"
						:go-to-page="props.goToPage"
					/>
				</template>
			</Accordion>
			<Accordion
				v-if="props.transaction?.result.__typename === 'Rejected'"
				:is-initial-open="true"
			>
				Reject reason
				<template #content>
					<RejectionReason :reason="props.transaction.result.reason" />
				</template>
			</Accordion>
		</DrawerContent>
	</div>
</template>

<script lang="ts" setup>
import TransactionDetailsHeader from './TransactionDetailsHeader.vue'
import Amount from '~/components/atoms/Amount.vue'
import DrawerContent from '~/components/Drawer/DrawerContent.vue'
import DetailsCard from '~/components/DetailsCard.vue'
import Accordion from '~/components/Accordion.vue'
import TransactionEventList from '~/components/TransactionEventList/TransactionEventList.vue'
import RejectionReason from '~/components/RejectionReason/RejectionReason.vue'
import { formatTimestamp, convertTimestampToRelative } from '~/utils/format'
import { translateTransactionType } from '~/utils/translateTransactionTypes'
import { useDateNow } from '~/composables/useDateNow'
import type { PageInfo, Transaction } from '~/types/generated'
import type { PaginationTarget } from '~/composables/usePagination'

const selectedTxId = useTransactionDetails()
const { NOW } = useDateNow()

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
