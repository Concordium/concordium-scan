<template>
	<nav class="flex justify-center py-8">
		<Button :on-click="handleLoadMore" :disabled="!props.pageInfo.hasNextPage">
			Load more
		</Button>
	</nav>
</template>

<script lang="ts" setup>
import type { PageInfo } from '~/types/generated'
type Props = {
	pageInfo: PageInfo
	onLoadMore: () => void
}

const props = defineProps<Props>()

const handleLoadMore = () => props.onLoadMore()
</script>
