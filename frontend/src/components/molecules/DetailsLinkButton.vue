<template>
	<LinkButton :on-click="onClick">
		<slot />
	</LinkButton>
</template>

<script lang="ts" setup>
import { useEntityNavigator } from '~/composables/useEntityNavigator'

type Props = {
	entity: string
	id?: string
	hash: string
}

const props = defineProps<Props>()
const { goto, getEntityTypeByString } = useEntityNavigator()

const onClick = () => {
	goto(getEntityTypeByString(props.entity), props.id, props.hash)
}
</script>
