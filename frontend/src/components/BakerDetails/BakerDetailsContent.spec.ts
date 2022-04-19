import BakerDetailsContent from './BakerDetailsContent.vue'
import { setupComponent, screen } from '~/utils/testing'

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

const { render } = setupComponent(BakerDetailsContent, {
	defaultProps,
})

describe('BakerDetailsContent', () => {
	it('will show the account address', () => {
		render({})

		expect(screen.getByText('c001-acc0un7')).toBeInTheDocument()
	})

	describe('when the baker is ACTIVE', () => {
		it('will show the staked amount', () => {
			render({})

			expect(screen.getByText('1,337.420666 Ͼ')).toBeInTheDocument()
		})

		it('can show that the earnings are being restaked', () => {
			render({})

			expect(
				screen.getByText('Earnings are being restaked')
			).toBeInTheDocument()
		})

		it('can show that the earnings are not being restaked', () => {
			const props = {
				baker: {
					...defaultProps.baker,
					state: { ...defaultProps.baker.state, restakeEarnings: false },
				},
			}

			render({ props })

			expect(
				screen.getByText('Earnings are not being restaked')
			).toBeInTheDocument()
		})
	})
})
