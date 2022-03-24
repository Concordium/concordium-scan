<template>
	<span>
		Encrypted transfer not allowed to the same account (<Hash
			:hash="reason.accountAddressString"
		/>)
	</span>
</template>

<script setup lang="ts">
import Hash from '~/components/molecules/Hash.vue'
import type { EncryptedAmountSelfTransfer } from '~/types/generated'

type Props = {
	reason: EncryptedAmountSelfTransfer
}

defineProps<Props>()
</script>
