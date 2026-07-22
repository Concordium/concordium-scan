import { gql, useQuery, type CombinedError } from '@urql/vue'
import type { Ref } from 'vue'
import { useComponentState, type ComponentState } from '~/composables/useComponentState'
import type { Lock } from '~/types/generated'

type QueryParams = {
	lockId: Ref<string>
}

type LockQueryResponse = {
	lock: Lock
}

const accountFields = `
address {
  asString
}
`

const LockQuery = gql`
query LockDetails($lockId: String!) {
  lock(lockId: $lockId) {
    id
    lockId
    status
    createdAt
    expiry
    canceledAt
    metadataName
    metadataDescription
    rawConfig
    creator {
      ${accountFields}
    }
    createdTransaction {
      transactionHash
    }
    canceledTransaction {
      transactionHash
    }
    balances {
      accountAddress {
        asString
      }
      tokenId
      amount {
        value
        decimals
      }
    }
    config {
      expiry
      recipients {
        recipientType
        accounts {
          ${accountFields}
        }
      }
      controller {
        simpleV0 {
          keepAlive
          tokenIds
          grants {
            account {
              ${accountFields}
            }
            roles
          }
        }
      }
      metadata {
        name
        description
      }
    }
    history(first: 50) {
      nodes {
        id
        eventType
        slotTime
        operationOrder
        tokenId
        amount {
          value
          decimals
        }
        account {
          ${accountFields}
        }
        source {
          ${accountFields}
        }
        recipient {
          ${accountFields}
        }
        transaction {
          transactionHash
        }
      }
      pageInfo {
        hasNextPage
      }
    }
  }
}
`

export const useLockQuery = ({
	lockId,
}: QueryParams): {
	data: Ref<LockQueryResponse | undefined>
	error: Ref<CombinedError | undefined>
	componentState: Ref<ComponentState>
	fetching: Ref<boolean>
} => {
	const { data, fetching, error } = useQuery<LockQueryResponse>({
		query: LockQuery,
		requestPolicy: 'cache-first',
		variables: {
			lockId: lockId.value,
		},
	})

	const componentState = useComponentState<LockQueryResponse | undefined>({
		fetching,
		error,
		data,
	})

	return { data, error, componentState, fetching }
}
