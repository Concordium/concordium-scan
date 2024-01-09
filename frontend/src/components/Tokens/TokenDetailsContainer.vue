<template>
	<div>Hello</div>
</template>
<script lang="ts" setup>
type Props = {
	tokenId: string
	contractAddressIndex: number
	contractAddressSubIndex: number
}
const props = defineProps<Props>()
</script>
