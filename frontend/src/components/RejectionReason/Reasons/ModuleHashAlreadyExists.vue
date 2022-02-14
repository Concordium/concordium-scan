<template>
	<span>
		Module <ModuleRef :module-ref="reason.moduleRef" /> already exists
	</span>
</template>

<script setup lang="ts">
import ModuleRef from '~/components/molecules/ModuleRef.vue'
import type { ModuleHashAlreadyExists } from '~/types/generated'

type Props = {
	reason: ModuleHashAlreadyExists
}

defineProps<Props>()
</script>
