<template>
	<div>
		<AccountDetailsHeader v-if="props.account" :account="props.account" />
		<DrawerContent v-if="props.account">
			<div
				class="
"
			>
				<DetailsCard>
					<template #title>Address</template>
					<template #default>
						{{ props.account?.address }}
					</template>
				</DetailsCard>
			</div>
			<div>
				<DetailsCard v-if="props.account?.createdAt">
					<template #title>Created At</template>
					<template #default>
						{{ convertTimestampToRelative(props.account?.createdAt) }}
					</template>
				</DetailsCard>
			</div>
		</DrawerContent>
	</div>
</template>

<script lang="ts" setup>
import DrawerContent from '~/components/Drawer/DrawerContent.vue'
import DetailsCard from '~/components/DetailsCard.vue'
import { convertTimestampToRelative } from '~/utils/format'
import type { Account } from '~/types/generated'
import AccountDetailsHeader from '~/components/Accounts/AccountDetailsHeader.vue'
type Props = {
	account: Account
}

const props = defineProps<Props>()
</script>

<style module>
.statusIcon {
	@apply h-4 mr-2 text-theme-interactive;
}
.cellIcon {
	@apply h-4 text-theme-white inline align-baseline;
}

.numerical {
	@apply font-mono;
	font-variant-ligatures: none;
}
</style>
