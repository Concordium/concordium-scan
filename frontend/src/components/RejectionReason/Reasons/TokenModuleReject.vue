
<template>
	<span>
		<template v-if="reason.reasonType === 'tokenBalanceInsufficient'">
			Transaction rejected: insufficient balance for token <b class="font-mono">{{ reason.tokenId }}</b>. Available <b>{{ calculateActualValue(reason.details.available_balance.value, reason.details.available_balance.decimals) }}</b>, required <b>{{ calculateActualValue(reason.details.required_balance.value, reason.details.required_balance.decimals) }}</b>.
		</template>

		<template v-else-if="reason.reasonType === 'deserializationFailure'">
			Transaction rejected: deserialization failure for token <b class="font-mono">{{ reason.tokenId }}</b>. Details: {{ reason.details.cause ?? '' }}
		</template>

		<template v-else-if="reason.reasonType === 'addressNotFound'">
			Transaction rejected: address not found for token <b class="font-mono">{{ reason.tokenId }}</b>. Address: <b>{{ reason.details.address.account.address.as_string }}</b>
		</template>

		<template v-else-if="reason.reasonType === 'unsupportedOperation'">
			Transaction rejected: unsupported operation for token <b class="font-mono">{{ reason.tokenId }}</b>. Operation: <b>{{ reason.details.operation_type }}</b>. {{ reason.details.reason ?? '' }}
		</template>

		<template v-else-if="reason.reasonType === 'operationNotPermitted'">
			Operation not permitted for token <b class="font-mono">{{ reason.tokenId }}</b>.<template v-if="reason.details.address?.account?.address"><br />Holder: <b>{{ reason.details.address.account.address.as_string }}</b></template><template v-if="reason.details.reason"><br />Reason: {{ reason.details.reason }}</template>
		</template>

		<template v-else-if="reason.reasonType === 'mintWouldOverflow'">
			Transaction rejected: mint would overflow for token <b class="font-mono">{{ reason.tokenId }}</b>. Requested <b>{{ reason.details.requested_amount }}</b>, current <b>{{ reason.details.current_supply }}</b>, max <b>{{ reason.details.max_representable_amount }}</b>.
		</template>

		<template v-else>
			Transaction rejected for token <b class="font-mono">{{ reason?.tokenId }}</b>. ({{ reason.reasonType }})
		</template>
	</span>
</template>

<script setup lang="ts">
import type { TokenModuleReject } from '~/types/generated'

type Props = {
	reason: TokenModuleReject
}

const props = defineProps<Props>()

function calculateActualValue(value: string, decimals: number): string {
	const n = BigInt(value)
	const d = 10n ** BigInt(decimals)
	const whole = n / d
	const frac = n % d
	return frac === 0n ? `${whole}` : `${whole}.${frac.toString().padStart(decimals, '0').replace(/0+$/, '')}`
}
</script>
