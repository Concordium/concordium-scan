<template>
	<div v-if="$route.params.internalId">
		Look up on internal id: {{ $route.params.internalId }}
		<TransactionDetailsContent :id="$route.params.internalId" />
	</div>
	<div v-else>
		Default to looking up on hash: {{ $route.params.transactionHash }}
	</div>
</template>
<script lang="ts" setup>
import TransactionDetailsContent from '../../components/TransactionDetails/TransactionDetailsContent.vue'
// const route = useRoute()
</script>
