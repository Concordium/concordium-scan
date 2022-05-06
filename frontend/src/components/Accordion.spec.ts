import Accordion from './Accordion.vue'
import { setupComponent, screen, fireEvent, waitFor } from '~/utils/testing'

const TITLE = 'Hello, World!'
const CONTENT = 'Body text'

const defaultSlots = {
	default: TITLE,
}

const { render } = setupComponent(Accordion, { defaultSlots })

describe('Accordion', () => {
	it('has a title', () => {
		render({})

		expect(screen.getByText(TITLE)).toBeVisible()
	})

	it('will show content when clicking header', () => {
		const slots = { content: CONTENT }
		render({ slots })

		expect(screen.queryByText(CONTENT)).not.toBeInTheDocument()

		fireEvent.click(screen.getByText(TITLE))

		waitFor(() => {
			expect(screen.getByText(CONTENT)).toBeVisible()
		})
	})

	it('will hide content when closing the accordion', () => {
		const slots = { content: CONTENT }
		render({ slots })

		fireEvent.click(screen.getByText(TITLE))

		waitFor(() => {
			expect(screen.getByText(CONTENT)).toBeVisible()
		})

		fireEvent.click(screen.getByText(TITLE))

		waitFor(() => {
			expect(screen.getByText(CONTENT)).not.toBeVisible()
		})
	})

	it('can be open by default', () => {
		const slots = { content: CONTENT }
		const props = { isInitialOpen: true }
		render({ slots, props })

		expect(screen.getByText(CONTENT)).toBeVisible()
	})

	it('can be closed after being initially open', () => {
		const slots = { content: CONTENT }
		const props = { isInitialOpen: true }
		render({ slots, props })

		expect(screen.getByText(CONTENT)).toBeVisible()

		fireEvent.click(screen.getByText(TITLE))

		waitFor(() => {
			expect(screen.getByText(CONTENT)).not.toBeVisible()
		})
	})
})
