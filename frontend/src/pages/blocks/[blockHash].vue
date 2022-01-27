<template>
	<div v-if="$route.params.internalId">
		Look up on internal id: {{ $route.params.internalId }}
	</div>
	<div v-else>Default to looking up on hash: {{ $route.params.blockHash }}</div>
</template>
<script lang="ts" setup>
// const route = useRoute()
</script>
