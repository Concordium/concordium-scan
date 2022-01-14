import { render, screen, fireEvent, waitFor } from '@testing-library/vue'
import Accordion from './Accordion.vue'

const TITLE = 'Hello, World!'
const CONTENT = 'Body text'

const defaultSlots = {
	default: TITLE,
}

const renderComponent = (slots: { default?: string; content?: string }) =>
	render(Accordion, { slots: { ...defaultSlots, ...slots } })

describe('Accordion', () => {
	it('has a title', () => {
		renderComponent({})

		expect(screen.getByText(TITLE)).toBeVisible()
	})

	it('will show content when clicking header', () => {
		renderComponent({ content: CONTENT })

		expect(screen.getByText(CONTENT)).not.toBeVisible()

		fireEvent.click(screen.getByText(TITLE))

		waitFor(() => {
			expect(screen.getByText(CONTENT)).toBeVisible()
		})
	})

	it('will hide content when closing the accordion', () => {
		renderComponent({ content: CONTENT })

		fireEvent.click(screen.getByText(TITLE))

		waitFor(() => {
			expect(screen.getByText(CONTENT)).toBeVisible()
		})

		fireEvent.click(screen.getByText(TITLE))

		waitFor(() => {
			expect(screen.getByText(CONTENT)).not.toBeVisible()
		})
	})
})
