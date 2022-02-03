<template>
	<div v-if="currentTopItem">
		<BlockDetailsDrawer
			v-if="currentTopItem.entityTypeName == 'block'"
			:id="currentTopItem.id"
		/>
		<TransactionDetailsDrawer
			v-if="currentTopItem.entityTypeName == 'transaction'"
			:id="currentTopItem.id"
		/>
	</div>
</template>

<script lang="ts" setup>
import { useDrawer } from '~/composables/useDrawer'

const { currentTopItem } = useDrawer()
</script>
