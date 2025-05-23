<template>
	<div
		ref="rootSearchContainer"
		class="lg:relative flex-grow w-min z-20"
		:class="$style.container"
	>
		<div class="relative flex flex-row z-20">
			<div class="hidden md:block pointer-events-none p-2 absolute left-0">
				<SearchIcon class="h-6 md:h-5" />
			</div>
			<input
				v-model="searchValue"
				:class="$style.input"
				class="rounded p-2 w-full focus:ring-2 focus:ring-pink-500 outline-none md:block pl-9"
				placeholder="Search for account, validator, block or transaction &hellip;"
				type="search"
				@blur="lostFocusOnSearch"
				@keyup.enter="gotoSearchResult"
			/>
		</div>

		<div
			v-if="searchValue !== ''"
			class="left-0 lg:left-auto absolute border-theme-selected border solid rounded-lg p-4 bg-theme-background-primary-elevated-nontrans w-full z-20"
			@click="searchValue = ''"
		>
			<div class="overflow-hidden whitespace-nowrap overflow-ellipsis">
				<div v-if="status === 'loading'" class="text-center">
					<BWCubeLogoIcon
						v-if="searchValue"
						class="w-10 h-10 animate-ping"
						:class="$style.loading"
					/>
				</div>

				<div v-else-if="status === 'empty'">No results</div>

				<!--`data` is always present in this state, but vue-tsc doesn't know this -->
				<div v-else-if="status === 'done' && data?.search">
					<SearchResultCategory
						v-if="resultCount.blocks"
						title="Blocks"
						:has-more-results="!!data.search.blocks.pageInfo.hasNextPage"
					>
						<div
							v-for="block in data.search.blocks.nodes"
							:key="block.blockHash"
							:class="$style.searchColumns"
						>
							<div>
								<BlockLink
									:id="block.id"
									:hash="block.blockHash"
									:hide-tooltip="true"
									@blur="lostFocusOnSearch"
								/>
							</div>
							<div :class="$style.threeColumns">@ {{ block.blockHeight }}</div>
							<div :class="$style.twoColumns">
								Age
								<Tooltip :text="formatTimestamp(block.blockSlotTime)">
									{{
										convertTimestampToRelative(block.blockSlotTime || '', NOW)
									}}
								</Tooltip>
							</div>
						</div>
					</SearchResultCategory>

					<SearchResultCategory
						v-if="resultCount.transactions"
						title="Transactions"
						:has-more-results="!!data.search.transactions.pageInfo.hasNextPage"
					>
						<div
							v-for="transaction in data.search.transactions.nodes"
							:key="transaction.transactionHash"
							:class="$style.searchColumns"
						>
							<TransactionLink
								:id="transaction.id"
								:hash="transaction.transactionHash"
								:hide-tooltip="true"
								@blur="lostFocusOnSearch"
							/>
							<div :class="$style.threeColumns">
								<BlockLink
									:id="transaction.block.id"
									:hash="transaction.block.blockHash"
									:hide-tooltip="true"
									@blur="lostFocusOnSearch"
								/>
							</div>
							<div :class="$style.twoColumns">
								Age
								<Tooltip
									:text="formatTimestamp(transaction.block.blockSlotTime)"
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
					</SearchResultCategory>

					<SearchResultCategory
						v-if="resultCount.accounts"
						title="Accounts"
						:has-more-results="data.search.accounts.pageInfo.hasNextPage"
					>
						<div
							v-for="account in data.search.accounts.nodes"
							:key="account.address.asString"
							:class="$style.searchColumns"
						>
							<AccountLink
								:address="account.address.asString"
								:hide-tooltip="true"
								@blur="lostFocusOnSearch"
							/>
							<div :class="$style.threeColumns" />
							<div :class="$style.twoColumns">
								Age
								<Tooltip :text="formatTimestamp(account.createdAt)">
									{{ convertTimestampToRelative(account.createdAt || '', NOW) }}
								</Tooltip>
							</div>
						</div>
					</SearchResultCategory>

					<SearchResultCategory
						v-if="resultCount.contracts"
						title="Contracts"
						:has-more-results="!!data.search.contracts.pageInfo.hasNextPage"
					>
						<div
							v-for="contract in data.search.contracts.nodes"
							:key="contract.contractAddress"
							:class="$style.searchColumns"
						>
							<ContractLink
								:address="contract.contractAddress"
								:contract-address-index="contract.contractAddressIndex"
								:contract-address-sub-index="contract.contractAddressSubIndex"
								:hide-tooltip="true"
								@blur="lostFocusOnSearch"
							/>
							<AccountLink
								:class="$style.threeColumns"
								:address="contract.creator.asString"
							/>
							<div :class="$style.twoColumns">
								Age
								<Tooltip :text="formatTimestamp(contract.blockSlotTime)">
									{{ convertTimestampToRelative(contract.blockSlotTime, NOW) }}
								</Tooltip>
							</div>
						</div>
					</SearchResultCategory>
					<SearchResultCategory
						v-if="resultCount.tokens"
						title="CIS-2 Tokens"
						:has-more-results="data.search.tokens.pageInfo.hasNextPage"
					>
						<div
							v-for="token in data.search.tokens.nodes"
							:key="token.tokenAddress"
							:class="$style.searchColumns"
						>
							<TokenLink
								:token-address="token.tokenAddress"
								:token-id="token.tokenId"
								:contract-address-index="token.contractIndex"
								:contract-address-sub-index="token.contractSubIndex"
								:width="80"
							/>
							<div :class="$style.threeColumns" />
							<div :class="$style.twoColumns">
								<TransactionLink
									:hash="token.initialTransaction.transactionHash"
								/>
							</div>
						</div>
					</SearchResultCategory>
					<SearchResultCategory
						v-if="resultCount.modules"
						title="Modules"
						:has-more-results="data.search.modules.pageInfo.hasNextPage"
					>
						<div
							v-for="module in data.search.modules.nodes"
							:key="module.moduleReference"
							:class="$style.searchColumns"
						>
							<ModuleLink
								:module-reference="module.moduleReference"
								:hide-tooltip="true"
								@blur="lostFocusOnSearch"
							/>
							<div :class="$style.threeColumns" />
							<div :class="$style.twoColumns">
								Age
								<Tooltip :text="formatTimestamp(module.blockSlotTime)">
									{{ convertTimestampToRelative(module.blockSlotTime, NOW) }}
								</Tooltip>
							</div>
						</div>
					</SearchResultCategory>

					<SearchResultCategory
						v-if="resultCount.bakers"
						title="Validators"
						:has-more-results="data.search.bakers.pageInfo.hasNextPage"
					>
						<div
							v-for="baker in data.search.bakers.nodes"
							:key="baker.bakerId"
							:class="$style.searchColumns"
						>
							<BakerLink :id="baker.bakerId" @blur="lostFocusOnSearch" />
							<div :class="$style.threeColumns" />
							<div :class="$style.twoColumns">
								<AccountLink
									:address="baker.account.address.asString"
									:hide-tooltip="true"
									@blur="lostFocusOnSearch"
								/>
							</div>
						</div>
					</SearchResultCategory>

					<SearchResultCategory
						v-if="resultCount.nodeStatuses"
						title="Nodes"
						:has-more-results="data.search.nodeStatuses.pageInfo.hasNextPage"
					>
						<div
							v-for="node in data.search.nodeStatuses.nodes"
							:key="node.id"
							:class="$style.searchColumns"
						>
							<NodeLink
								:node-id="node.nodeId"
								:node-name="node.nodeName"
								@blur="lostFocusOnSearch"
							/>
							<div :class="$style.threeColumns" />
							<div :class="$style.twoColumns">
								<!-- We checked that the value is an integer, so the type cast with `!` is safe -->
								<BakerLink
									v-if="Number.isInteger(node.consensusBakerId)"
									:id="node.consensusBakerId!"
								/>
							</div>
						</div>
					</SearchResultCategory>
				</div>
			</div>
		</div>

		<div v-if="status !== 'idle'" :class="$style.mask" />
	</div>
