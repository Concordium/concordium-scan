<template>
	<span>
		Module <ModuleLink :module-reference="reason.moduleRef" /> already exists
	</span>
</template>

<script setup lang="ts">
import ModuleLink from '~/components/molecules/ModuleLink.vue'
import type { ModuleHashAlreadyExists } from '~/types/generated'

type Props = {
	reason: ModuleHashAlreadyExists
}

defineProps<Props>()
</script>
