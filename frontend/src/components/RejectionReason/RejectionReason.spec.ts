import RejectionReason from './RejectionReason.vue'
import { setupComponent, screen } from '~/utils/testing'

const defaultProps = {
	reason: { __typename: 'RuntimeFailure' },
}

const { render } = setupComponent(RejectionReason, { defaultProps })

describe('ShowMoreButton', () => {
	it('will fall back to string translations for simple rejection reasons', () => {
		render({})

		expect(screen.getByText('Runtime failure')).toBeInTheDocument()
	})

	it('will render specialised component for more complex rejections', () => {
		const props = {
			reason: {
				__typename: 'DuplicateCredIds',
				credIds: ['1337', '42'],
			},
		}
		render({ props })

		expect(
			screen.getByText('Duplicate credentials (1337, 42)')
		).toBeInTheDocument()
	})
})
