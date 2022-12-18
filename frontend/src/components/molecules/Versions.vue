<template>
	<div>
		<p class="header-version">
			v{{ frontendVersion }}&nbsp;v{{ data?.versions.backendVersion }}
		</p>
	</div>
</template>

<script setup lang="ts">
import { useVersionsQuery } from '~/queries/useVersionsQuery'

const { version: frontendVersion } = useRuntimeConfig()
const { data } = useVersionsQuery()
</script>

<style scoped>
.header-version {
	text-align: end;
	font-size: xx-small;
	color: var(--color-text-regular);
}
</style>
