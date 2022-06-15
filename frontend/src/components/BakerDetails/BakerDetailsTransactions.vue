<template>
	<div class="w-full">
		<Table v-if="componentState === 'success' || componentState === 'loading'">
			<TableHead>
				<TableRow>
					<TableTh>Hash</TableTh>
					<TableTh>Type</TableTh>
					<TableTh>Age</TableTh>
				</TableRow>
			</TableHead>
			<TableBody v-if="componentState === 'success'">
				<TableRow
					v-for="transaction in data?.bakerByBakerId.transactions?.nodes || []"
					:key="transaction.transaction.transactionHash"
				>
					<TableTd class="numerical">
						<TransactionLink
							:id="transaction.transaction.id"
							:hash="transaction.transaction.transactionHash"
						/>
					</TableTd>
					<TableTd>
						<div class="whitespace-normal">
							{{
								translateTransactionType(
									transaction.transaction.transactionType
								)
							}}
						</div>
					</TableTd>
					<TableTd>
						<Tooltip
							:text="
								formatTimestamp(transaction.transaction.block.blockSlotTime)
							"
						>
							{{
								convertTimestampToRelative(
									transaction.transaction.block.blockSlotTime,
									NOW
								)
							}}
						</Tooltip>
					</TableTd>
				</TableRow>
			</TableBody>

			<TableBody v-else-if="componentState === 'loading'">
				<TableRow>
					<TableTd colspan="3">
						<div class="relative h-48">
							<Loader />
						</div>
					</TableTd>
				</TableRow>
			</TableBody>
		</Table>

		<NotFound v-else-if="componentState === 'empty'">
			No data
			<template #secondary>
				There are no related transactions for this baker
			</template>
		</NotFound>
		<Error v-else-if="componentState === 'error'" :error="error" />

		<Pagination
			v-if="
				componentState === 'success' &&
				(pageInfo?.hasNextPage || pageInfo?.hasPreviousPage)
			"
			:page-info="pageInfo"
			:go-to-page="goToPage"
		/>
	</div>
</template>

<script lang="ts" setup>
import { ref } from 'vue'
import { useDateNow } from '~/composables/useDateNow'
import { usePagination, PAGE_SIZE_SMALL } from '~/composables/usePagination'
import { formatTimestamp, convertTimestampToRelative } from '~/utils/format'
import { translateTransactionType } from '~/utils/translateTransactionTypes'
import { useBakerTransactionsQuery } from '~/queries/useBakerTransactionsQuery'
import Tooltip from '~/components/atoms/Tooltip.vue'
import Error from '~/components/molecules/Error.vue'
import Loader from '~/components/molecules/Loader.vue'
import NotFound from '~/components/molecules/NotFound.vue'
import TransactionLink from '~/components/molecules/TransactionLink.vue'
import Pagination from '~/components/Pagination.vue'
import Table from '~/components/Table/Table.vue'
import TableTd from '~/components/Table/TableTd.vue'
import TableTh from '~/components/Table/TableTh.vue'
import TableRow from '~/components/Table/TableRow.vue'
import TableBody from '~/components/Table/TableBody.vue'
import TableHead from '~/components/Table/TableHead.vue'
import type { Baker, PageInfo } from '~/types/generated'

const { first, last, after, before, goToPage } = usePagination({
	pageSize: PAGE_SIZE_SMALL,
})

const { NOW } = useDateNow()

type Props = {
	bakerId: Baker['bakerId']
}

const props = defineProps<Props>()

const { data, error, componentState } = useBakerTransactionsQuery(
	props.bakerId,
	{
		first,
		last,
		after,
		before,
	}
)

const pageInfo = ref<PageInfo | undefined>(
	data?.value?.bakerByBakerId?.transactions?.pageInfo
)

watch(
	() => data.value,
	value => (pageInfo.value = value?.bakerByBakerId?.transactions?.pageInfo)
)
</script>
