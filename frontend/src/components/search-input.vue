<template>
	<div ref="rootSearchContainer" class="md:relative md:w-1/3 w-full">
		<div class="relative">
			<input
				v-model="searchValue"
				:class="$style.input"
				class="rounded p-2 w-full focus:ring-2 focus:ring-pink-500 outline-none md:block"
				placeholder="Search for account, block or transaction &hellip;"
				type="search"
				@blur="lostFocusOnSearch"
				@keyup.enter="gotoSearchResult"
			/>
		</div>

		<div
			v-if="searchValue !== ''"
			class="left-0 md:left-auto absolute border-theme-selected border solid rounded-lg p-4 bg-theme-background-primary-elevated-nontrans w-full z-10"
			@click="searchValue = ''"
		>
			<div class="overflow-hidden whitespace-nowrap overflow-ellipsis">
				<div v-if="loading" class="text-center">
					<BWCubeLogoIcon
						v-if="loading && searchValue"
						class="w-10 h-10 animate-ping"
						:class="$style.loading"
					/>
				</div>
				<div
					v-else-if="
						!queryData ||
						(queryData &&
							!(
								queryData.search.blocks.nodes.length > 0 ||
								queryData.search.transactions.nodes.length > 0 ||
								queryData.search.accounts.nodes.length > 0
							))
					"
				>
					No results.
				</div>
				<div
					v-if="
						!loading &&
						queryData &&
						(queryData.search.blocks.nodes.length > 0 ||
							queryData.search.transactions.nodes.length > 0 ||
							queryData.search.accounts.nodes.length > 0)
					"
				>
					<div v-if="queryData.search.blocks.nodes.length > 0">
						<div class="text-xl">Blocks</div>
						<div
							v-for="(block, index) in queryData.search.blocks.nodes"
							:key="block.blockHash"
							class="grid grid-cols-4"
						>
							<div>
								<BlockLink
									:id="block.id"
									:hash="block.blockHash"
									:hide-tooltip="true"
									@blur="lostFocusOnSearch"
								/>
							</div>
							<div>@ {{ block.blockHeight }}</div>

							<div>
								Created
								<Tooltip
									:text="block.blockSlotTime"
									:position="index === 0 ? tooltipPositionBottom : ''"
								>
									{{
										convertTimestampToRelative(block.blockSlotTime || '', NOW)
									}}
								</Tooltip>
							</div>
						</div>
						<div
							v-if="queryData.search.blocks.pageInfo.hasNextPage"
							class="text-theme-faded"
						>
							&hellip; more than 3 results!
						</div>
					</div>

					<div v-if="queryData.search.transactions.nodes.length > 0">
						<div class="text-xl">Transactions</div>
						<div
							v-for="(transaction, index) in queryData.search.transactions
								.nodes"
							:key="transaction.transactionHash"
							class="grid grid-cols-4"
						>
							<TransactionLink
								:id="transaction.id"
								:hash="transaction.transactionHash"
								:hide-tooltip="true"
								@blur="lostFocusOnSearch"
							/>
							<div>
								<BlockLink
									:id="transaction.block.id"
									:hash="transaction.block.blockHash"
									:hide-tooltip="true"
									@blur="lostFocusOnSearch"
								/>
							</div>
							<div>
								Created
								<Tooltip
									:text="transaction.block.blockSlotTime"
									:position="index === 0 ? tooltipPositionBottom : ''"
								>
									{{
										convertTimestampToRelative(
											transaction.block.blockSlotTime || '',
											NOW
										)
									}}
								</Tooltip>
							</div>
						</div>
						<div
							v-if="queryData.search.transactions.pageInfo.hasNextPage"
							class="text-theme-faded"
						>
							&hellip; more than 3 results!
						</div>
					</div>

					<div v-if="queryData.search.accounts.nodes.length > 0">
						<div class="text-xl">Accounts</div>
						<div
							v-for="(account, index) in queryData.search.accounts.nodes"
							:key="account.address"
							class="grid grid-cols-4"
						>
							<AccountLink
								:address="account.address"
								:hide-tooltip="true"
								@blur="lostFocusOnSearch"
							/>
							<div></div>
							<div>
								Created
								<Tooltip
									:text="account.createdAt"
									:position="index === 0 ? tooltipPositionBottom : ''"
								>
									{{ convertTimestampToRelative(account.createdAt || '', NOW) }}
								</Tooltip>
							</div>
						</div>
						<div
							v-if="queryData.search.accounts.pageInfo.hasNextPage"
							class="text-theme-faded"
						>
							&hellip; more than 3 results!
						</div>
					</div>
				</div>
			</div>
		</div>
		<div :class="$style.button">
			<SearchIcon class="h-6 md:h-5" />
		</div>
	</div>
