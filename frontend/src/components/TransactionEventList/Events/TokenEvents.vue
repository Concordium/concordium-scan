<template>
	<span v-if="event.event.__typename === 'TokenTransferEvent'">
		Transferred
		<PltAmount
			:value="event.event.amount.value"
			:decimals="Number(event.event.amount.decimals)"
		/>
		<b>{{ event.tokenId }}</b> from
		<AccountLink :address="event.event.from.address.asString" />
		<template v-if="event.event.fromLock">
			(source lock <b>{{ event.event.fromLock }}</b
			>)
		</template>
		to
		<AccountLink :address="event.event.to.address.asString" />
		<template v-if="event.event.toLock">
			(destination lock <b>{{ event.event.toLock }}</b
			>)
		</template>
		<PltTransferMemo v-if="event.event.memo" :memo="event.event.memo" />
	</span>

	<span v-else-if="event.event.__typename === 'MintEvent'">
		Minted
		<PltAmount
			:value="event.event.amount.value"
			:decimals="Number(event.event.amount.decimals)"
		/>
		<b>{{ event.tokenId }}</b> to
		<AccountLink :address="event.event.target.address.asString" />
	</span>

	<span v-else-if="event.event.__typename === 'BurnEvent'">
		Burned
		<PltAmount
			:value="event.event.amount.value"
			:decimals="Number(event.event.amount.decimals)"
		/>
		<b>{{ event.tokenId }}</b> from
		<AccountLink :address="event.event.target.address.asString" />
	</span>
	<span v-else-if="event.event.__typename === 'TokenModuleEvent'">
		<template v-if="event.event.eventType === 'revokeAdminRoles'">
			Revoked admin roles (<template
				v-for="(role, i) in event.event.details.revokeAdminRoles.roles"
				:key="role"
				><b>{{ role }}</b
				><template
					v-if="i < event.event.details.revokeAdminRoles.roles.length - 1"
					>,
				</template></template
			>) for token <b>{{ event.tokenId }}</b> from
			<AccountLink
				:address="event.event.details.revokeAdminRoles.account.address"
			/>
		</template>

		<template v-else-if="event.event.eventType === 'assignAdminRoles'">
			Assigned admin roles (<template
				v-for="(role, i) in event.event.details.assignAdminRoles.roles"
				:key="role"
				><b>{{ role }}</b
				><template
					v-if="i < event.event.details.assignAdminRoles.roles.length - 1"
					>,
				</template></template
			>) for token <b>{{ event.tokenId }}</b> to
			<AccountLink
				:address="event.event.details.assignAdminRoles.account.address"
			/>
		</template>

		<template v-else-if="event.event.eventType === 'removeAllowList'">
			Removed
			<AccountLink
				:address="
					event.event.details?.removeAllowList?.target?.address ||
					event.event.details?.removeAllowList?.account?.address
				"
			/>
			from allow list of token <b>{{ event.tokenId }}</b>
		</template>

		<template v-else-if="event.event.eventType === 'addAllowList'">
			Added
			<AccountLink
				:address="
					event.event.details?.addAllowList?.target?.address ||
					event.event.details?.addAllowList?.account?.address
				"
			/>
			to allow list of token <b>{{ event.tokenId }}</b>
		</template>

		<template v-else-if="event.event.eventType === 'removeDenyList'">
			Removed
			<AccountLink
				:address="
					event.event.details?.removeDenyList?.target?.address ||
					event.event.details?.removeDenyList?.account?.address
				"
			/>
			from deny list of token <b>{{ event.tokenId }}</b>
		</template>

		<template v-else-if="event.event.eventType === 'addDenyList'">
			Added
			<AccountLink
				:address="
					event.event.details?.addDenyList?.target?.address ||
					event.event.details?.addDenyList?.account?.address
				"
			/>
			to deny list of token <b>{{ event.tokenId }}</b>
		</template>

		<template v-else-if="event.event.eventType === 'updateMetadata'">
			Updated metadata for token <b>{{ event.tokenId }}</b> &mdash;
			<a
				class="text-theme-interactive hover:underline break-all"
				:href="event.event.details.updateMetadata.metadataUrl.url"
				target="_blank"
				rel="noopener noreferrer"
			>
				Click here to view
				<ExternalLinkIcon
					stroke="none"
					fill="white"
					class="h-5 w-5 align-top"
				/>
			</a>
		</template>

		<template v-else-if="event.event.eventType === 'pause'">
			Paused token <b>{{ event.tokenId }}</b>
		</template>

		<template v-else-if="event.event.eventType === 'unpause'">
			Unpaused token <b>{{ event.tokenId }}</b>
		</template>

		<template v-else>
			Token module event <b>{{ event.event.eventType }}</b> for token
			<b>{{ event.tokenId }}</b>
		</template>
	</span>
</template>

<script setup lang="ts">
import type { TokenUpdate } from '~/types/generated'
import PltTransferMemo from './PltTransferMemo.vue'
import ExternalLinkIcon from '~/components/icons/ExternalLinkIcon.vue'

type Props = {
	event: TokenUpdate
}

defineProps<Props>()
</script>
