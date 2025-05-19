<template>
    <span>
        <!-- Transaction is rejected because of Insufficent balance -->
        <span v-if="reason.eventType == 'tokenBalanceInsufficient'">

            <Tooltip :text="reason.eventType" text-class="text-theme-body">

                <span class="px-2"> Token holder transaction rejected because due to insufficient balance. </span>
                <br>


                <span class="px-2"> Required balance <b> {{
                    reason.details.requiredBalance.map((obj: Record<string, unknown>) => Number(Object.values(obj)[0]))[1] }} {{
                            reason.tokenId
                        }} </b> </span>
                <br>

                <span class="px-2"> Available balance <b> {{
                    reason.details.availableBalance.map((obj: Record<string, unknown>) => Number(Object.values(obj)[0]))[0] }} {{
                            reason.tokenId
                        }} </b> </span>
                <br>

            </Tooltip>

        </span>
        <span v-if="reason.eventType == 'deserializationFailure'">

            <Tooltip :text="reason.eventType" text-class="text-theme-body">
                <span class="px-2"> Token holder transaction rejected because of deserialization failure. </span>
                <br />
                <span class="px-2"> Token Id <b> {{ reason.tokenId }} </b> </span>
                <br />
                <span class="px-2"> Details: <b> {{ reason.details }} </b> </span>


            </Tooltip>
        </span>





    </span>
</template>

<script setup lang="ts">
// import { convertMicroCcdToCcd } from '~/utils/format'
// import Contract from '~/components/molecules/Contract.vue'
import type { TokenHolderTransactionRejectReason } from '~/types/generated'

type Props = {
    reason: TokenHolderTransactionRejectReason
}


const props = defineProps<Props>()

// const addressType = computed(() =>
// 	props.reason.address.__typename === 'AccountAddress'
// 		? ' account'
// 		: ' contract'
// )
</script>
