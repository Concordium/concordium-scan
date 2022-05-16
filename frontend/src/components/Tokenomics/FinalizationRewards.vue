<template>
	<TokenomicsDisplay class="p-4 pr-0">
		<template #title>Finalisers</template>
		<template #content>
			<Table>
				<TableHead>
					<TableRow>
						<TableTh>Finaliser</TableTh>
						<TableTh align="right">Reward (Ͼ)</TableTh>
					</TableRow>
				</TableHead>
				<TableBody>
					<TableRow
						v-for="finalizer in data"
						:key="finalizer.accountAddress.asString"
					>
						<TableTd>
							<AccountLink :address="finalizer.accountAddress.asString" />
						</TableTd>
						<TableTd align="right" class="numerical">
							{{ convertMicroCcdToCcd(finalizer.amount) }}
						</TableTd>
					</TableRow>
				</TableBody>
			</Table>
			<Pagination
				v-if="pageInfo && (pageInfo.hasNextPage || pageInfo.hasPreviousPage)"
				class="relative"
				:page-info="pageInfo"
				:go-to-page="goToPage"
			/>
		</template>
	</TokenomicsDisplay>
</template>

<script lang="ts" setup>
import TokenomicsDisplay from './TokenomicsDisplay.vue'
import AccountLink from '~/components/molecules/AccountLink.vue'
import Table from '~/components/Table/Table.vue'
import TableTd from '~/components/Table/TableTd.vue'
import TableTh from '~/components/Table/TableTh.vue'
import TableRow from '~/components/Table/TableRow.vue'
import TableBody from '~/components/Table/TableBody.vue'
import TableHead from '~/components/Table/TableHead.vue'
import Pagination from '~/components/Pagination.vue'
import { convertMicroCcdToCcd } from '~/utils/format'
import type { PaginationTarget } from '~/composables/usePagination'
import type { AccountAddressAmount, PageInfo } from '~/types/generated'

type Props = {
	data?: AccountAddressAmount[]
	pageInfo?: PageInfo
	goToPage: (page: PageInfo) => (target: PaginationTarget) => void
}

defineProps<Props>()
</script>
