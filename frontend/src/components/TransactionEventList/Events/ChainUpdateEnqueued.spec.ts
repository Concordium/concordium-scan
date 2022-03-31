import { h } from 'vue'
import ChainUpdateEnqueued from './ChainUpdateEnqueued.vue'
import { setupComponent, screen } from '~/utils/testing'
import type { ChainUpdateEnqueued as ChainUpdateEnqueuedType } from '~/types/generated'

jest.mock('~/composables/useDrawer', () => ({
	useDrawer: () => ({
		drawer: {
			push: jest.fn(),
		},
	}),
}))

jest.mock('~/composables/useDateNow', () => ({
	useDateNow: () => ({
		NOW: new Date('1970-01-01'),
	}),
}))

// mocked as some of its imports causes problems for Jest
jest.mock('~/components/molecules/AccountLink', () => ({
	render: ({ address }: { address: string }) =>
		h(
			'div',
			{ address },
			{
				default: address,
			}
		),
}))

const defaultProps = {
	event: {
		__typename: 'ChainUpdateEnqueued',
		effectiveTime: '1969-07-20T20:17:40.000Z',
		payload: {
			__typename: 'RootKeysChainUpdatePayload',
		},
	} as ChainUpdateEnqueuedType,
}

const { render } = setupComponent(ChainUpdateEnqueued, { defaultProps })

