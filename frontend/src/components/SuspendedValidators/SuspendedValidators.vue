<template>
	<div class="flex flex-wrap flex-grow w-1/2">
		<div class="w-full flex items-center justify-items-stretch">
			<h1 class="inline-block text-2xl">Suspended validators</h1>
		</div>
	</div>
	<Table>
		<TableHead>
			<TableRow>
				<TableTh>Validator ID</TableTh>
			</TableRow>
		</TableHead>
		<TableBody>
			<TableRow
				v-for="suspendedValidator in suspendedValidators"
				:key="suspendedValidator.id"
			>
				<TableTd>
					<BakerLink :id="suspendedValidator.id" />
				</TableTd>
			</TableRow>
		</TableBody>
	</Table>
	<Pagination
		v-if="pageInfo && (pageInfo.hasNextPage || pageInfo.hasPreviousPage)"
		:page-info="pageInfo"
		:go-to-page="goToPage"
	/>
</template>

<script lang="ts" setup>
import type { PageInfo, Validators } from '~/types/generated'
import BakerLink from '~/components/molecules/BakerLink.vue'

type Props = {
	suspendedValidators: Validators[]
	pageInfo: PageInfo
	goToPage: PaginationGoToPage
}
defineProps<Props>()
</script>
