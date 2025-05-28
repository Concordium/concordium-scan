<template>
	<span>
		<!-- Transaction is rejected because of Insufficent balance -->
		<span v-if="reason.eventType === 'tokenBalanceInsufficient'">
			<Tooltip :text="reason.eventType" text-class="text-theme-body">
				<span class="px-2">
					Token holder transaction rejected due to insufficient balance.
				</span>
				<br />

				<span class="px-2">
					Required balance:
					<b>
						{{
							(
								Number(
									reason.details.requiredBalance['@@TAGGED@@'][1][1][
										'$serde_json::private::Number'
									]
								) /
								Math.pow(
									10,
									Math.abs(
										Number(
											reason.details.requiredBalance['@@TAGGED@@'][1][0][
												'$serde_json::private::Number'
											]
										)
									)
								)
							).toFixed(
								Math.abs(
									Number(
										reason.details.requiredBalance['@@TAGGED@@'][1][0][
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
									reason.details.availableBalance['@@TAGGED@@'][1][1][
										'$serde_json::private::Number'
									]
								) /
								Math.pow(
									10,
									Math.abs(
										Number(
											reason.details.availableBalance['@@TAGGED@@'][1][0][
												'$serde_json::private::Number'
											]
										)
									)
								)
							).toFixed(
								Math.abs(
									Number(
										reason.details.availableBalance['@@TAGGED@@'][1][0][
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
import type { TokenModuleReject } from '~/types/generated'

type Props = {
	reason: TokenModuleReject
}

// eslint-disable-next-line @typescript-eslint/no-unused-vars
const props = defineProps<Props>()
</script>
