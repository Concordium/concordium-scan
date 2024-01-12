<template>
	<TokenDetailsHeader :token-id="token.tokenId" />
	<DrawerContent>
		<div class="flex flex-row flex-wrap gap-5 md:gap-20 mb-6 md:mb-12">
			<DetailsCard>
				<template #title>Date</template>
				<template #default>
					{{ formatTimestamp(token.initialTransaction.block.blockSlotTime) }}
				</template>
				<template v-if="breakpoint >= Breakpoint.LG" #secondary>
					{{
						convertTimestampToRelative(
							token.initialTransaction.block.blockSlotTime,
							NOW
						)
					}}
				</template>
			</DetailsCard>
			<DetailsCard class="numeric-right-align">
				<template #title>Supply {{ token.metadata?.symbol ?? '' }}</template>
				<template #default>
					<TokenAmount :amount="String(token.totalSupply)" />
				</template>
			</DetailsCard>
			<template v-if="token.metadata && token.metadataUrl">
				<DetailsCard v-if="token.metadata.name">
					<template #title>Name</template>
					<template #default> {{ token.metadata.name }}</template>
				</DetailsCard>
				<DetailsCard v-if="token.metadata.symbol">
					<template #title>Symbol</template>
					<template #default> {{ token.metadata.symbol }}</template>
				</DetailsCard>
				<DetailsCard v-if="token.metadata.description">
					<template #title>Description</template>
					<template #default> {{ token.metadata.description }}</template>
				</DetailsCard>
				<DetailsCard v-if="token.metadata.unique">
					<template #title>Unique</template>
					<template #default> {{ token.metadata.unique }}</template>
				</DetailsCard>
				<DetailsCard v-if="token.metadata.decimals" class="numeric-right-align">
					<template #title>Decimals</template>
					<template #default>
						<span class="numerical">
							{{ token.metadata.decimals }}
						</span>
					</template>
				</DetailsCard>
			</template>
		</div>
	</DrawerContent>
	<DrawerContent v-if="token.metadata && token.metadataUrl">
		<div class="flex flex-col gap-5">
			<DetailsCard>
				<template #title>Metadata Url</template>
				<template #default>
					<TokenMetadataLink :url="token.metadataUrl" />
				</template>
			</DetailsCard>
			<DetailsCard v-if="token.metadata.display?.url">
				<template #title>Display Url</template>
				<template #default>
					<TokenMetadataLink :url="token.metadata.display.url" />
				</template>
			</DetailsCard>
			<DetailsCard v-if="token.metadata.thumbnail?.url">
				<template #title>Thumbnail Url</template>
				<template #default>
					<TokenMetadataLink :url="token.metadata.thumbnail.url" />
				</template>
			</DetailsCard>
			<DetailsCard v-if="token.metadata.artifact?.url">
				<template #title>Artifact Url</template>
				<template #default>
					<TokenMetadataLink :url="token.metadata.artifact.url" />
				</template>
			</DetailsCard>
		</div>
	</DrawerContent>
</template>
<script lang="ts" setup>
import DrawerContent from '../Drawer/DrawerContent.vue'
import DetailsCard from '../DetailsCard.vue'
import TokenAmount from '../atoms/TokenAmount.vue'
import TokenDetailsHeader from './TokenDetailsHeader.vue'
import { PageDropdownInfo } from '~~/src/composables/usePageDropdown'
import { PaginationOffsetInfo } from '~~/src/composables/usePaginationOffset'
import {
	convertTimestampToRelative,
	formatTimestamp,
} from '~~/src/utils/format'
import { Breakpoint } from '~~/src/composables/useBreakpoint'
import { TokenWithMetadata } from '~~/src/types/tokens'

const { NOW } = useDateNow()

const { breakpoint } = useBreakpoint()

type Props = {
	token: TokenWithMetadata
	paginationEvents: PaginationOffsetInfo
	paginationAccounts: PaginationOffsetInfo
	pageDropdownEvents: PageDropdownInfo
	pageDropdownAccounts: PageDropdownInfo
	fetching: boolean
}
const props = defineProps<Props>()

const tabList = computed(() => {
	return [
		`Event (${props.token.tokenEvents?.totalCount ?? 0})`,
		`Accounts (${props.token.accounts?.totalCount ?? 0})`,
	]
})
</script>
<style>
.numeric-right-align h3 {
	text-align: right;
}
</style>
