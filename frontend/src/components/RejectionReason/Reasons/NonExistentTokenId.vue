<template>
	<span>
		Plt Token with token id "<span class="font-bold">{{
			props.reason.tokenId
		}}</span
		>"" does not exist.
	</span>
</template>

<script setup lang="ts">
import type { NonExistentTokenId } from '~/types/generated'

type Props = {
	reason: NonExistentTokenId
}

const props = defineProps<Props>()
</script>
