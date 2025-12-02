<template>
	<div>
		<Title>CCDScan | All Protocol Level Tokens</Title>

		<div class="mb-8">
			<header class="flex justify-between items-center mb-6">
				<div>
					<h1 class="text-2xl font-bold">All Protocol Level Tokens</h1>
					<p class="text-theme-text-secondary mt-1">
						List of all available tokens on the network
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
			<Table>
				<TableHead>
					<TableRow>
						<TableTh>Name</TableTh>
						<TableTh>Token ID</TableTh>
						<TableTh>Total Supply</TableTh>
						<TableTh>Minted</TableTh>
						<TableTh>Burned</TableTh>
						<TableTh>Holders</TableTh>
						<TableTh>Issuer</TableTh>
						<TableTh>Age</TableTh>
					</TableRow>
				</TableHead>
				<TableBody>
					<!-- Loading State -->
					<TableRow v-if="pltTokenLoading">
						<TableTd colspan="9" align="center" class="py-8">
							<LoadingIndicator />
						</TableTd>
					</TableRow>

					<!-- Empty State -->
					<TableRow v-else-if="!pltTokenData?.length">
						<TableTd
							colspan="9"
							align="center"
							class="py-8 text-theme-text-secondary"
						>
							No tokens available.
						</TableTd>
					</TableRow>

					<!-- Token Rows -->
					<TableRow v-for="token in pltTokenData" :key="token.tokenId">
						<TableTd>
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
						</TableTd>
						<TableTd>
							<NuxtLink
								:to="`/protocol-token/${token.tokenId}`"
								class="text-theme-interactive hover:underline font-mono text-sm"
							>
								{{ truncateString(token.tokenId) }}
							</NuxtLink>
						</TableTd>
						<TableTd>
							<div class="font-medium">
								<PltAmount
									:value="String(token.totalSupply || '0')"
									:decimals="token.decimal || 0"
									:fixed-decimals="2"
									:format-number="true"
								/>
							</div>
						</TableTd>
						<TableTd>
							<div class="font-medium">
								<PltAmount
									:value="String(token.totalMinted || '0')"
									:decimals="token.decimal || 0"
									:fixed-decimals="2"
									:format-number="true"
								/>
							</div>
						</TableTd>
						<TableTd>
							<div class="font-medium">
								<PltAmount
									:value="String(token.totalBurned || '0')"
									:decimals="token.decimal || 0"
									:fixed-decimals="2"
									:format-number="true"
								/>
							</div>
						</TableTd>
						<TableTd>
							<span class="font-medium">
								{{ token.totalUniqueHolders || 0 }}
							</span>
						</TableTd>
						<TableTd>
							<AccountLink
								v-if="token.issuer?.asString"
								:address="token.issuer.asString"
							/>
							<span v-else class="text-theme-text-secondary text-sm">N/A</span>
						</TableTd>
						<TableTd>
							<Tooltip
								v-if="token.block?.blockSlotTime"
								:text="formatTimestamp(token.block.blockSlotTime)"
							>
								<span class="text-sm text-theme-text-secondary">
									{{
										convertTimestampToRelative(token.block.blockSlotTime, NOW)
									}}
								</span>
							</Tooltip>
							<span v-else class="text-sm text-theme-text-secondary">N/A</span>
						</TableTd>
					</TableRow>
				</TableBody>
			</Table>

			<Pagination
				v-if="pageInfo"
				:page-info="pageInfo"
				:go-to-page="goToPage"
			/>
		</div>
	</div>
</template>

<script lang="ts" setup>
import { computed } from 'vue'
import { usePltTokensPagedQuery } from '~/queries/usePltTokensPagedQuery'
import { usePagination } from '~/composables/usePagination'
import { useDateNow } from '~/composables/useDateNow'
import { formatTimestamp, convertTimestampToRelative } from '~/utils/format'

const { NOW } = useDateNow()

const pageSize = 25

const { after, before, first, last, goToPage } = usePagination({
	pageSize,
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

const pltTokenData = computed(() => data.value || [])

/**
 * Truncate a string to a specified length with ellipsis.
 * @param value The string value to truncate.
 * @param length The maximum length of the truncated string.
 */
const truncateString = (value: string, length: number = 12) => {
	if (!value) return ''
	if (value.length <= length) return value
	return `${value.slice(0, length / 2)}...${value.slice(-length / 2)}`
}
</script>
