<template>
	<div>
		<Title>CCDScan | Bakers</Title>
		<Table>
			<TableHead>
				<TableRow>
					<TableTh width="30%">Baker ID</TableTh>
					<TableTh width="30%">Account</TableTh>
					<TableTh width="40%" class="text-right">Staked amount (Ͼ)</TableTh>
				</TableRow>
			</TableHead>
			<TableBody>
				<TableRow v-for="baker in data?.bakers.nodes" :key="baker.bakerId">
					<TableTd>
						<BakerLink :id="baker.bakerId" />
						<Badge
							v-if="baker.state.__typename === 'RemovedBakerState'"
							type="failure"
							class="badge"
						>
							Removed
						</Badge>
					</TableTd>

					<TableTd>
						<AccountLink :address="baker.account.address.asString" />
					</TableTd>

					<TableTd class="text-right">
						<span
							v-if="baker.state.__typename === 'ActiveBakerState'"
							class="numerical"
						>
							{{ convertMicroCcdToCcd(baker.state.stakedAmount) }}
						</span>
					</TableTd>
				</TableRow>
			</TableBody>
		</Table>

		<Pagination
			v-if="data?.bakers.pageInfo"
			:page-info="data?.bakers.pageInfo"
			:go-to-page="goToPage"
		/>
	</div>
</template>
<script lang="ts" setup>
import { useBakerListQuery } from '~/queries/useBakerListQuery'
import { convertMicroCcdToCcd } from '~/utils/format'
import { usePagination } from '~/composables/usePagination'
import Badge from '~/components/Badge.vue'
import Pagination from '~/components/Pagination.vue'
import BakerLink from '~/components/molecules/BakerLink.vue'
import AccountLink from '~/components/molecules/AccountLink.vue'

const { first, last, after, before, goToPage } = usePagination()

const { data } = useBakerListQuery({ first, last, after, before })
</script>

<style scoped>
/*
  These styles could have been TW classes, but are not applied correctly
  A more dynamic approach would be to have a size prop on the component
*/
.badge {
	display: inline-block;
	font-size: 0.75rem;
	padding: 0.4rem 0.5rem 0.25rem;
	line-height: 1;
}
</style>
