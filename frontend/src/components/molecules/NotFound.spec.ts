import NotFound from './NotFound.vue'
import { setupComponent, screen } from '~/utils/testing'

const { render } = setupComponent(NotFound, {})

describe('NotFound', () => {
	it('will show a default heading and text', () => {
		render({})

		expect(screen.getByRole('heading')).toHaveTextContent('Not found')
		expect(
			screen.getByText('Please check the address and try again')
		).toBeInTheDocument()
	})

	it('can show a custom heading and text', () => {
		const slots = {
			default: 'Guru meditation',
			secondary: 'Press left mouse button to continue',
		}
		render({ slots })

		expect(screen.getByRole('heading')).toHaveTextContent(slots.default)
		expect(screen.getByText(slots.secondary)).toBeInTheDocument()
	})
})