</template>

<script lang="ts" setup>
import ModuleLink from '../molecules/ModuleLink.vue'
import TransactionLink from '../molecules/TransactionLink.vue'
import TokenLink from '../molecules/TokenLink.vue'
import SearchResultCategory from './SearchResultCategory.vue'
import { useSearchQuery } from '~/queries/useSearchQuery'
import { useDrawer } from '~/composables/useDrawer'
import BWCubeLogoIcon from '~/components/icons/BWCubeLogoIcon.vue'
import SearchIcon from '~/components/icons/SearchIcon.vue'
import { formatTimestamp, convertTimestampToRelative } from '~/utils/format'
import BlockLink from '~/components/molecules/BlockLink.vue'
import BakerLink from '~/components/molecules/BakerLink.vue'
import AccountLink from '~/components/molecules/AccountLink.vue'
import ContractLink from '~/components/molecules/ContractLink.vue'
import { useDateNow } from '~/composables/useDateNow'
import NodeLink from '~/components/molecules/NodeLink.vue'

const { NOW } = useDateNow()
const drawer = useDrawer()

const searchValue = ref('')
const delayedSearchValue = ref('')
const isMaskVisible = ref(false)

const { data, executeQuery } = useSearchQuery(delayedSearchValue)
let searchQueryTimeout: NodeJS.Timeout | null = null

