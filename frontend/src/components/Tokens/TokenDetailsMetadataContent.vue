<template>
	<div>
		<div
			class="grid gap-8 grid-cols-2 2xl:grid-cols-4 mb-4 commission-rates rounded-lg px-8 py-4"
		>
			<DetailsCard v-if="metadataUrl">
				<template #title>Metadata Url</template>
				<template #default>
					<span class="text-theme-body text-sm">
						<TokenMetadataLink
							:url="metadataUrl"
							data="Metadata Url"
							:length="50"
							:hide-tooltip="true"
						/>
					</span>
				</template>
			</DetailsCard>
			<DetailsCard v-if="metadata?.name">
				<template #title>Name</template>
				<template #default>
					<span class="text-theme-body text-sm">
						{{ metadata?.name }}
					</span>
				</template>
			</DetailsCard>
			<DetailsCard v-if="metadata?.symbol">
				<template #title>Symbol</template>
				<template #default>
					<span class="text-theme-body text-sm">
						{{ metadata?.symbol }}
					</span>
				</template>
			</DetailsCard>
			<DetailsCard v-if="metadata?.description">
				<template #title>Description</template>
				<template #default>
					<span class="text-theme-body text-sm">
						{{ metadata?.description }}
					</span>
				</template>
			</DetailsCard>
			<DetailsCard v-if="metadata?.unique">
				<template #title>Is Unique</template>
				<template #default>
					<span class="text-theme-body text-sm">
						{{ metadata?.unique }}
					</span>
				</template>
			</DetailsCard>
			<DetailsCard v-if="metadata?.decimals">
				<template #title>Decimals</template>
				<template #default>
					<span class="numerical">
						{{ metadata?.decimals }}
					</span>
				</template>
			</DetailsCard>
			<DetailsCard v-if="metadata?.display?.url">
				<template #title>Display Url</template>
				<template #default>
					<span class="text-theme-body text-sm">
						<TokenMetadataLink
							:url="metadata?.display?.url"
							data="Display Url"
							:length="50"
							:hide-tooltip="true"
						/>
					</span>
				</template>
			</DetailsCard>
			<DetailsCard v-if="metadata?.thumbnail?.url">
				<template #title>Thumbnail Url</template>
				<template #default>
					<span class="text-theme-body text-sm">
						<TokenMetadataLink
							:url="metadata?.thumbnail?.url"
							data="Thumbnail Url"
							:length="50"
						/>
					</span>
				</template>
			</DetailsCard>
		</div>
	</div>
</template>

<script lang="ts" setup>
import DetailsCard from '~/components/DetailsCard.vue'
import { TokenMetadata } from '~/types/tokens'
import TokenMetadataLink from '~/components/molecules/TokenMetadataLink.vue'
type Props = {
	metadata?: TokenMetadata
	metadataUrl?: string | null
}

const props = defineProps<Props>()
</script>

<style scoped>
.commission-rates {
	background-color: var(--color-thead-bg);
}
</style>
