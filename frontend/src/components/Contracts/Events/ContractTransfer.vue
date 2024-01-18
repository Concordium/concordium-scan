<template>
	<div>Amount:</div>
	<div>
		<Amount :amount="contractEvent.amount" />
	</div>
	<div>From:</div>
	<div>
		<AddressLink :address="contractEvent.from" />
	</div>
	<div>To:</div>
	<div>
		<AddressLink :address="contractEvent.to" />
	</div>
</template>
<script lang="ts" setup>
import { Transferred } from '../../../../src/types/generated'
import AddressLink from '../../molecules/AddressLink.vue'
import Amount from '~/components/atoms/Amount.vue'

type Props = {
	contractEvent: Transferred
}
const props = defineProps<Props>()
</script>
