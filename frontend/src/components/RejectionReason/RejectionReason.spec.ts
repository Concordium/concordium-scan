import { render, screen } from '@testing-library/vue'
import type { RenderOptions } from '@testing-library/vue'
import RejectionReason from './RejectionReason.vue'

const defaultProps = {
	reason: { __typename: 'RuntimeFailure' },
}

const renderComponent = (props?: RenderOptions['props']) =>
	render(RejectionReason, { props: { ...defaultProps, ...props } })

describe('ShowMoreButton', () => {
	it('will fall back to string translations for simple rejection reasons', () => {
		renderComponent({})

		expect(screen.getByText('Runtime failure')).toBeInTheDocument()
	})

	it('will render specialised component for more complex rejections', () => {
		renderComponent({
			reason: {
				__typename: 'DuplicateCredIds',
				credIds: ['1337', '42'],
			},
		})

		expect(
			screen.getByText('Duplicate credentials (1337, 42)')
		).toBeInTheDocument()
	})
})
