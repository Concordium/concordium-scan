import BakerDetailsHeader from './BakerDetailsHeader.vue'
import { setupComponent, screen } from '~/utils/testing'

jest.mock('~/composables/useDrawer', () => ({
	useDrawer: () => ({
		drawer: {
			push: jest.fn(),
		},
	}),
}))

jest.mock('vue-router', () => ({
	useRouter: () => ({
		push: jest.fn(),
	}),
}))

const defaultProps = {
	baker: {
		account: {
			address: {
				asString: 'c001-acc0un7',
			},
		},
		bakerId: 1337,
		id: '1337-acc-1d',
		state: {
			__typename: 'ActiveBakerState',
			stakedAmount: 1337420666,
			restakeEarnings: true,
		},
	},
}

const { render } = setupComponent(BakerDetailsHeader, {
	defaultProps,
})

describe('BakerDetailsHeader', () => {
	it('will show the baker id', () => {
		render({})

		expect(screen.getByText('1337')).toBeInTheDocument()
	})

	it("will not show the 'Removed' badge if baker is active", () => {
		render({})

		expect(screen.queryByText('Removed')).not.toBeInTheDocument()
	})

	it("can show the baker's 'Removed' state", () => {
		const props = {
			baker: {
				...defaultProps.baker,
				state: {
					__typename: 'RemovedBakerState',
					effectiveTime: '1969-07-20T20:17:40.000Z',
				},
			},
		}

		render({ props })

		expect(screen.getByText('Removed')).toBeInTheDocument()
	})
})
