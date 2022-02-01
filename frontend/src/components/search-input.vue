<template>
	<div class="relative md:w-1/3">
		<input
			v-model="searchValue"
			:class="$style.input"
			class="rounded p-2 w-full focus:ring-2 focus:ring-pink-500 outline-none hidden md:block"
			placeholder="Search for block or transaction &hellip;"
			type="search"
			@keyup.enter="gotoSearchResult"
		/>
		<div
			v-if="
				queryData &&
				(queryData.search.blocks.length > 0 ||
					queryData.search.transactions.length > 0)
			"
			class="absolute border solid rounded-lg p-4 bg-theme-common-white w-full bg-opacity-10"
			@click="searchValue = ''"
		>
			<div class="overflow-hidden whitespace-nowrap overflow-ellipsis">
				<h3>Search hits:</h3>
				<DetailsLinkButton
					v-if="queryData.search.blocks.length > 0"
					:id="queryData.search.blocks[0].id"
					entity="block"
					:hash="queryData.search.blocks[0].blockHash"
					>Block {{ queryData.search.blocks[0].blockHash }}</DetailsLinkButton
				>
				<DetailsLinkButton
					v-if="queryData.search.transactions.length > 0"
					:id="queryData.search.transactions[0].id"
					entity="transaction"
					:hash="queryData.search.transactions[0].transactionHash"
					>Transaction
					{{
						queryData.search.transactions[0].transactionHash
					}}</DetailsLinkButton
				>
			</div>
		</div>
		<button :class="$style.button">
			<SearchIcon class="h-6 md:h-5" />
		</button>
	</div>
</template>

<script lang="ts" setup>
import { SearchIcon } from '@heroicons/vue/outline/index.js'
import { useSearchQuery } from '~/queries/useSearchQuery'
import DetailsLinkButton from '~/components/molecules/DetailsLinkButton.vue'
import {
	useEntityNavigator,
	EntityType,
} from '~/composables/useEntityNavigator'
const { goto } = useEntityNavigator()
const searchValue = ref('')
const delayedSearchValue = ref('')
const { data: queryData } = useSearchQuery(delayedSearchValue)
let searchQueryTimeout: NodeJS.Timeout | null = null
watch(searchValue, (newValue, _oldValue) => {
	if (searchQueryTimeout) clearTimeout(searchQueryTimeout)
	if (!newValue) delayedSearchValue.value = newValue
	else
		searchQueryTimeout = setTimeout(() => {
			delayedSearchValue.value = newValue
		}, 500)
})
const gotoSearchResult = () => {
	if (
		searchValue.value !== delayedSearchValue.value &&
		!delayedSearchValue.value
	)
		return
	if (
		queryData &&
		queryData.value &&
		queryData.value.search &&
		(queryData.value.search.transactions[0] || queryData.value.search.blocks[0])
	) {
		if (queryData.value.search.transactions[0])
			goto(
				EntityType.Transaction,
				queryData.value.search.transactions[0].id,
				queryData.value.search.transactions[0].transactionHash
			)
		else if (queryData.value.search.blocks[0])
			goto(
				EntityType.Block,
				queryData.value.search.blocks[0].id,
				queryData.value.search.blocks[0].blockHash
			)

		searchValue.value = ''
	}
}
</script>

<style module>
.input {
	background: var(--color-input-bg);
}

.input::placeholder {
	@apply italic;
	color: var(--color-input-placeholder);
}

.button {
	@apply absolute top-1/2 right-3;
	transform: translateY(-50%);
	pointer-events: none;
}
</style>
