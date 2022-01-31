<template>
	<nav class="flex justify-center py-8">
		<Button :on-click="handleLoadMore">Load more</Button>
	</nav>
</template>

<script lang="ts" setup>
import type { PageInfo } from '~/types/pageInfo'
type Props = {
	pageInfo: PageInfo
	onLoadMore: () => void
}

const props = defineProps<Props>()

const handleLoadMore = () => props.onLoadMore()
</script>
