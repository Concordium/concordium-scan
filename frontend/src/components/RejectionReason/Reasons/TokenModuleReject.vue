<template>
	<span>
		<!-- Transaction is rejected because of Insufficent balance -->
		<span v-if="reason.reasonType === 'tokenBalanceInsufficient'">
			<Tooltip :text="reason.reasonType" text-class="text-theme-body">
				<span class="px-2">
					Transaction rejected because of insufficent balance.
				</span>
				<br />
				<span class="px-2">
					Token Id <b> {{ reason.tokenId }} </b>
				</span>
				<br />
				<span class="px-2">
					Available balance :
					<b>
						{{
							// Todo: fix this when we reset the devnet DB
							reason.details[reason.reasonType]?.availableBalance?.value /
							10 **
								reason.details[reason.reasonType]?.availableBalance?.decimals[
									'$serde_json::private::Number'
								]
								? reason.details[reason.reasonType]?.availableBalance?.value /
								  10 **
										reason.details[reason.reasonType]?.availableBalance
											?.decimals['$serde_json::private::Number']
								: Number(reason.details.available_balance.value) /
								  10 ** Number(reason.details.available_balance.decimals)
						}}
					</b>
				</span>
				<br />
				<span class="px-2">
					Required balance :
					<b>
						{{
							// Todo: fix this when we reset the devnet DB
							reason.details[reason.reasonType]?.requiredBalance?.value /
							10 **
								reason.details[reason.reasonType]?.requiredBalance?.decimals[
									'$serde_json::private::Number'
								]
								? reason.details[reason.reasonType]?.requiredBalance?.value /
								  10 **
										reason.details[reason.reasonType]?.requiredBalance
											?.decimals['$serde_json::private::Number']
								: Number(reason.details.required_balance.value) /
								  10 ** Number(reason.details.required_balance.decimals)
						}}
					</b>
				</span>
			</Tooltip>
		</span>

		<span v-if="reason.reasonType == 'deserializationFailure'">
			<Tooltip :text="reason.reasonType" text-class="text-theme-body">
				<span class="px-2">
					Transaction rejected because of deserialization failure.
				</span>
				<br />
				<span class="px-2">
					Token Id <b> {{ reason.tokenId }} </b>
				</span>
				<br />
				<span class="px-2">
					Details:
					<b>
						{{
							// Todo: fix this when we reset the devnet DB

							reason.details[reason.reasonType]?.cause ?? reason.details?.cause
						}}
					</b>
				</span>
			</Tooltip>
		</span>
		<span v-if="reason.reasonType == 'addressNotFound'">
			<Tooltip :text="reason.reasonType" text-class="text-theme-body">
				<span class="px-2">
					Transaction rejected because of address is not found.
				</span>
				<br />
				<span class="px-2">
					Token Id <b> {{ reason.tokenId }} </b>
				</span>
				<br />
				<span class="px-2">
					Address:
					<b>
						{{
							// Todo: fix this when we reset the devnet DB

							reason.details[reason.reasonType].address.address ??
							reason.details.address.account.address.as_string
						}}
					</b>
				</span>
			</Tooltip>
		</span>
		<span v-if="reason.reasonType == 'unsupportedOperation'">
			<Tooltip :text="reason.reasonType" text-class="text-theme-body">
				<span class="px-2">
					Transaction rejected because operation is not supported.
				</span>
				<br />
				<span class="px-2">
					Token Id <b> {{ reason.tokenId }} </b>
				</span>
				<br />
				<span class="px-2">
					Operation Type :
					<b>
						{{
							// Todo: fix this when we reset the devnet DB

							reason.details[reason.reasonType].operationType ??
							reason.details.operationType
						}}
					</b>
				</span>
				<br />
				<span class="px-2">
					Details:
					<b>
						{{
							// Todo: fix this when we reset the devnet DB

							reason.details[reason.reasonType].reason ?? reason.details.reason
						}}
					</b>
				</span>
			</Tooltip>
		</span>
		<span v-if="reason.reasonType == 'operationNotPermitted'">
			<Tooltip :text="reason.reasonType" text-class="text-theme-body">
				<span class="px-2">
					Transaction rejected because operation is not permitted.
				</span>
				<br />
				<span class="px-2">
					Token Id : <b> {{ reason.tokenId }} </b>
				</span>
				<br />
				<span
					v-if="
						// Todo: fix this when we reset the devnet DB

						reason.details[reason.reasonType]?.address ??
						reason.details.address?.account?.address
					"
					class="px-2"
				>
					Token Holder :
					<b>
						{{
							// Todo: fix this when we reset the devnet DB

							reason.details[reason.reasonType]?.address?.address ??
							reason.details.address?.account?.address?.as_string
						}}
					</b>
				</span>
				<br />
				<span
					v-if="
						// Todo: fix this when we reset the devnet DB

						reason.details[reason.reasonType]?.address?.coinInfo ??
						reason.details.address?.account?.coin_info
					"
					class="px-2"
				>
					Coin Info :
					<b>
						{{
							// Todo: fix this when we reset the devnet DB

							reason.details[reason.reasonType]?.address?.coinInfo ??
							reason.details.address?.account?.coin_info
						}}
					</b>
				</span>
				<br />
				<span class="px-2">
					Details:
					<b>
						{{
							// Todo: fix this when we reset the devnet DB

							reason.details[reason.reasonType]?.reason ?? reason.details.reason
						}}
					</b>
				</span>
			</Tooltip>
		</span>
		<span v-if="reason.reasonType == 'mintWouldOverflow'">
			<Tooltip :text="reason.reasonType" text-class="text-theme-body">
				<span class="px-2">
					Transaction rejected because mint would overflow.
				</span>
				<br />
				<span class="px-2">
					Token Id <b> {{ reason.tokenId }} </b>
				</span>
				<br />
				<span class="px-2">
					Requested amount :
					<b>
						{{
							// Todo: fix this when we reset the devnet DB

							reason.details[reason.reasonType]?.requested_amount ??
							reason.details.requested_amount
						}}
					</b>
				</span>
				<br />
				<span class="px-2">
					Current supply :
					<b>
						{{
							// Todo: fix this when we reset the devnet DB

							reason.details[reason.reasonType]?.current_supply ??
							reason.details.current_supply
						}}
					</b>
				</span>
				<br />
				<span class="px-2">
					Max representable ammount :
					<b>
						{{
							// Todo: fix this when we reset the devnet DB

							reason.details[reason.reasonType]?.max_representable_amount ??
							reason.details.max_representable_amount
						}}
					</b>
				</span>
			</Tooltip>
		</span>
		<span v-if="reason.reasonType == 'unknow'">
			<Tooltip :text="reason.reasonType" text-class="text-theme-body">
				<span class="px-2"> Transaction rejected due to unknown reason. </span>
				<br />
				<span class="px-2">
					Token Id <b> {{ reason?.tokenId }} </b>
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
