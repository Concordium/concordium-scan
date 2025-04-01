<template>
	<div class="flex flex-wrap flex-grow w-1/2">
		<div class="w-full flex items-center justify-items-stretch">
			<h1 class="inline-block text-2xl">Primed for suspension validators</h1>
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
				v-for="primedForSuspensionValidator in primedForSuspensionValidators"
				:key="primedForSuspensionValidator.id"
			>
				<TableTd>
					<BakerLink :id="primedForSuspensionValidator.id" />
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
	primedForSuspensionValidators: Validators[]
	pageInfo: PageInfo
	goToPage: PaginationGoToPage
}
defineProps<Props>()
</script>
