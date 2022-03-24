<template>
	<span>
		Reward account <Hash :hash="reason.accountAddressString" /> does not exist
	</span>
</template>

<script setup lang="ts">
import Hash from '~/components/molecules/Hash.vue'
import type { NonExistentRewardAccount } from '~/types/generated'

type Props = {
	reason: NonExistentRewardAccount
}

defineProps<Props>()
</script>
