<template>
	{{ tokenId || '-' }}
</template>
<script lang="ts" setup>
type Props = {
	tokenId: string
}
defineProps<Props>()
</script>
