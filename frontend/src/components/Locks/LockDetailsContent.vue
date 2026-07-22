<template>
	<LockDetailsHeader :lock="lock" />
	<DrawerContent>
		<div class="grid gap-8 md:grid-cols-3 mb-12">
			<DetailsCard v-if="lock.createdAt">
				<template #title>Created</template>
				<template #default>
					{{ formatTimestamp(lock.createdAt) }}
				</template>
				<template v-if="lock.createdTransaction" #secondary>
					<TransactionLink :hash="lock.createdTransaction.transactionHash" />
				</template>
			</DetailsCard>
			<DetailsCard v-if="lock.expiry">
				<template #title>Expiry</template>
				<template #default>
					{{ formatTimestamp(lock.expiry) }}
				</template>
			</DetailsCard>
			<DetailsCard v-if="lock.canceledAt">
				<template #title>Canceled</template>
				<template #default>
					{{ formatTimestamp(lock.canceledAt) }}
				</template>
				<template v-if="lock.canceledTransaction" #secondary>
					<TransactionLink :hash="lock.canceledTransaction.transactionHash" />
				</template>
			</DetailsCard>
			<DetailsCard v-if="lock.creator?.address.asString">
				<template #title>Creator</template>
				<template #default>
					<AccountLink
						icon-size="big"
						:address="lock.creator.address.asString"
					/>
				</template>
			</DetailsCard>
			<DetailsCard v-if="metadataName">
				<template #title>Name</template>
				<template #default>{{ metadataName }}</template>
			</DetailsCard>
			<DetailsCard v-if="metadataDescription" class="md:col-span-2">
				<template #title>Description</template>
				<template #default>{{ metadataDescription }}</template>
			</DetailsCard>
		</div>
	</DrawerContent>

	<DrawerContent>
		<section class="mb-12">
			<h2 class="text-xl mb-4">Current balances</h2>
			<Table v-if="lock.balances.length" :class="{ fetching }">
				<TableHead>
					<TableRow>
						<TableTh>Account</TableTh>
						<TableTh>Token</TableTh>
						<TableTh align="right">Amount</TableTh>
					</TableRow>
				</TableHead>
				<TableBody>
					<TableRow
						v-for="balance in lock.balances"
						:key="`${balance.accountAddress.asString}-${balance.tokenId}`"
					>
						<TableTd>
							<AccountLink :address="balance.accountAddress.asString" />
						</TableTd>
						<TableTd>
							<span class="numerical">{{ balance.tokenId }}</span>
						</TableTd>
						<TableTd align="right">
							<PltAmount
								:value="balance.amount.value"
								:decimals="Number(balance.amount.decimals)"
							/>
						</TableTd>
					</TableRow>
				</TableBody>
			</Table>
			<p v-else class="text-theme-faded">No current locked balances.</p>
		</section>
	</DrawerContent>

	<DrawerContent>
		<section class="mb-12">
			<h2 class="text-xl mb-4">Configuration</h2>
			<div v-if="lock.config" class="grid gap-8 md:grid-cols-2">
				<div>
					<h3 class="text-sm text-theme-faded mb-2">Recipients</h3>
					<p v-if="lock.config.recipients.recipientType === 'Any'">
						Any eligible recipient
					</p>
					<ul v-else class="space-y-2">
						<li
							v-for="account in lock.config.recipients.accounts"
							:key="account.address.asString"
						>
							<AccountLink :address="account.address.asString" />
						</li>
					</ul>
				</div>
				<div v-if="simpleController">
					<h3 class="text-sm text-theme-faded mb-2">Controller</h3>
					<p>
						SimpleV0
						<span v-if="simpleController.keepAlive" class="text-theme-faded">
							keep alive
						</span>
					</p>
					<p v-if="simpleController.tokenIds.length" class="mt-2">
						Tokens:
						<span class="numerical">{{ simpleController.tokenIds.join(', ') }}</span>
					</p>
				</div>
				<div v-if="simpleController?.grants.length" class="md:col-span-2">
					<h3 class="text-sm text-theme-faded mb-2">Controller grants</h3>
					<Table>
						<TableHead>
							<TableRow>
								<TableTh>Account</TableTh>
								<TableTh>Roles</TableTh>
							</TableRow>
						</TableHead>
						<TableBody>
							<TableRow
								v-for="grant in simpleController.grants"
								:key="grant.account.address.asString"
							>
								<TableTd>
									<AccountLink :address="grant.account.address.asString" />
								</TableTd>
								<TableTd>{{ grant.roles.join(', ') }}</TableTd>
							</TableRow>
						</TableBody>
					</Table>
				</div>
			</div>
			<p v-else-if="lock.rawConfig" class="text-theme-faded">
				Raw config is indexed, but decoded config is unavailable.
			</p>
			<p v-else class="text-theme-faded">No decoded configuration available.</p>
		</section>
	</DrawerContent>

	<DrawerContent>
		<section class="mb-12">
			<h2 class="text-xl mb-4">Lifecycle history</h2>
			<Table v-if="historyItems.length" :class="{ fetching }">
				<TableHead>
					<TableRow>
						<TableTh>Transaction</TableTh>
						<TableTh>Type</TableTh>
						<TableTh>Time</TableTh>
						<TableTh>Details</TableTh>
					</TableRow>
				</TableHead>
				<TableBody>
					<TableRow v-for="event in historyItems" :key="event.id">
						<TableTd>
							<TransactionLink :hash="event.transaction.transactionHash" />
						</TableTd>
						<TableTd>{{ formatEventType(event.eventType) }}</TableTd>
						<TableTd>{{ formatTimestamp(event.slotTime) }}</TableTd>
						<TableTd>
							<template v-if="event.amount">
								<PltAmount
									:value="event.amount.value"
									:decimals="Number(event.amount.decimals)"
								/>
								<span v-if="event.tokenId" class="numerical ml-1">
									{{ event.tokenId }}
								</span>
							</template>
							<template v-if="event.account?.address.asString">
								<span class="mx-1 text-theme-faded">account</span>
								<AccountLink :address="event.account.address.asString" />
							</template>
							<template v-if="event.source?.address.asString">
								<span class="mx-1 text-theme-faded">from</span>
								<AccountLink :address="event.source.address.asString" />
							</template>
							<template v-if="event.recipient?.address.asString">
								<span class="mx-1 text-theme-faded">to</span>
								<AccountLink :address="event.recipient.address.asString" />
							</template>
						</TableTd>
					</TableRow>
				</TableBody>
			</Table>
			<p v-else class="text-theme-faded">No lifecycle history indexed.</p>
			<p v-if="lock.history.pageInfo.hasNextPage" class="text-theme-faded mt-4">
				Showing the first 50 history events.
			</p>
		</section>
	</DrawerContent>
