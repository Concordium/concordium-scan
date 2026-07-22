<template>
	<span v-if="event.__typename === 'LockCreated'">
		Created lock
		<LockLink v-if="event.lockId" :lock-id="event.lockId" />
		<template v-else>with pending lock ID</template>
		<template v-if="event.config?.metadata?.name">
			named <b>{{ event.config.metadata.name }}</b>
		</template>
		<template v-if="event.config">
			expiring {{ formatTimestamp(event.config.expiry) }}
			for
			<template v-if="event.config.recipients.recipientType === 'Any'">
				any eligible recipient
			</template>
			<template v-else>
				<template
					v-for="(account, i) in event.config.recipients.accounts"
					:key="account.address.asString"
				>
					<AccountLink :address="account.address.asString" />
					<template
						v-if="i < event.config.recipients.accounts.length - 1"
					>, </template>
				</template>
			</template>
			<template v-if="event.config.controller.simpleV0">
				<span>
					(SimpleV0<template
						v-if="event.config.controller.simpleV0.keepAlive"
					>, keep alive</template
					><template v-if="event.config.controller.simpleV0.tokenIds.length">
						; tokens
						<b>{{ event.config.controller.simpleV0.tokenIds.join(', ') }}</b></template
					>)
				</span>
			</template>
		</template>
		<template v-else-if="event.configUnavailable">
			with unavailable config
		</template>
	</span>

	<span v-else-if="event.__typename === 'LockFunded'">
		Funded lock <LockLink :lock-id="event.lockId" /> with
		<PltAmount
			:value="event.amount.value"
			:decimals="Number(event.amount.decimals)"
		/>
		<b>{{ event.tokenId }}</b>
		<PltTransferMemo v-if="event.memo" :memo="event.memo" />
	</span>

	<span v-else-if="event.__typename === 'LockSent'">
		Sent
		<PltAmount
			:value="event.amount.value"
			:decimals="Number(event.amount.decimals)"
		/>
		<b>{{ event.tokenId }}</b> from lock <LockLink :lock-id="event.lockId" />
		held by <AccountLink :address="event.source.address.asString" /> to
		<AccountLink :address="event.recipient.address.asString" />
		<PltTransferMemo v-if="event.memo" :memo="event.memo" />
	</span>

	<span v-else-if="event.__typename === 'LockReturned'">
		Returned
		<PltAmount
			:value="event.amount.value"
			:decimals="Number(event.amount.decimals)"
		/>
		<b>{{ event.tokenId }}</b> from lock <LockLink :lock-id="event.lockId" />
		to <AccountLink :address="event.source.address.asString" />
		<PltTransferMemo v-if="event.memo" :memo="event.memo" />
	</span>

	<span v-else-if="event.__typename === 'LockCanceled'">
		Canceled lock <LockLink :lock-id="event.lockId" />
		<template v-if="event.destroyed"> and destroyed it</template>
		<PltTransferMemo v-if="event.memo" :memo="event.memo" />
	</span>

	<span v-else-if="event.__typename === 'LockDestroyed'">
		Destroyed lock <LockLink :lock-id="event.lockId" />
	</span>
</template>

<script setup lang="ts">
import type {
	LockCanceled,
	LockCreated,
	LockDestroyed,
	LockFunded,
	LockReturned,
	LockSent,
} from '~/types/generated'
import { formatTimestamp } from '~/utils/format'
import LockLink from '~/components/molecules/LockLink.vue'
import PltTransferMemo from './PltTransferMemo.vue'

type LockEvent =
	| LockCreated
	| LockFunded
	| LockSent
	| LockReturned
	| LockCanceled
	| LockDestroyed

type Props = {
	event: LockEvent
}

defineProps<Props>()
</script>
