<template>
	<div>
		<Title>CCDScan | Dashboard</Title>
		<main class="p-4 pb-0 xl:container xl:mx-auto">
			<section class="flex flex-wrap gap-10">
				<article class="flex flex-col flex-1 mb-12">
					<header class="flex justify-between items-center mb-4">
						<h1 class="text-xl">Latest blocks</h1>
						<NuxtLink to="/blocks">
							<Button>Show all blocks</Button>
						</NuxtLink>
					</header>
					<Table>
						<TableHead>
							<TableRow>
								<TableTh>Height</TableTh>
								<TableTh>Block hash</TableTh>
								<TableTh>Baker</TableTh>
								<TableTh align="right">Baker reward (Ͼ)</TableTh>
							</TableRow>
						</TableHead>

						<TransitionGroup name="list" tag="tbody">
							<TableRow v-for="block in blocks" :key="block.blockHash">
								<TableTd class="numerical">
									<StatusCircle
										:class="[
											'h-4 mr-2 text-theme-interactive',
											{ 'text-theme-info': !block.finalized },
										]"
									/>
									{{ block.blockHeight }}
								</TableTd>
								<TableTd>
									<LinkButton
										class="numerical"
										@click="
											() => {
												drawer.push('block', block.blockHash, block.id)
											}
										"
									>
										<BlockIcon
											class="h-4 text-theme-white inline align-baseline"
										/>
										<Tooltip
											:text="block.blockHash"
											text-class="text-theme-body"
										>
											{{ shortenHash(block.blockHash) }}
										</Tooltip>
									</LinkButton>
								</TableTd>
								<TableTd class="numerical">
									<UserIcon
										v-if="block.bakerId || block.bakerId === 0"
										class="h-4 text-theme-white inline align-baseline"
									/>
									{{ block.bakerId }}
								</TableTd>
								<TableTd align="right" class="numerical">
									{{
										convertMicroCcdToCcd(
											block.specialEvents.blockRewards?.bakerReward
										)
									}}
								</TableTd>
							</TableRow>
						</TransitionGroup>
					</Table>
				</article>

				<article class="flex flex-col flex-1 mb-12">
					<header class="flex justify-between items-center mb-4">
						<h1 class="text-xl">Latest transactions</h1>
						<NuxtLink to="/transactions">
							<Button>Show all transactions</Button>
						</NuxtLink>
					</header>
					<Table>
						<TableHead>
							<TableRow>
								<TableTh>Type</TableTh>
								<TableTh>Transaction hash</TableTh>
								<TableTh>Sender</TableTh>
								<TableTh align="right">Cost (Ͼ)</TableTh>
							</TableRow>
						</TableHead>
						<TableBody>
							<TableRow
								v-for="transaction in transactions"
								:key="transaction.transactionHash"
							>
								<TableTd>
									<StatusCircle
										:class="[
											'h-4 mr-2 text-theme-interactive',
											{
												'text-theme-error':
													transaction.result.__typename === 'Rejected',
											},
										]"
									/>
									{{ translateTransactionType(transaction.transactionType) }}
								</TableTd>
								<TableTd>
									<TransactionIcon class="h-4 w-4" />
									<LinkButton
										class="numerical"
										@click="
											drawer.push(
												'transaction',
												transaction.transactionHash,
												transaction.id
											)
										"
									>
										<Tooltip
											:text="transaction.transactionHash"
											text-class="text-theme-body"
										>
											{{ shortenHash(transaction.transactionHash) }}
										</Tooltip>
									</LinkButton>
								</TableTd>
								<TableTd class="numerical">
									<AccountLink :address="transaction.senderAccountAddress" />
								</TableTd>
								<TableTd align="right" class="numerical">
									{{ convertMicroCcdToCcd(transaction.ccdCost) }}
								</TableTd>
							</TableRow>
						</TableBody>
					</Table>
				</article>
			</section>
		</main>
	</div>
</template>

<script lang="ts" setup>
import { UserIcon } from '@heroicons/vue/solid/index.js'
import BlockIcon from '~/components/icons/BlockIcon.vue'
import Tooltip from '~/components/atoms/Tooltip.vue'
import { useDrawer } from '~/composables/useDrawer'
import { useBlockListQuery } from '~/queries/useBlockListQuery'
import { useTransactionsListQuery } from '~/queries/useTransactionListQuery'
import { useBlockSubscription } from '~/subscriptions/useBlockSubscription'
import { convertMicroCcdToCcd, shortenHash } from '~/utils/format'
import { translateTransactionType } from '~/utils/translateTransactionTypes'
import type { BlockSubscriptionResponse, Block } from '~/types/blocks'
import type { Transaction } from '~/types/transactions'

const pageSize = 10

const subscriptionHandler = (
	_prevData: void,
	newData: BlockSubscriptionResponse
) => {
	blocks.value = [newData.blockAdded, ...blocks.value].slice(0, pageSize)
	transactions.value = [
		...newData.blockAdded.transactions.nodes,
		...transactions.value,
	].slice(0, pageSize)
}

useBlockSubscription(subscriptionHandler)

const blocks = ref<Block[]>([])
const transactions = ref<Transaction[]>([])

const { data: blockData } = useBlockListQuery({ first: pageSize })
const { data: txData } = useTransactionsListQuery({ first: pageSize })

watch(
	() => blockData.value,
	value => {
		blocks.value = value?.blocks.nodes || []
	}
)

watch(
	() => txData.value,
	value => {
		transactions.value = value?.transactions.nodes || []
	}
)
const drawer = useDrawer()
</script>

<style>
.list-move,
.list-enter-active,
.list-leave-active {
	transition: all 0.5s ease;
}

.list-enter-from,
.list-leave-to {
	opacity: 0;
	transform: translateY(-30px);
}

.list-leave-active {
	position: absolute;
}
</style>
