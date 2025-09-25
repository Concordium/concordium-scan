<template>
	<div>
		<Title>CCDScan | All Protocol Level Tokens</Title>

		<div class="mb-8">
			<header class="flex justify-between items-center mb-6">
				<div>
					<h1 class="text-2xl font-bold">All Protocol Level Tokens</h1>
					<p class="text-theme-text-secondary mt-1">
						Complete list of all available PLT tokens on the network
					</p>
				</div>
				<NuxtLink
					to="/protocol-token"
					class="text-sm text-theme-text-secondary hover:text-theme-interactive transition-colors duration-200 flex items-center gap-1"
				>
					<svg
						class="w-4 h-4"
						fill="none"
						stroke="currentColor"
						viewBox="0 0 24 24"
					>
						<path
							stroke-linecap="round"
							stroke-linejoin="round"
							stroke-width="2"
							d="M15 19l-7-7 7-7"
						></path>
					</svg>
					<span>Back to Overview</span>
				</NuxtLink>
			</header>

			<!-- Tokens List -->
			<div
				class="bg-theme-background-primary rounded-lg border border-theme-border overflow-hidden"
			>
				<div class="overflow-x-auto">
					<table class="w-full">
						<thead
							class="bg-theme-background-secondary border-b border-theme-border"
						>
							<tr>
								<th
									class="text-left py-3 px-4 font-medium text-theme-text-secondary"
								>
									Sl. No.
								</th>
								<th
									class="text-left py-3 px-4 font-medium text-theme-text-secondary"
								>
									Token
								</th>
								<th
									class="text-left py-3 px-4 font-medium text-theme-text-secondary"
								>
									Token ID
								</th>
								<th
									class="text-left py-3 px-4 font-medium text-theme-text-secondary"
								>
									Total Supply
								</th>
								<th
									class="text-left py-3 px-4 font-medium text-theme-text-secondary"
								>
									Minted
								</th>
								<th
									class="text-left py-3 px-4 font-medium text-theme-text-secondary"
								>
									Burned
								</th>
								<th
									class="text-left py-3 px-4 font-medium text-theme-text-secondary"
								>
									Holders
								</th>
								<th
									class="text-left py-3 px-4 font-medium text-theme-text-secondary"
								>
									Issuer
								</th>
								<th
									class="text-left py-3 px-4 font-medium text-theme-text-secondary"
								>
									Created
								</th>
							</tr>
						</thead>
						<tbody>
							<!-- Loading State -->
							<TableRow v-if="pltTokenLoading">
								<td colspan="9" class="text-center py-8">
									<LoadingIndicator />
								</td>
							</TableRow>

							<!-- Empty State -->
							<TableRow v-else-if="!pltTokenData?.length">
								<td
									colspan="9"
									class="text-center py-8 text-theme-text-secondary"
								>
									No tokens available.
								</td>
							</TableRow>

							<!-- Token Rows -->
							<TableRow
								v-for="(token, index) in pltTokenData"
								:key="token.tokenId"
								class="hover:bg-theme-background-secondary transition-colors duration-200"
							>
								<td class="py-4 px-4">
									<span class="text-sm font-medium text-theme-text-secondary">
										{{ (currentPage - 1) * pageSize + index + 1 }}
									</span>
								</td>
								<td class="py-4 px-4">
									<div class="flex items-center gap-3">
										<div>
											<div class="font-medium">
												{{ token.name || 'Unnamed Token' }}
											</div>
											<div class="text-sm text-theme-text-secondary">
												{{ token.tokenId }}
											</div>
										</div>
									</div>
								</td>
								<td class="py-4 px-4">
									<NuxtLink
										:to="`/protocol-token/${token.tokenId}`"
										class="text-theme-interactive hover:underline font-mono text-sm"
									>
										{{ truncateAddress(token.tokenId) }}
									</NuxtLink>
								</td>
								<td class="py-4 px-4">
									<div class="font-medium">
										<PltAmount
											:value="String(token.totalSupply || '0')"
											:decimals="token.decimal || 0"
											:fixed-decimals="2"
											:format-number="true"
										/>
									</div>
								</td>
								<td class="py-4 px-4">
									<div class="font-medium">
										<PltAmount
											:value="String(token.totalMinted || '0')"
											:decimals="token.decimal || 0"
											:fixed-decimals="2"
											:format-number="true"
										/>
									</div>
								</td>
								<td class="py-4 px-4">
									<div class="font-medium">
										<PltAmount
											:value="String(token.totalBurned || '0')"
											:decimals="token.decimal || 0"
											:fixed-decimals="2"
											:format-number="true"
										/>
									</div>
								</td>
								<td class="py-4 px-4">
									<span class="font-medium">
										{{ token.totalUniqueHolders || 0 }}
									</span>
								</td>
								<td class="py-4 px-4">
									<AccountLink
										v-if="token.issuer?.asString"
										:address="token.issuer.asString"
									/>
									<span v-else class="text-theme-text-secondary text-sm"
										>N/A</span
									>
								</td>
								<td class="py-4 px-4">
									<Tooltip
										v-if="token.block?.blockSlotTime"
										:text="formatTimestamp(token.block.blockSlotTime)"
									>
										<span class="text-sm text-theme-text-secondary">
											{{
												convertTimestampToRelative(
													token.block.blockSlotTime,
													NOW
												)
											}}
										</span>
									</Tooltip>
									<span v-else class="text-sm text-theme-text-secondary"
										>N/A</span
									>
								</td>
							</TableRow>
						</tbody>
					</table>
				</div>
			</div>

			<Pagination
				v-if="pageInfo"
				:page-info="pageInfo"
				:go-to-page="goToPage"
			/>
		</div>
	</div>
</template>

<script lang="ts" setup>
import { ref, watch } from 'vue'
import { usePltTokensPagedQuery } from '~/queries/usePltTokensPagedQuery'
import { usePagination } from '~/composables/usePagination'
import { useDateNow } from '~/composables/useDateNow'
import { formatTimestamp, convertTimestampToRelative } from '~/utils/format'

definePageMeta({
	middleware: 'plt-features-guard',
})

const { NOW } = useDateNow()

const pageSize = 25

const { after, before, first, last, goToPage } = usePagination({
	pageSize,
})

const currentPage = ref(1)

watch([after, before], () => {
	if (!after.value && !before.value) {
		currentPage.value = 1
	} else if (after.value) {
		currentPage.value = currentPage.value + 1
	} else if (before.value) {
		currentPage.value = Math.max(1, currentPage.value - 1)
	}
})

const {
	data,
	pageInfo,
	loading: pltTokenLoading,
} = usePltTokensPagedQuery({
	first,
	last,
	after,
	before,
})

const pltTokenData = ref(data.value)

watch(
	() => data.value,
	newData => {
		pltTokenData.value = newData
	},
	{ immediate: true }
)

const truncateAddress = (address: string, length: number = 12) => {
	if (!address) return ''
	if (address.length <= length) return address
	return `${address.slice(0, length / 2)}...${address.slice(-length / 2)}`
}
</script>
