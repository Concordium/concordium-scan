<template>
	<span>
		<!-- Transaction is rejected because of Insufficent balance -->
		<span v-if="reason.eventType == 'tokenBalanceInsufficient'">
			<Tooltip :text="reason.eventType" text-class="text-theme-body">
				<span class="px-2">
					Token holder transaction rejected because due to insufficient balance.
				</span>
				<br />
				<span class="px-2">
					Required balance:
					<b>
						{{
							(
								Number(
									reason.details.requiredBalance[1][
										'$serde_json::private::Number'
									]
								) /
								Math.pow(
									10,
									Number(
										reason.details.requiredBalance[0][
											'$serde_json::private::Number'
										]
									)
								)
							).toFixed(
								Math.abs(
									Number(
										reason.details.availableBalance[0][
											'$serde_json::private::Number'
										]
									)
								)
							)
						}}
						{{ reason.tokenId }}
					</b>
				</span>
				<br />

				<span class="px-2">
					Available balance:
					<b>
						{{
							(
								Number(
									reason.details.availableBalance[1][
										'$serde_json::private::Number'
									]
								) /
								Math.pow(
									10,
									Number(
										reason.details.availableBalance[0][
											'$serde_json::private::Number'
										]
									)
								)
							).toFixed(
								Math.abs(
									Number(
										reason.details.availableBalance[0][
											'$serde_json::private::Number'
										]
									)
								)
							)
						}}
						{{ reason.tokenId }}
					</b>
				</span>
				<br />
			</Tooltip>
		</span>
		<span v-if="reason.eventType == 'deserializationFailure'">
			<Tooltip :text="reason.eventType" text-class="text-theme-body">
				<span class="px-2">
					Token holder transaction rejected because of deserialization failure.
				</span>
				<br />
				<span class="px-2">
					Token Id <b> {{ reason.tokenId }} </b>
				</span>
				<br />
				<span class="px-2">
					Details: <b> {{ reason.details }} </b>
				</span>
			</Tooltip>
		</span>
	</span>
</template>

<script setup lang="ts">
// import { convertMicroCcdToCcd } from '~/utils/format'
// import Contract from '~/components/molecules/Contract.vue'
import type { TokenModule } from '~/types/generated'

type Props = {
	reason: TokenModule
}

const props = defineProps<Props>()

// const addressType = computed(() =>
// 	props.reason.address.__typename === 'AccountAddress'
// 		? ' account'
// 		: ' contract'
// )
</script>