</template>

<script lang="ts" setup>
import LockDetailsHeader from './LockDetailsHeader.vue'
import DrawerContent from '~/components/Drawer/DrawerContent.vue'
import DetailsCard from '~/components/DetailsCard.vue'
import PltAmount from '~/components/atoms/PltAmount.vue'
import AccountLink from '~/components/molecules/AccountLink.vue'
import TransactionLink from '~/components/molecules/TransactionLink.vue'
import { formatTimestamp } from '~/utils/format'
import type { Lock } from '~/types/generated'

type Props = {
	lock: Lock
	fetching: boolean
}

const props = defineProps<Props>()

const metadataName = computed(
	() => props.lock.config?.metadata?.name ?? props.lock.metadataName
)
const metadataDescription = computed(
	() => props.lock.config?.metadata?.description ?? props.lock.metadataDescription
)
const simpleController = computed(
	() => props.lock.config?.controller.simpleV0 ?? null
)
const historyItems = computed(() => props.lock.history.nodes ?? [])

const formatEventType = (eventType: string) =>
	eventType
		.toLowerCase()
		.split('_')
		.map(part => part.charAt(0).toUpperCase() + part.slice(1))
		.join(' ')
</script>

<style scoped>
.fetching tbody tr {
	opacity: 0.4;
}
</style>