const lastSearchTerm = ref('')

const status = ref<'idle' | 'loading' | 'empty' | 'done'>('idle')

watch(data, () => {
	if (searchValue.value) {
		status.value = resultCount.value.total === 0 ? 'empty' : 'done'
	}
})

watch(searchValue, newValue => {
	if (searchQueryTimeout) clearTimeout(searchQueryTimeout)
	status.value = 'loading'
	isMaskVisible.value = true

	if (!newValue) {
		status.value = 'idle'
		delayedSearchValue.value = newValue
	} else
		searchQueryTimeout = setTimeout(() => {
			delayedSearchValue.value = newValue
			lastSearchTerm.value = delayedSearchValue.value
			executeQuery()
		}, 500)
})

const gotoSearchResult = () => {
	if (status.value !== 'done' || !data.value || resultCount.value.total > 1)
		return

	if (data.value.search.transactions.nodes[0])
		drawer.push({
			entityTypeName: 'transaction',
			hash: data.value.search.transactions.nodes[0].transactionHash,
			id: data.value.search.transactions.nodes[0].id,
		})
	else if (data.value.search.modules.nodes[0])
		drawer.push({
			entityTypeName: 'module',
			moduleReference: data.value.search.modules.nodes[0].moduleReference,
		})
	else if (data.value.search.blocks.nodes[0])
		drawer.push({
			entityTypeName: 'block',
			hash: data.value.search.blocks.nodes[0].blockHash,
		})
	else if (data.value.search.accounts.nodes[0])
		drawer.push({
			entityTypeName: 'account',
			address: data.value.search.accounts.nodes[0].address.asString,
		})
	else if (data.value.search.bakers.nodes[0])
		drawer.push({
			entityTypeName: 'validator',
			id: data.value.search.bakers.nodes[0].bakerId,
		})
	else if (data.value.search.nodeStatuses.nodes[0])
		drawer.push({
			entityTypeName: 'node',
			nodeId: data.value.search.nodeStatuses.nodes[0].id,
		})
	else if (data.value.search.contracts.nodes[0])
		drawer.push({
			entityTypeName: 'contract',
			contractAddressIndex:
				data.value.search.contracts.nodes[0].contractAddressIndex,
			contractAddressSubIndex:
				data.value.search.contracts.nodes[0].contractAddressSubIndex,
		})
	searchValue.value = ''
	status.value = 'idle'
	isMaskVisible.value = false
}

const rootSearchContainer = ref()
const lostFocusOnSearch = (x: FocusEvent) => {
	if (x.relatedTarget && rootSearchContainer.value.contains(x.relatedTarget))
		return
	setTimeout(() => {
		searchValue.value = ''
		status.value = 'idle'
	}, 100)
}

const resultCount = computed(() => ({
	modules: data.value?.search.modules.nodes.length,
	contracts: data.value?.search.contracts.nodes.length,
	blocks: data.value?.search.blocks.nodes.length,
	transactions: data.value?.search.transactions.nodes.length,
	accounts: data.value?.search.accounts.nodes.length,
	bakers: data.value?.search.bakers.nodes.length,
	nodeStatuses: data.value?.search.nodeStatuses.nodes.length,
	tokens: data.value?.search.tokens.nodes.length,
	total:
		(data.value?.search.contracts.nodes.length ?? 0) +
		(data.value?.search.blocks.nodes.length ?? 0) +
		(data.value?.search.transactions.nodes.length ?? 0) +
		(data.value?.search.accounts.nodes.length ?? 0) +
		(data.value?.search.bakers.nodes.length ?? 0) +
		(data.value?.search.nodeStatuses.nodes.length ?? 0) +
		(data.value?.search.modules?.nodes.length ?? 0) +
		(data.value?.search.tokens?.nodes.length ?? 0),
}))
</script>

<style module>
.twoColumns {
	@media (max-width: 640px) {
		display: none;
	}
}
.threeColumns {
	@media (max-width: 639px) {
		display: none;
	}
}

.searchColumns {
	display: grid;
	grid-template-columns: repeat(1, minmax(0, 1fr));
	@media (min-width: 640px) {
		grid-template-columns: repeat(3, minmax(0, 1fr));
	}
}

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
	color: var(--color-input-placeholder);
	font-style: italic;
}

.loading {
	min-height: 100px;
}

.container {
	max-width: 600px;
}

.mask {
	position: fixed;
	top: 0;
	left: 0;
	width: 100%;
	height: 100%;
	background: hsla(247, 40%, 4%, 0.5);
	backdrop-filter: blur(2px);
	z-index: 11;
}
</style>
