<template>
	<div>
		<ContractDetailsHeader :contract="contract" />
		<DrawerContent>
			<div class="grid gap-8 md:grid-cols-2 mb-4">
				<ContractDetailsAmounts :contract="contract" />
				<DetailsCard>
					<template #title>Age</template>
					<template #default>
						{{ convertTimestampToRelative(contract.createdTime, NOW) }}
					</template>
					<template #secondary>
						{{ formatTimestamp(contract.createdTime) }}
					</template>
				</DetailsCard>
			</div>
			<Accordion>
				Transactions
				<span class="numerical text-theme-faded"
					>({{ contract.transactionsCount }})</span
				>
				<template #content>
					<ContractDetailsTransactions
						v-if="
							contract.transactions?.nodes?.length &&
							contract.transactions?.nodes?.length > 0
						"
						:transactions="contract.transactions.nodes"
						:total-count="contract.transactions!.nodes.length"
						:page-info="contract!.transactions!.pageInfo"
						:go-to-page="goToPageTx"
					/>
					<div v-else class="p-4">No transactions</div>
				</template>
			</Accordion>
		</DrawerContent>
	</div>
</template>

<script lang="ts" setup>
import ContractDetailsAmounts from './ContractDetailsAmounts.vue'
import ContractDetailsHeader from './ContractDetailsHeader.vue'
import ContractDetailsTransactions from './ContractDetailsTransactions.vue'
import DrawerContent from '~/components/Drawer/DrawerContent.vue'
import DetailsCard from '~/components/DetailsCard.vue'
import { formatTimestamp, convertTimestampToRelative } from '~/utils/format'
import type { Contract, PageInfo } from '~/types/generated'
import { useDateNow } from '~/composables/useDateNow'
import type { PaginationTarget } from '~/composables/usePagination'

const { NOW } = useDateNow()

type Props = {
	contract: Contract
	goToPageTx: (page: PageInfo) => (target: PaginationTarget) => void
}

defineProps<Props>()
</script>
