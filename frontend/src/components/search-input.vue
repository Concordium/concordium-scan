<template>
	<div class="md:relative md:w-1/3 w-full">
		<div class="relative">
			<input
				v-model="searchValue"
				:class="$style.input"
				class="rounded p-2 w-full focus:ring-2 focus:ring-pink-500 outline-none md:block"
				placeholder="Search for account, block or transaction &hellip;"
				type="search"
				@keyup.enter="gotoSearchResult"
			/>
			<BWCubeLogoIcon
				v-if="loading && searchValue"
				class="absolute right-8 top-2 w-6 h-6 animate-ping"
			/>
		</div>

		<div
			v-if="
				!loading &&
				queryData &&
				(queryData.search.blocks.nodes.length > 0 ||
					queryData.search.transactions.nodes.length > 0 ||
					queryData.search.accounts.nodes.length > 0)
			"
			class="left-0 md:left-auto absolute border solid rounded-lg p-4 bg-theme-background-primary-elevated-nontrans w-full z-10"
			@click="searchValue = ''"
		>
			<div class="overflow-hidden whitespace-nowrap overflow-ellipsis">
				<h3>Search hits:</h3>
				<div v-if="queryData.search.blocks.nodes.length > 0">
					<div class="text-xl">Blocks</div>
					<div
						v-for="block in queryData.search.blocks.nodes"
						:key="block.blockHash"
						class="grid grid-cols-4"
					>
						<BlockLink
							:id="block.id"
							:hash="block.blockHash"
							:hide-tooltip="true"
						/>
						<div>
							{{ block.transactions.nodes.length }}
							<span v-if="block.transactions.nodes.length === 1"
								>transaction</span
							><span v-else>transactions</span>
						</div>
					</div>
				</div>

				<div v-if="queryData.search.transactions.nodes.length > 0">
					<div class="text-xl">Transactions</div>
					<div
						v-for="transaction in queryData.search.transactions.nodes"
						:key="transaction.transactionHash"
					>
						<TransactionLink
							:id="transaction.id"
							:hash="transaction.transactionHash"
							:hide-tooltip="true"
						/>
					</div>
				</div>

				<div v-if="queryData.search.accounts.nodes.length > 0">
					<div class="text-xl">Accounts</div>
					<div
						v-for="account in queryData.search.accounts.nodes"
						:key="account.address"
						class="grid grid-cols-4"
					>
						<AccountLink :address="account.address" :hide-tooltip="true" />
						<div>
							{{ account.transactions.nodes.length }}
							<span v-if="account.transactions.nodes.length === 1"
								>transaction</span
							><span v-else>transactions</span>
						</div>
					</div>
				</div>
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
import { useDrawer } from '~/composables/useDrawer'
import BWCubeLogoIcon from '~/components/icons/BWCubeLogoIcon.vue'
const searchValue = ref('')
const delayedSearchValue = ref('')
const { data: queryData, executeQuery } = useSearchQuery(delayedSearchValue)
let searchQueryTimeout: NodeJS.Timeout | null = null
const drawer = useDrawer()
const loading = ref(true)
watch(queryData, () => {
	loading.value = false
})
watch(searchValue, (newValue, _oldValue) => {
	loading.value = true
	if (searchQueryTimeout) clearTimeout(searchQueryTimeout)
	if (!newValue) {
		delayedSearchValue.value = newValue
	} else
		searchQueryTimeout = setTimeout(() => {
			delayedSearchValue.value = newValue
			executeQuery()
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
		(queryData.value.search.transactions.nodes[0] ||
			queryData.value.search.blocks.nodes[0] ||
			queryData.value.search.accounts.nodes[0])
	) {
		if (queryData.value.search.transactions.nodes[0])
			drawer.push(
				'transaction',
				queryData.value.search.transactions.nodes[0].id,
				queryData.value.search.transactions.nodes[0].transactionHash
			)
		else if (queryData.value.search.blocks.nodes[0])
			drawer.push(
				'block',
				queryData.value.search.blocks.nodes[0].blockHash,
				queryData.value.search.blocks.nodes[0].id
			)
		else if (queryData.value.search.accounts.nodes[0])
			drawer.push(
				'account',
				null,
				null,
				queryData.value.search.accounts.nodes[0].address
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