describe('ChainUpdateEnqueued', () => {
	it('will show a chain update with a formatted timestamp', () => {
		render({})

		expect(
			screen.getByText(
				'Chain update enqueued to be effective at Jul 20, 1969, 8:17 PM (5 months ago)'
			)
		).toBeInTheDocument()
	})

	it('AddAnonymityRevokerChainUpdatePayload: will show the name of the new anonymity revoker', () => {
		const props = {
			event: {
				...defaultProps.event,
				payload: {
					__typename: 'AddAnonymityRevokerChainUpdatePayload',
					name: 'MOM Corp.',
				},
			} as ChainUpdateEnqueuedType,
		}
		render({ props })

		expect(
			screen.getByText("Add anonymity revoker 'MOM Corp.'")
		).toBeInTheDocument()
	})

	it('AddIdentityProviderChainUpdatePayload: will show the name of the new identity provider', () => {
		const props = {
			event: {
				...defaultProps.event,
				payload: {
					__typename: 'AddIdentityProviderChainUpdatePayload',
					name: 'Planet Express',
				},
			} as ChainUpdateEnqueuedType,
		}
		render({ props })

		expect(
			screen.getByText("Add identity provider 'Planet Express'")
		).toBeInTheDocument()
	})

	it('BakerStakeThresholdChainUpdatePayload: will show the updated baker threshold', () => {
		const props = {
			event: {
				...defaultProps.event,
				payload: {
					__typename: 'BakerStakeThresholdChainUpdatePayload',
					amount: 1337421337.42,
				},
			} as ChainUpdateEnqueuedType,
		}
		render({ props })

		expect(
			screen.getByText('Update baker stake threshold to 1,337.421337Ͼ')
		).toBeInTheDocument()
	})

	it('ElectionDifficultyChainUpdatePayload: will show the updated election difficulty', () => {
		const props = {
			event: {
				...defaultProps.event,
				payload: {
					__typename: 'ElectionDifficultyChainUpdatePayload',
					electionDifficulty: 42,
				},
			} as ChainUpdateEnqueuedType,
		}
		render({ props })

		expect(
			screen.getByText('Update election difficulty to 42%')
		).toBeInTheDocument()
	})

	it('EuroPerEnergyChainUpdatePayload: will show the updated ENERGY/EUR exchange rate', () => {
		const props = {
			event: {
				...defaultProps.event,
				payload: {
					__typename: 'EuroPerEnergyChainUpdatePayload',
					exchangeRate: {
						numerator: 13371337,
						denominator: 1,
					},
				},
			} as ChainUpdateEnqueuedType,
		}
		render({ props })

		expect(
			screen.getByText('Update ENERGY/EUR exchange rate to 13.371337')
		).toBeInTheDocument()
	})

	it('GasRewardsChainUpdatePayload: will show the new gas rewards', () => {
		const props = {
			event: {
				...defaultProps.event,
				payload: {
					__typename: 'GasRewardsChainUpdatePayload',
					accountCreation: 42.24,
					baker: 13.37,
					chainUpdate: 10.01,
					finalizationProof: 20.22,
				},
			} as ChainUpdateEnqueuedType,
		}
		render({ props })

		expect(screen.getByText('Update gas rewards to:')).toBeInTheDocument()

		expect(
			screen.getByText('Account creation').nextElementSibling
		).toHaveTextContent('42.24')
		expect(screen.getByText('Baker').nextElementSibling).toHaveTextContent(
			'13.37'
		)
		expect(
			screen.getByText('Chain update').nextElementSibling
		).toHaveTextContent('10.01')
		expect(
			screen.getByText('Finalisation proof').nextElementSibling
		).toHaveTextContent('20.22')
	})

	it('Level1KeysChainUpdatePayload: will show the new Level 1 keys', () => {
		const props = {
			event: {
				...defaultProps.event,
				payload: {
					__typename: 'Level1KeysChainUpdatePayload',
				},
			} as ChainUpdateEnqueuedType,
		}
		render({ props })

		expect(screen.getByText('Update Level 1 keys')).toBeInTheDocument()
	})

	it('MicroCcdPerEuroChainUpdatePayload: will show a new exchange rate', () => {
		const props = {
			event: {
				...defaultProps.event,
				payload: {
					__typename: 'MicroCcdPerEuroChainUpdatePayload',
					exchangeRate: {
						numerator: 13371337,
						denominator: 1,
					},
				},
			} as ChainUpdateEnqueuedType,
		}
		render({ props })

		expect(
			screen.getByText(
				'Update CCD/EUR exchange rate to 13.371337 (1Ͼ ≈ 0.075€)'
			)
		).toBeInTheDocument()
	})

	it('MintDistributionChainUpdatePayload: will show the new mint distribution', () => {
		const props = {
			event: {
				...defaultProps.event,
				payload: {
					__typename: 'MintDistributionChainUpdatePayload',
					bakingReward: 42.24,
					finalizationReward: 13.37,
					mintPerSlot: 10.01,
				},
			} as ChainUpdateEnqueuedType,
		}
		render({ props })

		expect(screen.getByText('Update mint distribution to:')).toBeInTheDocument()

		expect(
			screen.getByText('Baking reward account').nextElementSibling
		).toHaveTextContent('42.24')
		expect(
			screen.getByText('Finalisation reward account').nextElementSibling
		).toHaveTextContent('13.37%')
		expect(
			screen.getByText('Mint per slot').nextElementSibling
		).toHaveTextContent('10.01')
	})

	it('ProtocolChainUpdatePayload: will show an update message and specification link', () => {
		const props = {
			event: {
				...defaultProps.event,
				payload: {
					__typename: 'ProtocolChainUpdatePayload',
					message: 'Blockchains are totally rad!',
					specificationUrl: 'https://ccdscan.io/',
				},
			} as ChainUpdateEnqueuedType,
		}
		render({ props })

		expect(
			screen.getByText("Update protocol: 'Blockchains are totally rad!'.")
		).toBeInTheDocument()

		expect(screen.getByText('See specification').closest('a')).toHaveAttribute(
			'href',
			'https://ccdscan.io/'
		)
	})

	it('RootKeysChainUpdatePayload: will show an update message', () => {
		const props = {
			event: {
				...defaultProps.event,
				payload: {
					__typename: 'RootKeysChainUpdatePayload',
				},
			} as ChainUpdateEnqueuedType,
		}
		render({ props })

		expect(screen.getByText('Update root keys')).toBeInTheDocument()
	})

	it('TransactionFeeDistributionChainUpdatePayload: will show the new transaction fee distributions', () => {
		const props = {
			event: {
				...defaultProps.event,
				payload: {
					__typename: 'TransactionFeeDistributionChainUpdatePayload',
					baker: 13.37,
					gasAccount: 20.22,
				},
			} as ChainUpdateEnqueuedType,
		}
		render({ props })

		expect(
			screen.getByText('Update transaction fee distribution to:')
		).toBeInTheDocument()

		expect(
			screen.getByText('Baker account').nextElementSibling
		).toHaveTextContent('13.37')
		expect(
			screen.getByText('Gas account').nextElementSibling
		).toHaveTextContent('20.22')
	})
})