</template>

<script lang="ts" setup>
import { SearchIcon } from '@heroicons/vue/outline/index.js'
import { useSearchQuery } from '~/queries/useSearchQuery'
import { useDrawer } from '~/composables/useDrawer'
import BWCubeLogoIcon from '~/components/icons/BWCubeLogoIcon.vue'
import { convertTimestampToRelative } from '~/utils/format'
import BlockLink from '~/components/molecules/BlockLink.vue'
import AccountLink from '~/components/molecules/AccountLink.vue'
import type { Position } from '~/composables/useTooltip'
const tooltipPositionBottom = 'bottom' as Position
const searchValue = ref('')
const delayedSearchValue = ref('')
const queryData = ref()
const { data: rawQueryData, executeQuery } = useSearchQuery(delayedSearchValue)
let searchQueryTimeout: NodeJS.Timeout | null = null
const drawer = useDrawer()
const loading = ref(true)
const lastSearchTerm = ref('')
const NOW = ref(new Date())
watch(rawQueryData, () => {
	loading.value = false
	NOW.value = new Date()
	if (lastSearchTerm.value === searchValue.value) {
		queryData.value = rawQueryData.value
	} else {
		queryData.value = null
	}
})
watch(searchValue, (newValue, _oldValue) => {
	if (searchQueryTimeout) clearTimeout(searchQueryTimeout)
	loading.value = true

	if (!newValue) {
		delayedSearchValue.value = newValue
	} else
		searchQueryTimeout = setTimeout(() => {
			delayedSearchValue.value = newValue
			lastSearchTerm.value = delayedSearchValue.value
			executeQuery()
		}, 500)
})
const gotoSearchResult = () => {
	if (
		(searchValue.value !== delayedSearchValue.value &&
			!delayedSearchValue.value) ||
		loading.value ||
		queryData.value.search.transactions.nodes.length > 1 ||
		queryData.value.search.blocks.nodes.length > 1 ||
		queryData.value.search.accounts.nodes.length > 1
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
				queryData.value.search.transactions.nodes[0].transactionHash,
				queryData.value.search.transactions.nodes[0].id
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
const rootSearchContainer = ref()
const lostFocusOnSearch = (x: FocusEvent) => {
	if (
		x &&
		x.relatedTarget &&
		rootSearchContainer.value.contains(x.relatedTarget)
	)
		return
	searchValue.value = ''
}
</script>

<style module>
.input {
	background: var(--color-input-bg);
	-webkit-appearance: none;
}
.input::-webkit-search-cancel-button {
	-webkit-appearance: none;
	height: 16px;
	width: 16px;
	background-image: url('data:image/svg+xml;utf8,<svg style="color:white" stroke="currentColor" fill="currentColor" stroke-width="0" viewBox="0 0 24 24" height="1em" width="1em" xmlns="http://www.w3.org/2000/svg"><path fill="none" d="M0 0h24v24H0z"></path><path d="M19 6.41L17.59 5 12 10.59 6.41 5 5 6.41 10.59 12 5 17.59 6.41 19 12 13.41 17.59 19 19 17.59 13.41 12z"></path></svg>');
}
.input::placeholder {
	@apply italic;
	color: var(--color-input-placeholder);
}

.button {
	@apply absolute top-1/2 right-8;
	transform: translateY(-50%);
	pointer-events: none;
}
.loading {
	min-height: 100px;
}
</style>
